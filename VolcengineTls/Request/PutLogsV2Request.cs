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

using System.Collections.Generic;

namespace VolcengineTls.Request
{
    public class PutLogsV2Request
    {
        public string TopicId { get; set; }
        public string HashKey { get; set; }
        public string CompressType { get; set; }
        public string Source { get; set; }
        public string FileName { get; set; }
        public List<Log> Logs { get; set; }

        public PutLogsV2Request()
        {

        }

        public PutLogsV2Request(string topicId, List<Log> logs)
        {
            TopicId = topicId;
            Logs = logs;
        }

        public PutLogsV2Request(string topicId, List<Log> logs, string compressType, string hashKey,
            string source, string fileName) : this(topicId, logs)
        {
            CompressType = compressType;
            HashKey = hashKey;
            Source = source;
            FileName = fileName;
        }

        public bool CheckValidation()
        {
            if (Logs.Count == 0)
            {
                return false;
            }

            return true;
        }
    }
}
