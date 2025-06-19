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
using System.Diagnostics;
using System.Threading;
using NUnit.Framework;
using VolcengineTls.Producer;

namespace VolcengineTls.Tests.Producer
{
    public class DefaultCallBackImp : ICallBack
    {
        public void Fail(ProducerResult result)
        {
            foreach (var item in result.Attempts)
            {
                Debug.WriteLine($"producer failed, result is {item}");
            }
        }

        public void Success(ProducerResult result)
        {
            foreach (var item in result.Attempts)
            {
                Debug.WriteLine($"producer failed, result is {item}");
            }
        }
    }

    [TestFixture]
    public class ProducerTest : BaseTest
    {
        private ProducerConfig _defaultConfig;

        [OneTimeSetUp]
        public void InitTestSetUp()
        {
            GlobalSetup();

            _defaultConfig = new ProducerConfig(
                Region,
                Endpoint,
                AccessKeyId,
                AccessKeySecret,
                SecurityToken
            );
        }

        [Test]
        public void TestSendLogNormally()
        {
            var producer = new ProducerImp(_defaultConfig);
            producer.Start();

            var keyNum = 2;

            var log = new Pb.Log
            {
                Time = 0,
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

            producer.SendLog(
                topicId: Environment.GetEnvironmentVariable("TOPIC_ID"),
                log: log,
                source: null,
                filename: null,
                shardHash: null,
                callback: null
            );

            Thread.Sleep(TimeSpan.FromSeconds(5));

            producer.Close();
        }

        [Test]
        public void TestSendLogsNormally()
        {
            var producer = new ProducerImp(_defaultConfig);
            producer.Start();

            var logNum = 1000000;
            var keyNum = 2;

            var logGroup = new Pb.LogGroup();

            for (var j = 0; j < logNum; j++)
            {
                var log = new Pb.Log
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
                source: TEST_SOURCE,
                filename: TEST_FILENAME,
                shardHash: null,
                callback: null
            );

            Thread.Sleep(TimeSpan.FromSeconds(20));

            producer.Close();
        }

        [Test]
        public void TestSendLogV2Normally()
        {
            var producer = new ProducerImp(_defaultConfig);
            producer.Start();

            var log = new Log
            {
                Time = DateTimeOffset.Now.ToUnixTimeSeconds(),
                Contents = new List<LogContent>
                {
                    new LogContent("key1", "value1"),
                    new LogContent("key2", "value2"),
                }
            };

            producer.SendLogV2(
                topicId: Environment.GetEnvironmentVariable("TOPIC_ID"),
                log: log,
                source: TEST_SOURCE,
                filename: TEST_FILENAME,
                shardHash: null,
                callback: new DefaultCallBackImp()
            );

            Thread.Sleep(TimeSpan.FromSeconds(5));

            producer.Close();
        }

        [Test]
        public void TestSendLogsV2Normally()
        {
            var producer = new ProducerImp(_defaultConfig);
            producer.Start();

            var logNum = 1000000;
            var keyNum = 2;

            var logGroup = new List<Log>();

            for (var i = 0; i < logNum; i++)
            {
                var log = new Log
                {
                    Time = DateTimeOffset.Now.ToUnixTimeSeconds(),
                    Contents = new List<LogContent>(),
                };

                for (var j = 0; j < keyNum; j++)
                {
                    var logContent = new LogContent($"no-{i}-key{j}", $"no-{i}-c#-value-test-{j}");
                    log.Contents.Add(logContent);
                }

                logGroup.Add(log);
            }

            producer.SendLogsV2(
                topicId: Environment.GetEnvironmentVariable("TOPIC_ID"),
                logs: logGroup,
                source: TEST_SOURCE,
                filename: TEST_FILENAME,
                shardHash: null,
                contextFlow: null,
                callback: null
            );

            Thread.Sleep(TimeSpan.FromSeconds(20));

            producer.Close();
        }

        [Test]
        public void TestSendLogV2NormallyWithMultipleStartStop()
        {
            for (var i = 0; i < 10; i++)
            {
                TestSendLogsNormally();
            }
        }

        [Test]
        public void TestSendLogUnNormally()
        {
            var producer = new ProducerImp(_defaultConfig);
            producer.Start();

            var keyNum = 2;

            var log = new Pb.Log
            {
                Time = 0,
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

            producer.SendLog(
                topicId: "test-topicId-not-exists",
                log: log,
                source: null,
                filename: null,
                shardHash: null,
                callback: null
            );

            Thread.Sleep(TimeSpan.FromSeconds(1));

            producer.Close();

            Assert.AreEqual(0, ProducerImp._producerLogGroupSize);
        }
    }
}
