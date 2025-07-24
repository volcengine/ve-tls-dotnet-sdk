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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Logging;
using VolcengineTls.Error;

namespace VolcengineTls.Producer
{
    public class ProducerImp : IProducer
    {
        private int _isStarted = 0;
        private readonly ILogger _logger;
        private readonly ProducerConfig _config;
        private readonly ProducerSender _sender;
        private readonly ProducerThreadPool _threadPool;
        private readonly ProducerDispatcher _dispatcher;
        internal static long _producerLogGroupSize;

        public ProducerImp(ProducerConfig config)
        {
            _config = config;
            _logger = config.Logger;

            _sender = new ProducerSender(config, new ProducerRetryQueue(_logger));
            _threadPool = new ProducerThreadPool(_sender, _logger);
            _dispatcher = new ProducerDispatcher(config, _sender, _threadPool);
        }

        public void ResetAccessKeyToken(string accessKeyId, string accessKeySecret, string securityToken)
        {
            _config.TlsClient.ResetAccessKeyToken(accessKeyId, accessKeySecret, securityToken);
        }

        public void Start()
        {
            int isStarted = Interlocked.CompareExchange(ref _isStarted, Consts.ProducerStart, Consts.ProducerStop);
            if (isStarted != 0)
            {
                _logger.LogWarning("producer is already started.");
                return;
            }

            _threadPool.Start();
            _dispatcher.Start();

            _logger.LogInformation("producer start");
        }

        public void Close()
        {
            int isStarted = Interlocked.CompareExchange(ref _isStarted, Consts.ProducerStop, Consts.ProducerStart);
            if (isStarted != 1)
            {
                _logger.LogWarning("producer is already closed or not started");
                return;
            }

            _sender.Close();
            _threadPool.Close();
            _dispatcher.Close();

            // 重置日志大小统计
            Interlocked.Exchange(ref _producerLogGroupSize, 0);

            _logger.LogInformation("producer closed");
        }

        public void ForceClose()
        {
            int isStarted = Interlocked.CompareExchange(ref _isStarted, Consts.ProducerStop, Consts.ProducerStart);
            if (isStarted != 1)
            {
                _logger.LogWarning("producer is already closed or not started");
                return;
            }

            _sender.ForceClose();
            _threadPool.ForceClose();
            _dispatcher.ForceClose();

            // 重置日志大小统计
            Interlocked.Exchange(ref _producerLogGroupSize, 0);

            _logger.LogInformation("producer force closed");
        }

        public void SendLog(string topicId, Pb.Log log, string source = null, string filename = null, string shardHash = null, ICallBack callback = null)
        {
            WaitTime();

            var batchLog = new ProducerBatchLog
            {
                Key = new ProducerBatchLogKey
                {
                    TopicId = topicId,
                    Source = source,
                    FileName = filename,
                    ShardHash = shardHash,
                    CallBackFun = callback,
                },
                Log = log,
            };

            PutToDisPatcher(batchLog);
        }

        public void SendLogV2(string topicId, Log log, string source = null, string filename = null, string shardHash = null, ICallBack callback = null)
        {
            WaitTime();

            var pbLog = new Pb.Log()
            {
                Time = log.Time,
                Contents = {
                    log.Contents.Select(lc => new Pb.LogContent
                    {
                        Key = lc.Key,
                        Value = lc.Value
                    })
                }
            };

            var batchLog = new ProducerBatchLog
            {
                Key = new ProducerBatchLogKey
                {
                    TopicId = topicId,
                    Source = source,
                    FileName = filename,
                    ShardHash = shardHash,
                    CallBackFun = callback,
                },
                Log = pbLog,
            };

            PutToDisPatcher(batchLog);
        }

        public void SendLogs(string topicId, Pb.LogGroup logs, string source = null, string filename = null, string shardHash = null, ICallBack callback = null)
        {
            WaitTime();

            foreach (var log in logs.Logs)
            {
                var batchLog = new ProducerBatchLog
                {
                    Key = new ProducerBatchLogKey
                    {
                        TopicId = topicId,
                        Source = source,
                        FileName = filename,
                        ShardHash = shardHash,
                        ContextFlow = logs.ContextFlow,
                        CallBackFun = callback,
                    },
                    Log = log,
                };

                PutToDisPatcher(batchLog);
            }
        }

        public void SendLogsV2(string topicId, List<Log> logs, string source = null, string filename = null, string shardHash = null, string contextFlow = null, ICallBack callback = null)
        {
            WaitTime();

            foreach (var log in logs)
            {
                var pbLog = new Pb.Log()
                {
                    Time = log.Time,
                    Contents = {
                        log.Contents.Select(lc => new Pb.LogContent
                        {
                            Key = lc.Key,
                            Value = lc.Value
                        })
                    }
                };

                var batchLog = new ProducerBatchLog
                {
                    Key = new ProducerBatchLogKey
                    {
                        TopicId = topicId,
                        Source = source,
                        FileName = filename,
                        ShardHash = shardHash,
                        ContextFlow = contextFlow,
                        CallBackFun = callback,
                    },
                    Log = pbLog,
                };

                PutToDisPatcher(batchLog);
            }
        }

        internal static void AddProducerLogGroupSize(long shouldAddSize)
        {
            Interlocked.Add(ref _producerLogGroupSize, shouldAddSize);
        }

        private void PutToDisPatcher(ProducerBatchLog batchLog)
        {
            if (_isStarted == Consts.ProducerStop)
            {
                _logger.LogInformation("the producer is closed");
                return;
            }

            _dispatcher.AddNewBatchLog(batchLog);
        }

        private void WaitTime()
        {
            if (_config.BlockSec > 0)
            {
                for (var i = 0; i < _config.BlockSec; i++)
                {
                    if (Interlocked.Read(ref _producerLogGroupSize) <= _config.TotalSizeInBytes)
                    {
                        return;
                    }

                    Thread.Sleep(TimeSpan.FromSeconds(1));
                }

                throw new TlsError(Consts.SdkErrorMaxBlockSecTimeOut);
            }

            if (_config.BlockSec == 0)
            {
                if (Interlocked.Read(ref _producerLogGroupSize) > _config.TotalSizeInBytes)
                {
                    throw new TlsError(Consts.SdkErrorMaxBlockSecTimeOut);
                }

                return;
            }

            if (_config.BlockSec < 0)
            {
                while (true)
                {
                    if (Interlocked.Read(ref _producerLogGroupSize) <= _config.TotalSizeInBytes)
                    {
                        return;
                    }

                    Thread.Sleep(TimeSpan.FromSeconds(1));
                }
            }
        }
    }
}
