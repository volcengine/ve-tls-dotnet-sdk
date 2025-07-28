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
using Microsoft.Extensions.Logging;

namespace VolcengineTls.Producer
{
    public class ProducerConfig
    {
        internal const long TOTAL_SIZE_IN_BYTES_DEFAULT = 100 * 1024 * 1024;
        internal const long TOTAL_SIZE_IN_BYTES_MAX = long.MaxValue;

        internal const int SENDER_COUNT_DEFAULT = 10;
        internal const int SENDER_COUNT_MAX = int.MaxValue;

        internal const int RESERVED_ATTEMPTS_DEFAUL = 11;
        internal const int RESERVED_ATTEMPTS_MAX = int.MaxValue;

        internal const int BATCH_COUNT_DEFAULT = 4096;
        internal const int BATCH_COUNT_MAX = 10000;

        internal const int BATCH_SIZE_DEFAULT = 512 * 1024;
        internal const int BATCH_SIZE_MAX = 5 * 1024 * 1024;

        internal const int BASE_RETRY_BACKOFF_MS_DEFAULT = 1000;
        internal const int BASE_RETRY_BACKOFF_MS_MAX = int.MaxValue;

        internal const int RETRY_BACKOFF_MS_DEFAULT = 10 * 1000;
        internal const int RETRY_BACKOFF_MS_MAX = int.MaxValue;

        internal static readonly TimeSpan LINGER_TIME_DEFAULT = TimeSpan.FromMilliseconds(2000);
        internal static readonly TimeSpan LINGER_TIME_MAX = TimeSpan.FromHours(1);

        private long _totalSizeInBytes = TOTAL_SIZE_IN_BYTES_DEFAULT;
        private int _senderCount = SENDER_COUNT_DEFAULT;
        private int _reservedAttempts = RESERVED_ATTEMPTS_DEFAUL;
        private int _batchCount = BATCH_COUNT_DEFAULT;
        private long _batchSize = BATCH_SIZE_DEFAULT;
        private int _baseRetryBackoffMs = BASE_RETRY_BACKOFF_MS_DEFAULT;
        private int _retryBackoffMs = RETRY_BACKOFF_MS_DEFAULT;
        private TimeSpan _lingerTime = LINGER_TIME_DEFAULT;

        public ILogger Logger { get; }
        public Client TlsClient { get; }
        public int Retries { get; set; } = 10;
        public int ShardCount { get; set; } = 2;
        public int BlockSec { get; set; } = 60;
        public bool AdjustShardHashFlag { get; set; } = true;
        public HashSet<int> NoRetryStatusCodeList { get; set; } = new HashSet<int> { 400, 404 };

        public long TotalSizeInBytes
        {
            get
            {
                return _totalSizeInBytes;
            }
            set
            {
                _totalSizeInBytes = Util.GetValueEnsureInRange(
                    valueToCheck: value,
                    min: 0,
                    max: TOTAL_SIZE_IN_BYTES_MAX,
                    defaultValue: TOTAL_SIZE_IN_BYTES_DEFAULT
                );
            }
        }

        public int SenderCount
        {
            get
            {
                return _senderCount;
            }
            set
            {
                _senderCount = Util.GetValueEnsureInRange(
                    valueToCheck: value,
                    min: 0,
                    max: SENDER_COUNT_MAX,
                    defaultValue: SENDER_COUNT_DEFAULT
                );
            }
        }

        public int ReservedAttempts
        {
            get
            {
                return _reservedAttempts;
            }
            set
            {
                _reservedAttempts = Util.GetValueEnsureInRange(
                    valueToCheck: value,
                    min: 0,
                    max: RESERVED_ATTEMPTS_MAX,
                    defaultValue: RESERVED_ATTEMPTS_DEFAUL
                );
            }
        }

        public int BatchCount
        {
            get
            {
                return _batchCount;
            }
            set
            {
                _batchCount = Util.GetValueEnsureInRange(
                    valueToCheck: value,
                    min: 0,
                    max: BATCH_COUNT_MAX,
                    defaultValue: BATCH_COUNT_DEFAULT
                );
            }
        }

        public long BatchSize
        {
            get
            {
                return _batchSize;
            }
            set
            {
                _batchSize = Util.GetValueEnsureInRange(
                    valueToCheck: value,
                    min: 0,
                    max: BATCH_SIZE_MAX,
                    defaultValue: BATCH_SIZE_DEFAULT
                );
            }
        }

        public int BaseRetryBackoffMs
        {
            get
            {
                return _baseRetryBackoffMs;
            }
            set
            {
                _baseRetryBackoffMs = Util.GetValueEnsureInRange(
                    valueToCheck: value,
                    min: 0,
                    max: BASE_RETRY_BACKOFF_MS_MAX,
                    defaultValue: BASE_RETRY_BACKOFF_MS_DEFAULT
                );
            }
        }

        public int RetryBackoffMs
        {
            get
            {
                return _retryBackoffMs;
            }
            set
            {
                _retryBackoffMs = Util.GetValueEnsureInRange(
                    valueToCheck: value,
                    min: 0,
                    max: RETRY_BACKOFF_MS_MAX,
                    defaultValue: RETRY_BACKOFF_MS_DEFAULT
                );
            }
        }

        public TimeSpan LingerTime
        {
            get
            {
                return _lingerTime;
            }
            set
            {
                _lingerTime = Util.GetValueEnsureInRange(
                    valueToCheck: value,
                    min: TimeSpan.FromMilliseconds(100),
                    max: LINGER_TIME_MAX,
                    defaultValue: LINGER_TIME_DEFAULT
                );
            }
        }

        public ProducerConfig(string region, string endpoint, string ak, string sk, string token = null)
            : this(new VolcengineConfig(region, endpoint, ak, sk, token))
        {

        }

        public ProducerConfig(VolcengineConfig config)
        {
            Logger = new Logger().GetDefaultLogger();
            TlsClient = new Client(config);
        }
    }
}
