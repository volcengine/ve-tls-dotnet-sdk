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
using System.Net.Http;
using System.Security.Cryptography;
using Google.Protobuf;
using VolcengineTls.Request;

namespace VolcengineTls.Tests
{
    [TestFixture]
    public class SignTest : BaseTest
    {
        [Test]
        public void TestSign()
        {
            var sign = new Sign(
                region: Region,
                service: Consts.ServiceName,
                endpoint: Endpoint,
                path: Consts.ApiPathPutLogs,
                ak: AccessKeyId,
                sk: AccessKeySecret
            );

            int logNum = 10;

            byte[] bodyBytes = CreateTestLogGroupList(logNum, 1750733700).ToByteArray();
            // 计算压缩
            var putLogsRequest = new PutLogsRequest
            {
                CompressType = null,
            };

            byte[] compressBodyBytes = putLogsRequest.GetCompressBodyBytes(bodyBytes);

            string bodyMd5 = string.Empty;
            using (var md5 = MD5.Create())
            {
                var hash = md5.ComputeHash(bodyBytes);
                bodyMd5 = BitConverter.ToString(hash).Replace("-", "").ToUpperInvariant();
            }

            var headers = new Dictionary<string, string>
            {
                { Consts.HeaderContentType, Consts.MimeTypeProtobuf },
                { Consts.HeaderLogCount, $"{logNum}" },
                { Consts.HeaderEarliestLogTime, "1750733700" },
                { Consts.HeaderLatestLogTime, "1750733700" },
                { Consts.HeaderXTlsCompressType, "none" },
                { Consts.HeaderXTlsBodyRawSize, bodyBytes.Length.ToString() },
                { Consts.HeaderAgent, $"{Consts.SdkName}/{Consts.SdkVersion}" },
                { Consts.HeaderAPIVersion,  Consts.APIVersion3 },
                { Consts.HeaderContentMd5, bodyMd5 },
                { Consts.HeaderXTlsHashKey, string.Empty }
            };

            var signHeaders = sign.GetSignatureHeaders(
                method: HttpMethod.Post,
                queryString: $"{Consts.TopicId}={Environment.GetEnvironmentVariable("TOPIC_ID")}",
                body: bodyBytes,
                apiHeaders: headers,
                date: DateTimeOffset.Parse("2025-06-24T10:55:00+08:00")
            );

            string authorization = signHeaders[Consts.HeaderAuthorization];
            string signature = authorization.Split(new[] { "Signature=" }, StringSplitOptions.None)[1].Trim();

            // 验证host
            Assert.AreEqual(new Uri(Endpoint).Host, signHeaders[Consts.HeaderHost]);
            // 验证x-date
            Assert.AreEqual("20250624T025500Z", signHeaders[Consts.HeaderXDate]);
            // 验证x-content-sha256
            Assert.AreEqual("df02307d61cc33dd2f3c314003a45d749321393080537b5a22f65ca35d557940", signHeaders[Consts.HeaderXContentSha256]);
            // 验证签名
            Assert.AreEqual("e037fd5726f4bc0e8f39b3b6c1fb7f5cf43bc1bb7be9ebb8d9a771d7cd0ecd24", signature);
        }
    }
}
