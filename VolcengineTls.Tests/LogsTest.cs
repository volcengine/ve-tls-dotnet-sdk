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
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VolcengineTls.Error;
using VolcengineTls.Request;
using VolcengineTls.Response;

namespace VolcengineTls.Tests
{
    [TestFixture]
    public class LogsTest : BaseTest
    {
        [Test]
        public void TestPutLogsValidRequestUnNormally()
        {
            var testCases = new[]
            {
                new Dictionary<string, object>
                {
                    { "Name", "Invalid topic" },
                    { "Request", new PutLogsRequest(CreateTestLogGroupList(), "") },
                    { "ExpectedResult", "Invalid request params, Please check it" }
                },
                new Dictionary<string, object>
                {
                    { "Name", "Null LogGroupList" },
                    { "Request", new PutLogsRequest(null, "valid-topic") },
                    { "ExpectedResult", "Invalid request params, Please check it" }
                },
                new Dictionary<string, object>
                {
                    { "Name", "logCnt is 0" },
                    { "Request", new PutLogsRequest(CreateTestLogGroupListWithOutLogContent(), "valid-topic") },
                    { "ExpectedResult", "Invalid log num, Please check it" }
                }
            };

            foreach (var testCase in testCases)
            {
                var name = (string)testCase["Name"];
                var request = (PutLogsRequest)testCase["Request"];
                var expectedResult = (string)testCase["ExpectedResult"];

                var ex = Assert.ThrowsAsync<TlsError>(
                    async () => await Client.PutLogs(request)
                );

                Assert.AreEqual(expectedResult, ex.Message, name);
            }
        }

        [Test]
        public async Task TestPutLogsValidRequestNormally()
        {
            var putLogsRequest = new PutLogsRequest
            {
                TopicId = Environment.GetEnvironmentVariable("TOPIC_ID"),
                LogGroupList = CreateTestLogGroupList(10),
            };

            PutLogsResponse putLogsResponse = await Client.PutLogs(putLogsRequest);

            Assert.IsNotNull(putLogsResponse.RequestId);
        }

        [Test]
        public async Task TestPutLogsV2ValidRequestNormally()
        {
            var putLogsV2Request = new PutLogsV2Request
            {
                TopicId = Environment.GetEnvironmentVariable("TOPIC_ID"),
                Logs = CreateTestLogs(10),
                Source = "c-shard-sdk",
                FileName = "c-shard-fn",
            };

            PutLogsResponse putLogsResponse = await Client.PutLogsV2(putLogsV2Request);

            Assert.IsNotNull(putLogsResponse.RequestId);
        }
    }
}
