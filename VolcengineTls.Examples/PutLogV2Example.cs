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
using System.Threading.Tasks;
using VolcengineTls.Request;
using VolcengineTls.Response;

namespace VolcengineTls.Examples
{
    public class PutLogsV2Example
    {
        public async Task RunAsync()
        {
            // 设置火山云权限配置
            var vcConfig = new VolcengineConfig(
                region: Environment.GetEnvironmentVariable("VOLCENGINE_REGION"),
                endpoint: Environment.GetEnvironmentVariable("VOLCENGINE_ENDPOINT"),
                accessKeyId: Environment.GetEnvironmentVariable("VOLCENGINE_ACCESS_KEY_ID"),
                accessKeySecret: Environment.GetEnvironmentVariable("VOLCENGINE_ACCESS_KEY_SECRET"),
                securityToken: Environment.GetEnvironmentVariable("VOLCENGINE_SECURITY_TOKEN")
            );

            // 实例化tls sdk client
            var client = new Client(vcConfig);

            /// 构建日志数据（封装的Pb数据）
            var logs = new List<Log>();
            // 单个logGroup不超过10000条
            var num = 100;
            while (num > 0)
            {
                var log = new Log(
                    time: DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    contents: new List<LogContent>{
                        new LogContent("k1", $"value{num}"),
                        new LogContent("k2", $"value{num}"),
                    }
                );

                logs.Add(log);

                num--;
            }

            // 构建请求
            var putLogsV2Request = new PutLogsV2Request(
                logs: logs,
                topicId: Environment.GetEnvironmentVariable("TOPIC_ID"),
                compressType: Consts.CompressLz4,
                hashKey: "",
                source: "your log source",
                fileName: "your log filename"
            );

            PutLogsResponse putLogsV2Response = await client.PutLogsV2(putLogsV2Request);
        }
    }
}
