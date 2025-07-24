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
using System.Threading;
using VolcengineTls.Producer;

namespace VolcengineTls.Examples
{
    public class DefaultCallBackImp : ICallBack
    {
        public void Fail(ProducerResult result)
        {
            Console.WriteLine("put log to tls failed");
        }

        public void Success(ProducerResult result)
        {
            Console.WriteLine("put log to tls success");
        }
    }

    public class ProducerExample
    {
        public void Run()
        {
            // 设置火山云权限配置
            var vc = new VolcengineConfig(
                region: Environment.GetEnvironmentVariable("VOLCENGINE_REGION"),
                endpoint: Environment.GetEnvironmentVariable("VOLCENGINE_ENDPOINT"),
                accessKeyId: Environment.GetEnvironmentVariable("VOLCENGINE_ACCESS_KEY_ID"),
                accessKeySecret: Environment.GetEnvironmentVariable("VOLCENGINE_ACCESS_KEY_SECRET"),
                securityToken: Environment.GetEnvironmentVariable("VOLCENGINE_SECURITY_TOKEN")
            );

            // 设置火山云配置，当前获取默认配置
            var config = new ProducerConfig(vc);
            // 实例化
            var producer = new ProducerImp(config);
            // 启动消费者
            producer.Start();

            /// 发送单挑
            // 构造日志
            var keyNum = 2;
            var log = new Pb.Log
            {
                Time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            };

            for (var i = 0; i < keyNum; i++)
            {
                var logContent = new Pb.LogContent
                {
                    Key = $"key{i}",
                    Value = $"c#-value-test-{i}",
                };

                log.Contents.Add(logContent);
            }

            // 发送单条 或者使用SendLogV2
            producer.SendLog(
                topicId: Environment.GetEnvironmentVariable("TOPIC_ID"),
                log: log,
                source: "your log source",
                filename: "your log filename",
                shardHash: null,
                callback: new DefaultCallBackImp() // 不需要的话可以传null
            );

            /// 发送多条
            var logNum = 1000;
            var logGroup = new Pb.LogGroup();

            for (var j = 0; j < logNum; j++)
            {
                log = new Pb.Log
                {
                    Time = DateTimeOffset.Now.ToUnixTimeSeconds(),
                };

                for (var i = 0; i < keyNum; i++)
                {
                    var logContent = new Pb.LogContent
                    {
                        Key = $"no-{j}-key{i}",
                        Value = $"no-{j}-c#-value-test-{i}",
                    };

                    log.Contents.Add(logContent);
                }

                logGroup.Logs.Add(log);
            }

            producer.SendLogs(
                topicId: Environment.GetEnvironmentVariable("TOPIC_ID"),
                logs: logGroup,
                source: "your log source",
                filename: "your log filename",
                shardHash: null,
                callback: new DefaultCallBackImp() // 不需要的话可以传null
            );

            // 模拟其他任务处理
            Thread.Sleep(TimeSpan.FromSeconds(20));

            // 生产者关闭
            producer.Close();
        }
    }
}
