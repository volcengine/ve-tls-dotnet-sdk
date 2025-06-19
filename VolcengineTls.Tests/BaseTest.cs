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

using NUnit.Framework;
using Pb;
using System;
using System.Collections.Generic;
using VolcengineTls.Producer;

namespace VolcengineTls.Tests
{
    [TestFixture]
    public class BaseTest
    {
        protected static string Region { get; private set; }
        protected static string Endpoint { get; private set; }
        protected static string AccessKeyId { get; private set; }
        protected static string AccessKeySecret { get; private set; }
        protected static string SecurityToken { get; private set; }
        protected static Client Client { get; private set; }

        protected const string TEST_SOURCE = "cSharpSdkTestSource";
        protected const string TEST_FILENAME = "cSharpSdkTestFilename";

        [OneTimeSetUp]
        public void GlobalSetup()
        {
            InitializeEnvironmentVariables();

            Console.WriteLine("Initializing client for test...");

            var config = new VolcengineConfig(
                region: Region,
                endpoint: Endpoint,
                accessKeyId: AccessKeyId,
                accessKeySecret: AccessKeySecret,
                securityToken: SecurityToken
            );

            Client = new Client(config);

            Console.WriteLine("Client initialized");
        }

        protected static void InitializeEnvironmentVariables()
        {
            Region = Environment.GetEnvironmentVariable("VOLCENGINE_REGION");
            Endpoint = Environment.GetEnvironmentVariable("VOLCENGINE_ENDPOINT");
            AccessKeyId = Environment.GetEnvironmentVariable("VOLCENGINE_ACCESS_KEY_ID");
            AccessKeySecret = Environment.GetEnvironmentVariable("VOLCENGINE_ACCESS_KEY_SECRET");
            SecurityToken = Environment.GetEnvironmentVariable("VOLCENGINE_SECURITY_TOKEN");

            Assert.That(Region, Is.Not.Null, "VOLCENGINE_REGION 环境变量未设置");
            Assert.That(Endpoint, Is.Not.Null, "VOLCENGINE_ENDPOINT 环境变量未设置");
            Assert.That(AccessKeyId, Is.Not.Null, "VOLCENGINE_ACCESS_KEY_ID 环境变量未设置");
            Assert.That(AccessKeySecret, Is.Not.Null, "VOLCENGINE_ACCESS_KEY_SECRET 环境变量未设置");
        }

        #region 辅助方法

        protected List<Log> CreateTestLogs(int num = 100, long specificTime = 0)
        {
            long logTime;
            var logs = new List<Log> { };

            while (num > 0)
            {
                if (specificTime == 0)
                {
                    logTime = DateTimeOffset.Now.ToUnixTimeSeconds() + num;

                    if (num == 1)
                    {
                        logTime = 0;
                    }
                }
                else
                {
                    logTime = specificTime;
                }

                var log = new Log(logTime, new List<LogContent>{
                    new LogContent("k1", $"value{num}"),
                    new LogContent("k2", $"value{num}"),
                });

                logs.Add(log);

                num--;
            }

            return logs;
        }

        protected LogGroupList CreateTestLogGroupList(int num = 100, long specificTime = 0)
        {
            var logGroupList = new Pb.LogGroupList();
            var logGroup = new Pb.LogGroup
            {
                Source = "c-shard-sdk",
                FileName = "c-shard-fn",
            };

            long logTime;

            while (num > 0)
            {
                if (specificTime == 0)
                {
                    logTime = DateTimeOffset.Now.ToUnixTimeSeconds() + num;

                    if (num == 1)
                    {
                        logTime = 0;
                    }
                }
                else
                {
                    logTime = specificTime;
                }

                var log = new Pb.Log
                {
                    Time = logTime,
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

            return logGroupList;
        }

        protected LogGroupList CreateTestLogGroupListWithOutLogContent()
        {
            var logGroupList = new Pb.LogGroupList();
            var logGroup = new Pb.LogGroup
            {
                Source = "c-shard-sdk",
                FileName = "c-shard-fn",
            };
            logGroupList.LogGroups.Add(logGroup);
            return logGroupList;
        }

        protected ProducerBatch GetDefaultProducerBatch(string topicId, ProducerConfig config)
        {
            var keyNum = 2;

            var log = new Pb.Log
            {
                Time = DateTimeOffset.Now.ToUnixTimeSeconds(),
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

            var batchLog = new ProducerBatchLog()
            {
                Key = new ProducerBatchLogKey()
                {
                    Source = TEST_SOURCE,
                    FileName = TEST_FILENAME,
                    TopicId = topicId,
                },
                Log = log,
            };

            return new ProducerBatch(batchLog, config);
        }

        #endregion
    }
}
