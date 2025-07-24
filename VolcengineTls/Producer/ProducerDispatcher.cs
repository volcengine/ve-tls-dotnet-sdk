/*
 * Copyright (2025) Volcengine
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
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace VolcengineTls.Producer
{
    public class ProducerDispatcher : IDisposable
    {
        private int closeType = Consts.ForceCloseNo;
        private readonly ILogger _logger;
        private readonly ProducerSender _sender;
        private readonly ProducerConfig _config;
        private readonly ProducerThreadPool _threadPool;
        private readonly ReaderWriterLockSlim _rwLockSlim = new ReaderWriterLockSlim();
        private CountdownEvent _countDownEvent;
        private readonly CancellationTokenSource _stopCts = new CancellationTokenSource();
        private readonly BlockingCollection<ProducerBatchLog> _newLogRecvCollection = new BlockingCollection<ProducerBatchLog>(
            new ConcurrentQueue<ProducerBatchLog>(),
            100
        );
        private readonly ConcurrentDictionary<string, ProducerBatch> _logGroupData = new ConcurrentDictionary<string, ProducerBatch>();

        public ProducerDispatcher(ProducerConfig config, ProducerSender sender, ProducerThreadPool threadPool)
        {
            _logger = config.Logger;
            _config = config;
            _sender = sender;
            _threadPool = threadPool;
        }

        public void Dispose()
        {
            _rwLockSlim.Dispose();

            _countDownEvent.Dispose();

            _stopCts.Dispose();

        }

        internal void Close()
        {
            _stopCts.Cancel();

            _countDownEvent.Wait();

            Dispose();
        }

        internal void ForceClose()
        {
            closeType = Consts.ForceCloseYes;

            _stopCts.Cancel();

            Dispose();
        }

        internal void Start()
        {
            _countDownEvent = new CountdownEvent(2);

            Task.Run(() => Run());
            Task.Run(() => CheckBatchTime());
        }

        private void Run()
        {
            try
            {
                while (!_stopCts.Token.IsCancellationRequested)
                {
                    try
                    {
                        var newBatchLog = _newLogRecvCollection.Take(_stopCts.Token);
                        HandleLogs(newBatchLog);
                    }
                    catch (OperationCanceledException)
                    {
                        _newLogRecvCollection.CompleteAdding();
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in dispatcher run: {ex.Message}");
            }
            finally
            {
                _countDownEvent.Signal();
            }
        }

        internal void AddNewBatchLog(ProducerBatchLog batchLog)
        {
            try
            {
                _newLogRecvCollection.Add(batchLog, _stopCts.Token);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("AddNewBatchLog was canceled due to StopCts being triggered.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error adding log to collection.");
            }
        }

        private void CheckBatchTime()
        {
            try
            {
                while (!_stopCts.Token.IsCancellationRequested)
                {
                    CheckBatches(_config);
                    Thread.Sleep(100); // 短暂休眠避免CPU占用过高
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in batch time checker: {ex.Message}");
            }
            finally
            {
                if (closeType == Consts.ForceCloseNo)
                {
                    RetryQueueElegantQuit();
                }
                _countDownEvent.Signal();
            }
        }

        private void CheckBatches(ProducerConfig config)
        {
            var sleepMs = config.LingerTime;
            var mapCount = _logGroupData.Count;

            _rwLockSlim.EnterWriteLock();
            try
            {
                foreach (var item in _logGroupData.ToList())
                {
                    var key = item.Key;
                    var batch = item.Value;

                    var timeInterval = DateTime.Now - batch.CreateTime;

                    if (timeInterval >= config.LingerTime)
                    {
                        _logger.LogDebug("execute sending producerBatch to Sender");
                        _threadPool.AddBatchToTaskQueue(batch);

                        ProducerBatch removedBatch;
                        _logGroupData.TryRemove(key, out removedBatch);
                    }
                    else
                    {
                        if (sleepMs > timeInterval)
                        {
                            sleepMs = timeInterval;
                        }
                    }
                }
            }
            finally
            {
                _rwLockSlim.ExitWriteLock();
            }

            if (mapCount == 0)
            {
                _logger.LogDebug("No data time in map waiting for user configured RemainMs parameter values");
                sleepMs = config.LingerTime;
            }

            var retryProducerBatchList = _sender._retryQueue.GetRetryBatch(false);
            if (retryProducerBatchList == null)
            {
                while (sleepMs.TotalMilliseconds > 0)
                {
                    if (_stopCts.Token.IsCancellationRequested)
                    {
                        return;
                    }
                    var sleepTimeInterval = TimeSpan.FromSeconds(1);
                    Thread.Sleep(sleepTimeInterval);
                    sleepMs -= sleepTimeInterval;
                }
            }
            else
            {
                foreach (var producerBatch in retryProducerBatchList)
                {
                    _threadPool.AddBatchToTaskQueue(producerBatch);
                }
            }
        }

        internal void RetryQueueElegantQuit()
        {
            try
            {
                // 处理队列中剩余的日志
                ProducerBatchLog batchLog;
                while (_newLogRecvCollection.TryTake(out batchLog))
                {
                    HandleLogs(batchLog);
                }

                _rwLockSlim.EnterWriteLock();
                try
                {
                    // 发送所有未处理的批次
                    foreach (var batch in _logGroupData.Values)
                    {
                        _threadPool.AddBatchToTaskQueue(batch);
                    }

                    _logGroupData.Clear();
                }
                finally
                {
                    _rwLockSlim.ExitWriteLock();
                }

                // 处理所有重试批次
                var producerBatchList = _sender._retryQueue.GetRetryBatch(true);
                foreach (var producerBatch in producerBatchList)
                {
                    _threadPool.AddBatchToTaskQueue(producerBatch);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during elegant quit: {ex.Message}");
            }
        }

        private void HandleLogs(ProducerBatchLog batchLog)
        {
            // dispatcher is closed
            if (batchLog == null)
            {
                return;
            }

            var key = GetKeyString(batchLog.Key);
            var logSize = batchLog.Log.CalculateSize();

            _rwLockSlim.EnterWriteLock();
            try
            {
                ProducerBatch batch;
                if (_logGroupData.TryGetValue(key, out batch))
                {
                    batch.AddTotalDataSize(logSize);
                    ProducerImp.AddProducerLogGroupSize(logSize);
                    AddOrSendProducerBatch(key, batchLog, batch);
                }
                else
                {
                    CreateNewProducerBatch(batchLog, key);
                    _logGroupData[key].AddTotalDataSize(logSize);
                    ProducerImp.AddProducerLogGroupSize(logSize);
                }
            }
            finally
            {
                _rwLockSlim.ExitWriteLock();
            }
        }

        private void AddOrSendProducerBatch(string key, ProducerBatchLog batchLog, ProducerBatch batch)
        {
            var totalDataCount = batch.GetLogCount() + 1;

            if (batch.TotalDataSize > _config.BatchSize &&
                batch.TotalDataSize < ProducerConfig.BATCH_SIZE_MAX &&
                totalDataCount <= _config.BatchCount)
            {
                batch.AddLogToLogGroup(batchLog.Log);

                if (batchLog.Key.CallBackFun != null)
                {
                    batch.AddProducerBatchCallBack(batchLog.Key.CallBackFun);
                }

                InnerSendToServer(key, batch);
            }
            else if (batch.TotalDataSize <= _config.BatchSize && totalDataCount <= _config.BatchCount)
            {
                batch.AddLogToLogGroup(batchLog.Log);

                if (batchLog.Key.CallBackFun != null)
                {
                    batch.AddProducerBatchCallBack(batchLog.Key.CallBackFun);
                }
            }
            else
            {
                InnerSendToServer(key, batch);
                CreateNewProducerBatch(batchLog, key);
            }
        }

        private void CreateNewProducerBatch(ProducerBatchLog batchLog, string key)
        {
            _logger.LogDebug("Create a new ProducerBatch");

            var newProducerBatch = new ProducerBatch(batchLog, _config);

            _logGroupData[key] = newProducerBatch;
        }

        private void InnerSendToServer(string key, ProducerBatch producerBatch)
        {
            _logger.LogDebug("Send producerBatch to Sender from dispatcher");

            _threadPool.AddBatchToTaskQueue(producerBatch);

            ProducerBatch removedBatch;
            _logGroupData.TryRemove(key, out removedBatch);
        }

        private string GetKeyString(ProducerBatchLogKey batchLogKey)
        {
            string delimiter = "|";

            var keyBuilder = new StringBuilder();

            keyBuilder.Append(batchLogKey.TopicId);
            keyBuilder.Append(delimiter);
            keyBuilder.Append(batchLogKey.ShardHash);
            keyBuilder.Append(delimiter);
            keyBuilder.Append(batchLogKey.Source);
            keyBuilder.Append(delimiter);
            keyBuilder.Append(batchLogKey.FileName);
            keyBuilder.Append(delimiter);
            keyBuilder.Append(batchLogKey.ContextFlow);

            return keyBuilder.ToString();
        }
    }
}
