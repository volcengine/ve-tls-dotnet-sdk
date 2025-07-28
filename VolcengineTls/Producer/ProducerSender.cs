/*
 * Copyright 2025 Beijing Volcano Engine Technology Ltd.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VolcengineTls.Error;
using VolcengineTls.Request;
using VolcengineTls.Response;

namespace VolcengineTls.Producer
{
    public interface ICallBack
    {
        void Success(ProducerResult result);
        void Fail(ProducerResult result);
    }

    public class ProducerSender : IDisposable
    {
        private readonly IClient _client;
        private readonly ILogger _logger;
        private readonly HashSet<int> _noRetryStatusCodeMap;
        private readonly SemaphoreSlim _semaphore;
        private CountdownEvent _countdownEvent;
        private readonly CancellationTokenSource _stopCts = new CancellationTokenSource();

        internal readonly ProducerRetryQueue _retryQueue;

        internal ProducerSender(
            ProducerConfig producerConfig,
            ProducerRetryQueue retryQueue
        )
        {
            _client = producerConfig.TlsClient;
            _retryQueue = retryQueue;
            _logger = producerConfig.Logger;
            _noRetryStatusCodeMap = producerConfig.NoRetryStatusCodeList;

            _semaphore = new SemaphoreSlim(producerConfig.SenderCount, producerConfig.SenderCount);
        }

        public void Dispose()
        {
            _semaphore.Dispose();

            _countdownEvent?.Dispose();

            _stopCts.Dispose();
        }

        internal void Close()
        {
            _stopCts.Cancel();

            _countdownEvent?.Wait();

            Dispose();
        }

        internal void ForceClose()
        {
            _stopCts.Cancel();

            Dispose();
        }

        internal async Task HandleBatchAsync(ProducerBatch batch)
        {
            if (batch == null)
                return;

            _countdownEvent = new CountdownEvent(1);

            try
            {
                await _semaphore.WaitAsync();
                await SendToServerAsync(batch);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error sending batch: " + ex);
            }
            finally
            {
                _countdownEvent.Signal();
                _semaphore.Release();
            }
        }

        private async Task SendToServerAsync(ProducerBatch batch)
        {
            _logger.LogDebug("sender send data to server");

            Exception error;

            var logGroupList = new Pb.LogGroupList();
            logGroupList.LogGroups.Add(batch.LogGroup);

            var putLogsRequest = new PutLogsRequest(
                topicId: batch.TopicId,
                logGroupList: logGroupList
            );

            if (!string.IsNullOrEmpty(batch.ShardHash))
            {
                putLogsRequest.HashKey = batch.ShardHash;
            }

            try
            {
                var resp = await _client.PutLogs(putLogsRequest);
                HandleSuccess(batch, resp);
                return;
            }
            catch (Exception ex)
            {
                error = ex;
                _logger.LogWarning($"put log error, Ready to check for retries, error is {error.Message}");
            }

            HandleFailure(batch, error);
        }

        private void HandleSuccess(ProducerBatch batch, PutLogsResponse putLogsResp)
        {
            _logger.LogDebug("sendToServer succeeded,Execute successful callback function");

            ProducerImp.AddProducerLogGroupSize(-batch.TotalDataSize);

            batch.Result.SuccessFlag = true;

            if (batch.AttemptCount < batch.MaxReservedAttempts)
            {
                batch.Result.Attempts.Add(
                    new ProducerAttempt(
                        successFlag: true,
                        requestId: putLogsResp.RequestId,
                        errorCode: "",
                        errorMessage: "",
                        timestampMs: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                    )
                );
            }

            foreach (var callBack in batch.CallBackList)
            {
                callBack.Success(batch.Result);
            }

            batch.RetryBackoffMs = 0;
        }

        internal void HandleFailure(ProducerBatch batch, Exception error)
        {
            _logger.LogInformation($"sendToServer failed: {error?.Message}");

            bool noRetryStatusCode = false;
            var sdkError = error as TlsError;
            if (sdkError != null)
            {
                noRetryStatusCode = _noRetryStatusCodeMap.Contains(sdkError.HttpCode);
            }

            bool noNeedRetry = batch.AttemptCount >= batch.MaxRetryTimes;

            if (_stopCts.IsCancellationRequested || (sdkError != null && noRetryStatusCode) || noNeedRetry)
            {
                AddErrorMessageToBatchAttempt(batch, error, false);
                FailedCallback(batch);
                return;
            }

            _logger.LogDebug("Submit to the retry queue after meeting the retry criteria");

            AddErrorMessageToBatchAttempt(batch, error, true);

            if (batch.AttemptCount == 1)
            {
                batch.RetryBackoffMs += batch.BaseRetryBackoffMs;
            }
            else
            {
                var random = new Random();
                double increaseBackoffMs = random.NextDouble() * batch.BaseIncreaseRetryBackoffMs;
                batch.RetryBackoffMs += (long)increaseBackoffMs;
            }

            if (batch.RetryBackoffMs > batch.MaxRetryIntervalInMs)
            {
                batch.RetryBackoffMs = batch.MaxRetryIntervalInMs;
            }

            batch.NextRetryMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + batch.RetryBackoffMs;

            _retryQueue.AddToRetryQueue(batch);
        }

        private void FailedCallback(ProducerBatch batch)
        {
            _logger.LogInformation("sendToServer failed,Execute failed callback function");

            ProducerImp.AddProducerLogGroupSize(-batch.TotalDataSize);

            foreach (var callBack in batch.CallBackList)
            {
                callBack.Fail(batch.Result);
            }
        }

        private void AddErrorMessageToBatchAttempt(ProducerBatch batch, Exception error, bool retryInfo)
        {
            if (batch.AttemptCount < batch.MaxReservedAttempts)
            {
                ProducerAttempt attempt;

                var tlsError = error as TlsError;
                if (tlsError != null)
                {
                    if (retryInfo)
                    {
                        _logger.LogInformation($"sendToServer failed,start retrying. Retry times: {batch.AttemptCount}, RequestId: {tlsError.RequestId}, Error code: {tlsError.Code}, Error message: {tlsError.Message}");
                    }

                    attempt = new ProducerAttempt(
                        successFlag: false,
                        requestId: tlsError.RequestId,
                        errorCode: tlsError.Code,
                        errorMessage: tlsError.Message,
                        timestampMs: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                    );
                }
                else
                {
                    _logger.LogError($"putLogs internal err: {error?.Message}");

                    attempt = new ProducerAttempt(
                        successFlag: false,
                        requestId: "",
                        errorCode: "",
                        errorMessage: error?.Message ?? "Unknown error",
                        timestampMs: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                    );
                }

                batch.Result.Attempts.Add(attempt);
            }

            batch.Result.SuccessFlag = false;
            batch.AttemptCount++;
        }
    }
}
