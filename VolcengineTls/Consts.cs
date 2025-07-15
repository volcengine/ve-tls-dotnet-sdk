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

namespace VolcengineTls
{
    public static class Consts
    {
        /// <summary>
        /// SDK constants
        /// </summary>
        public const string SdkName = "ve-tls-dotnet-sdk";
        public const string SdkVersion = "v1.0.1";

        /// <summary>
        /// SDK Error constants
        /// </summary>
        public const string SdkErrorHttpRequest = "http request error";
        public const string SdkErrorMaxBlockSecTimeOut = "The size of accumulated logs has exceeded the setting of TotalSizeInBytes, and the duration has exceeded the configuration item MaxBlockSec.";

        /// <summary>
        /// Producer constants
        /// </summary>
        public const int ProducerStop = 0;
        public const int ProducerStart = 1;
        public const int ForceCloseNo = 0;
        public const int ForceCloseYes = 1;

        /// <summary>
        /// Header constants
        /// </summary>
        public const string HeaderContentType = "Content-Type";
        public const string HeaderAgent = "User-Agent";
        public const string HeaderContentMd5 = "Content-MD5";
        public const string ServiceName = "TLS";
        public const string HeaderRequestId = "x-tls-requestid";
        public const string HeaderXTlsHashKey = "x-tls-hashkey";
        public const string HeaderXTlsCompressType = "x-tls-compresstype";
        public const string HeaderXTlsBodyRawSize = "x-tls-bodyrawsize";
        public const string HeaderLogCount = "log-count";
        public const string HeaderEarliestLogTime = "earliest-log-time";
        public const string HeaderLatestLogTime = "latest-log-time";
        public const string HeaderAPIVersion = "x-tls-apiversion";
        public const string HeaderXSecurityToken = "X-Security-Token";
        public const string HeaderAuthorization = "Authorization";
        public const string HeaderHost = "Host";
        public const string HeaderXDate = "X-Date";
        public const string HeaderXContentSha256 = "X-Content-Sha256";
        public const string APIVersion2 = "0.2.0";
        public const string APIVersion3 = "0.3.0";

        /// <summary>
        /// Header constants
        /// </summary>
        public const string MimeTypeJson = "application/json";
        public const string MimeTypeProtobuf = "application/x-protobuf";

        /// <summary>
        /// compress type constants
        /// </summary>
        public const string CompressLz4 = "lz4";
        public const string CompressGz = "gzip";
        public const string CompressNoCompress = null;

        /// <summary>
        /// interface params constants
        /// </summary>
        public const string RequestId = "RequestId";
        public const string ProjectId = "ProjectId";
        public const string TopicId = "TopicId";

        /// <summary>
        /// interface name constants
        /// </summary>
        public const string ApiPathPutLogs = "/PutLogs";
    }
}
