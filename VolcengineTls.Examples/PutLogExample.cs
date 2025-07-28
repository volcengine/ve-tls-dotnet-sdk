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
using System.Threading.Tasks;
using VolcengineTls.Request;
using VolcengineTls.Response;

namespace VolcengineTls.Examples
{
    public class PutLogsExample
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

            // 构建日志数据
            var logGroupList = new Pb.LogGroupList();
            var logGroup = new Pb.LogGroup();

            // 单个logGroup不超过10000条
            var num = 100;
            while (num > 0)
            {
                var log = new Pb.Log
                {
                    Time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                };

                var logContents = new List<Pb.LogContent>
                {
                    new Pb.LogContent { Key = "k1", Value = $"value{num}" },
                    new Pb.LogContent { Key = "k2", Value = $"value{num}" }
                };

                log.Contents.Add(logContents);

                logGroup.Logs.Add(log);

                num--;
            }

            logGroupList.LogGroups.Add(logGroup);

            // 构建请求
            var putLogsRequest = new PutLogsRequest(
                logGroupList: logGroupList,
                topicId: Environment.GetEnvironmentVariable("TOPIC_ID")
            );

            PutLogsResponse putLogsResponse = await client.PutLogs(putLogsRequest);
        }
    }
}
