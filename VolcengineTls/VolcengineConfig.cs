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

using System.Threading;

namespace VolcengineTls
{
    public class VolcengineConfig
    {
        private readonly ReaderWriterLockSlim _rwLockSlim = new ReaderWriterLockSlim();

        public string Region { get; set; }
        public string Endpoint { get; set; }
        public string AccessKeyId { get; private set; }
        public string AccessKeySecret { get; private set; }
        public string SecurityToken { get; private set; }

        public VolcengineConfig(string region, string endpoint, string accessKeyId, string accessKeySecret, string securityToken)
        {
            Region = region;
            Endpoint = endpoint;
            AccessKeyId = accessKeyId;
            AccessKeySecret = accessKeySecret;
            SecurityToken = securityToken;
        }

        public VolcengineConfig(string region, string endpoint, string accessKeyId, string accessKeySecret)
            : this(region, endpoint, accessKeyId, accessKeySecret, null)
        {
        }

        public void ResetAccessKeyToken(string accessKeyID, string accessKeySecret, string securityToken)
        {
            _rwLockSlim.EnterWriteLock();
            try
            {
                AccessKeyId = accessKeyID;
                AccessKeySecret = accessKeySecret;
                SecurityToken = securityToken;
            }
            finally
            {
                _rwLockSlim.ExitWriteLock();
            }

        }
    }
}
