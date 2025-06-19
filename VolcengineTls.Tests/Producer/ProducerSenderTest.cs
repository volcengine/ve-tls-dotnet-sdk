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
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using VolcengineTls.Error;
using VolcengineTls.Producer;

namespace VolcengineTls.Tests.Producer
{
    [TestFixture]
    public class ProducerSenderTest : BaseTest
    {
        private ProducerConfig _defaultConfig;

        private ProducerSender _sender;

        private string topicId;

        [OneTimeSetUp]
        public void TestSetUp()
        {
            GlobalSetup();

            _defaultConfig = new ProducerConfig(
                Region,
                Endpoint,
                AccessKeyId,
                AccessKeySecret,
                SecurityToken
            );

            _sender = new ProducerSender(_defaultConfig, new ProducerRetryQueue(_defaultConfig.Logger));

            topicId = Environment.GetEnvironmentVariable("TOPIC_ID");
        }

        [OneTimeTearDown]
        public void TestTearDown()
        {
            _sender.Close();
        }

        [Test]
        public async Task TestHandleBatchAsync()
        {
            var batch = GetDefaultProducerBatch(topicId, _defaultConfig);

            await _sender.HandleBatchAsync(batch);

            Assert.True(batch.Result.SuccessFlag);
            Assert.AreEqual(-batch.TotalDataSize, ProducerImp._producerLogGroupSize);
            Assert.AreEqual(0, batch.AttemptCount);
            Assert.AreEqual(0, batch.RetryBackoffMs);
        }

        [Test]
        public void TestHandleFailureWithRetry()
        {
            var batch = GetDefaultProducerBatch("not-exists-topicId", _defaultConfig);

            var mockError = new TlsError(429, "too many requests", "too many requests", "mock-request-id");

            _sender.HandleFailure(batch, mockError);

            Assert.AreEqual(1, batch.AttemptCount);
            Assert.False(batch.Result.SuccessFlag);
            Assert.GreaterOrEqual(batch.RetryBackoffMs, batch.BaseIncreaseRetryBackoffMs);
            Assert.LessOrEqual(batch.RetryBackoffMs, batch.MaxRetryIntervalInMs);

            Thread.Sleep(TimeSpan.FromSeconds(1));

            var retryBatchList = _sender._retryQueue.GetRetryBatch(false);
            Assert.AreEqual(1, retryBatchList.Count);
        }
    }
}
