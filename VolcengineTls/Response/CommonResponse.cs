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
using System.Linq;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Net.Http;

namespace VolcengineTls.Response
{
    [DataContract]
    public class CommonResponse
    {
        [DataMember(Name = Consts.RequestId)]
        public string RequestId { get; set; }

        public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public CommonResponse(HttpResponseMessage response)
        {
            if (response == null)
            {
                return;
            }

            AddHeaders(response.Headers);
            AddHeaders(response.Content.Headers);
            RequestId = GetHeaderByKey(Consts.HeaderRequestId);
        }

        private void AddHeaders(IEnumerable<KeyValuePair<string, IEnumerable<string>>> headers)
        {
            if (headers == null) return;

            foreach (var header in headers)
            {
                if (!Headers.ContainsKey(header.Key))
                {
                    Headers[header.Key] = header.Value.FirstOrDefault();
                }
            }
        }

        public string GetHeaderByKey(string key)
        {
            if (string.IsNullOrEmpty(key))
                return null;

            string value;
            return Headers.TryGetValue(key, out value) ? value : null;
        }

        public static T Deserialize<T>(byte[] data)
        {
            using (var stream = new MemoryStream(data))
            {
                var serializer = new DataContractJsonSerializer(typeof(T));
                return (T)serializer.ReadObject(stream);
            }
        }

        // public static CommonResponse DeserializeCommonResponse(byte[] data)
        // {
        //     return Deserialize<CommonResponse>(data);
        // }

        public override string ToString()
        {
            var headersStr = Headers != null
                ? string.Join(", ", Headers.Select(h => h.Key + ":" + h.Value))
                : string.Empty;

            return $"RequestId={RequestId}, Headers={headersStr}";
        }
    }
}
