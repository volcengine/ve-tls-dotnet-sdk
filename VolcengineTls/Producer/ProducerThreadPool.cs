/*
 * Copyright (2023) Volcengine
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
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace VolcengineTls.Producer
{
    public class ProducerThreadPool : IDisposable
    {
        private readonly ILogger _logger;
        private readonly ProducerSender _sender;
        private CountdownEvent _countdownEvent;
        private readonly CancellationTokenSource _stopCts = new CancellationTokenSource();
        private readonly BlockingCollection<ProducerBatch> _taskQueue = new BlockingCollection<ProducerBatch>(
            new ConcurrentQueue<ProducerBatch>(),
            100
        );

        internal ProducerThreadPool(ProducerSender sender, ILogger logger)
        {
            if (sender == null) throw new ArgumentNullException("sender");
            if (logger == null) throw new ArgumentNullException("logger");

            _sender = sender;
            _logger = logger;
        }

        public void Dispose()
        {
            _stopCts.Dispose();

            _countdownEvent.Dispose();

            _taskQueue.Dispose();
        }

        internal void Close()
        {
            _stopCts.Cancel();

            _countdownEvent.Wait();

            Dispose();

            _logger.LogInformation("producer threadpool quit.");
        }

        internal void ForceClose()
        {
            _stopCts.Cancel();

            Dispose();

            _logger.LogInformation("producer threadpool forced quit.");
        }

        internal void Start()
        {
            _countdownEvent = new CountdownEvent(1);

            Task.Run(async () =>
            {
                try
                {
                    await ProcessTasksAsync();
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("producer threadpool canceled");
                }
                catch (Exception ex)
                {
                    _logger.LogError("Error in thread pool processing: " + ex);
                }
                finally
                {
                    _countdownEvent.Signal();
                    _logger.LogInformation("ProducerThreadPool processing task exited.");
                }
            });

            _logger.LogInformation("producer threadpool start.");
        }

        private async Task ProcessTasksAsync()
        {
            ProducerBatch batch;
            CancellationToken cancellationToken = _stopCts.Token;

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    batch = _taskQueue.Take(cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    _taskQueue.CompleteAdding();
                    break;
                }

                await _sender.HandleBatchAsync(batch);
            }

            ProducerBatch remainingBatch;
            while (_taskQueue.TryTake(out remainingBatch))
            {
                await _sender.HandleBatchAsync(remainingBatch);
            }
        }

        internal void AddBatchToTaskQueue(ProducerBatch batch)
        {
            if (!_taskQueue.IsAddingCompleted)
            {
                _taskQueue.Add(batch);
            }
            else
            {
                _logger.LogWarning("Attempted to add batch after completing adding.");
            }
        }
    }
}
