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

using NUnit.Framework;

namespace VolcengineTls.Tests
{
    [TestFixture]
    public class ClientTest : BaseTest
    {
        [Test]
        public void TestNewClient()
        {
            var config = new VolcengineConfig(
                region: Region,
                endpoint: Endpoint,
                accessKeyId: AccessKeyId,
                accessKeySecret: AccessKeySecret,
                securityToken: SecurityToken
            );

            var client = new Client(config);
            var clientConfig = client.Config;

            Assert.AreEqual(Region, clientConfig.Region);
            Assert.AreEqual(Endpoint, clientConfig.Endpoint);
            Assert.AreEqual(AccessKeyId, clientConfig.AccessKeyId);
            Assert.AreEqual(AccessKeySecret, clientConfig.AccessKeySecret);
            Assert.AreEqual(SecurityToken ?? "", clientConfig.SecurityToken);
        }
    }
}
