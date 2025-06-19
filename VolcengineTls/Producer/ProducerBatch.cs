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
using System.Collections.Generic;
using System.Threading;

namespace VolcengineTls.Producer
{
    public class ProducerBatch
    {
        private long _totalDataSize;
        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();

        internal long TotalDataSize
        {
            get
            {
                return Interlocked.Read(ref _totalDataSize);
            }
            set
            {
                Interlocked.Exchange(ref _totalDataSize, value);
            }
        }
        internal Pb.LogGroup LogGroup { get; private set; }
        internal int AttemptCount { get; set; } = 0;
        internal long RetryBackoffMs { get; set; } = 0;
        internal long BaseRetryBackoffMs { get; set; }
        internal long BaseIncreaseRetryBackoffMs { get; set; } = 1000;
        internal long NextRetryMs { get; set; }
        internal long MaxRetryIntervalInMs { get; set; }
        internal List<ICallBack> CallBackList { get; private set; } = new List<ICallBack>();
        internal DateTime CreateTime { get; private set; } = DateTime.Now;
        internal int MaxRetryTimes { get; set; }
        internal string TopicId { get; private set; }
        internal string ShardHash { get; private set; }
        internal ProducerResult Result { get; private set; } = new ProducerResult();
        internal int MaxReservedAttempts { get; set; }

        internal ProducerBatch(ProducerBatchLog batchLog, ProducerConfig config)
        {
            LogGroup = new Pb.LogGroup()
            {
                Source = batchLog.Key.Source ?? string.Empty,
                FileName = batchLog.Key.FileName ?? string.Empty,
                ContextFlow = batchLog.Key.ContextFlow ?? string.Empty,
                Logs = {batchLog.Log},
            };
            _totalDataSize = LogGroup.CalculateSize();

            MaxRetryIntervalInMs = config.RetryBackoffMs;
            MaxRetryTimes = config.Retries;
            BaseRetryBackoffMs = config.BaseRetryBackoffMs;
            TopicId = batchLog.Key.TopicId;
            MaxReservedAttempts = config.ReservedAttempts;
            ShardHash = ParseHash(batchLog.Key.ShardHash);

            if (batchLog.Key.CallBackFun != null)
            {
                CallBackList.Add(batchLog.Key.CallBackFun);
            }
        }

        internal int GetLogCount()
        {
            _lock.EnterReadLock();
            try
            {
                return LogGroup.Logs.Count;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        internal void AddLogToLogGroup(Pb.Log log)
        {
            _lock.EnterWriteLock();
            try
            {
                LogGroup.Logs.Add(log);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        internal void AddProducerBatchCallBack(ICallBack callBack)
        {
            _lock.EnterWriteLock();
            try
            {
                CallBackList.Add(callBack);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        internal static string ParseHash(string inputHash)
        {
            if (string.IsNullOrEmpty(inputHash))
            {
                return null;
            }

            return inputHash;
        }

        internal void AddTotalDataSize(long shouldAddSize)
        {
            Interlocked.Add(ref _totalDataSize, shouldAddSize);
        }
    }

    public class ProducerBatchLog
    {
        public ProducerBatchLogKey Key { get; set; }
        public Pb.Log Log { get; set; }
    }

    public class ProducerBatchLogKey
    {
        public string Source { get; set; }
        public string FileName { get; set; }
        public string ContextFlow { get; set; }
        public string TopicId { get; set; }
        public string ShardHash { get; set; }
        public ICallBack CallBackFun { get; set; }
    }
}
