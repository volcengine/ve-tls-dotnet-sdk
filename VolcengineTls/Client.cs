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
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Google.Protobuf;
using Pb;
using VolcengineTls.Error;
using VolcengineTls.Request;
using VolcengineTls.Response;

namespace VolcengineTls
{
    public class Client : IClient
    {
        private HttpClient _defaultHttpClient = new HttpClient(new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.None,
        })
        {
            Timeout = TimeSpan.FromSeconds(60)
        };
        private static readonly string _defaultUserAgent = $"{Consts.SdkName}/{Consts.SdkVersion}";

        public VolcengineConfig Config { get; private set; }

        public HttpClient HttpClient
        {
            get
            {
                return _defaultHttpClient;
            }
            set
            {
                _defaultHttpClient = value;
            }
        }
        public string APIVersion { get; set; } = Consts.APIVersion3;
        public string CustomUserAgent { get; private set; }

        public Client(string region, string endpoint, string ak, string sk, string token = null) : this(new VolcengineConfig(region, endpoint, ak, sk, token))
        {

        }

        public Client(VolcengineConfig config)
        {
            Config = config;

            ServicePointManager.DefaultConnectionLimit = 1000;
            ServicePointManager.MaxServicePointIdleTime = 10 * 1000;
        }

        public void ConfigClient(VolcengineConfig config)
        {
            Config = config;
        }

        public VolcengineConfig GetVolcengineConfig()
        {
            return Config;
        }

        public void ResetAccessKeyToken(string accessKeyId, string accessKeySecret, string securityToken)
        {
            Config.ResetAccessKeyToken(accessKeyId, accessKeySecret, securityToken);
        }

        public async Task<HttpResponseMessage> Request(HttpMethod method, string apiPath, Dictionary<string, string> paramsDict = null,
            Dictionary<string, string> headers = null, byte[] body = null)
        {
            if (string.IsNullOrEmpty(Config.Region))
                throw new InvalidOperationException("Empty Region; 请在初始化时填入 Region");

            if (string.IsNullOrEmpty(Config.Endpoint))
                throw new InvalidOperationException("Empty Endpoint; 请在初始化时填入 Endpoint");

            string queryString = String.Empty;
            if (paramsDict != null && paramsDict.Count > 0)
            {
                queryString = HttpQueryBuild(paramsDict);
            }

            string relativeUri = (queryString != String.Empty)
                ? $"{apiPath}?{queryString}"
                : apiPath;

            var requestUri = Config.Endpoint.TrimEnd('/') + relativeUri;

            if (headers == null)
                headers = new Dictionary<string, string>();

            string value;
            if (!headers.TryGetValue(Consts.HeaderContentType, out value))
            {
                headers.Add(Consts.HeaderContentType, Consts.MimeTypeJson);
            }

            if (!string.IsNullOrWhiteSpace(Config.SecurityToken))
            {
                headers.Add(Consts.HeaderXSecurityToken, Config.SecurityToken);
            }

            return await RetryHandler.ExecuteWithRetryAsync(
                () => RealRequest(method, requestUri, headers, queryString, body)
            );
        }

        private string HttpQueryBuild(Dictionary<string, string> paramsDict)
        {
            var urlBuilder = new StringBuilder(String.Empty);

            bool isFirstParam = true;

            var sortedParams = new SortedDictionary<string, string>();
            foreach (var item in paramsDict)
            {
                sortedParams.Add(item.Key, item.Value);
            }

            foreach (var item in sortedParams)
            {
                if (!isFirstParam)
                {
                    urlBuilder.Append("&");
                }

                isFirstParam = false;

                urlBuilder.AppendFormat(
                    "{0}={1}",
                    Uri.EscapeDataString(item.Key),
                    Uri.EscapeDataString(item.Value)
                );
            }

            return urlBuilder.ToString();
        }

        private async Task<HttpResponseMessage> RealRequest(HttpMethod method, string requestUri, Dictionary<string, string> headers,
            string queryString, byte[] body)
        {
            var request = new HttpRequestMessage(method, requestUri)
            {
                Content = new ByteArrayContent(body)
            };

            if (string.IsNullOrEmpty(CustomUserAgent))
                headers[Consts.HeaderAgent] = _defaultUserAgent;
            else
                headers[Consts.HeaderAgent] = CustomUserAgent;

            // 如果header没有配置api version，增加默认的api version 0.3.0
            if (!headers.ContainsKey(Consts.HeaderAPIVersion))
                headers[Consts.HeaderAPIVersion] = APIVersion;

            if (body != null)
                headers[Consts.HeaderContentMd5] = ComputeMd5(body);

            var sign = new Sign(
                service: Consts.ServiceName,
                region: Config.Region,
                endpoint: Config.Endpoint,
                path: request.RequestUri.AbsolutePath,
                ak: Config.AccessKeyId,
                sk: Config.AccessKeySecret
            );

            var signatureHeaders = sign.GetSignatureHeaders(
                method: method,
                queryString: queryString,
                body: body,
                apiHeaders: headers,
                date: DateTimeOffset.Now
            );

            foreach (var item in signatureHeaders)
            {
                headers.Add(item.Key, item.Value);
            }

            foreach (var item in headers)
            {
                if (!request.Headers.TryAddWithoutValidation(item.Key, item.Value))
                {
                    request.Content?.Headers.TryAddWithoutValidation(item.Key, item.Value);
                }
            }

            var resp = await HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

            if (resp.StatusCode != HttpStatusCode.OK)
            {
                string bodyResponse = await resp.Content.ReadAsStringAsync();

                string requestId = "";
                IEnumerable<string> values;
                if (resp.Headers.TryGetValues(Consts.HeaderRequestId, out values))
                    requestId = values.FirstOrDefault() ?? "";

                throw new TlsError(
                    httpcode: (int)resp.StatusCode,
                    errorCode: Consts.SdkErrorHttpRequest,
                    errorMessage: bodyResponse,
                    requestId: requestId
                );
            }

            return resp;
        }

        private string ComputeMd5(byte[] data)
        {
            using (var md5 = MD5.Create())
            {
                var hash = md5.ComputeHash(data);
                return BitConverter.ToString(hash).Replace("-", "").ToUpperInvariant();
            }
        }

        public void Close()
        {
            HttpClient.Dispose();
        }

        public override string ToString()
        {
            return $"{Config.Endpoint} {Config.Region}";
        }

        /// <summary>
        /// 调用 PutLogs 接口上传日志到指定的日志主题中
        /// 文档：https://www.volcengine.com/docs/6470/112191
        /// </summary>
        /// <param name="request">上传日志请求，包含日志内容、主题ID等信息</param>
        /// <returns>日志上传响应结果</returns>
        /// <exception cref="TlsError">异常场景下会抛出</exception>
        /// <exception cref="Exception">异常场景下会抛出</exception>
        public async Task<PutLogsResponse> PutLogs(PutLogsRequest request)
        {
            if (request == null || !request.CheckValidation())
            {
                throw new TlsError("Invalid request params, Please check it");
            }

            int logCnt = 0;
            long maxLogTime = long.MinValue;
            long minLogTime = long.MaxValue;

            foreach (var logGroup in request.LogGroupList.LogGroups)
            {
                var logs = logGroup.Logs;
                for (int i = 0; i < logs.Count; i++)
                {
                    var log = logs[i];
                    long time = log.Time;
                    long normalizedTime;

                    if (time <= 0)
                    {
                        log.Time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                        normalizedTime = log.Time;
                    }
                    else if (time < 1e10) // 秒级时间戳
                    {
                        normalizedTime = time * 1000;
                    }
                    else if (time < 1e15) // 毫秒级时间戳
                    {
                        normalizedTime = time;
                    }
                    else // 纳秒级时间戳
                    {
                        normalizedTime = time / 1000000;
                    }

                    maxLogTime = Math.Max(maxLogTime, normalizedTime);
                    minLogTime = Math.Min(minLogTime, normalizedTime);
                    logCnt++;
                }
            }

            if (logCnt == 0)
                throw new TlsError("Invalid log num, Please check it");

            var queryParams = new Dictionary<string, string>
            {
                [Consts.TopicId] = request.TopicId
            };

            var headers = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(request.HashKey))
                headers[Consts.HeaderXTlsHashKey] = request.HashKey;

            var compressType = request.CompressType;

            byte[] bodyBytes;

            try
            {
                bodyBytes = request.LogGroupList.ToByteArray();
            }
            catch (Exception e)
            {
                throw new TlsError($"Protobuf Serialization failed: {e.Message}");
            }

            byte[] compressBodybytes = request.GetCompressBodyBytes(bodyBytes);

            if (compressType != null)
            {
                headers.Add(Consts.HeaderXTlsCompressType, compressType);
                headers.Add(Consts.HeaderXTlsBodyRawSize, bodyBytes.Length.ToString());
            }

            headers.Add(Consts.HeaderContentType, Consts.MimeTypeProtobuf);
            headers.Add(Consts.HeaderLogCount, logCnt.ToString());
            headers.Add(Consts.HeaderEarliestLogTime, minLogTime.ToString());
            headers.Add(Consts.HeaderLatestLogTime, maxLogTime.ToString());

            var response = await Request(
                method: HttpMethod.Post,
                apiPath: Consts.ApiPathPutLogs,
                paramsDict: queryParams,
                headers: headers,
                body: compressBodybytes
            );

            return new PutLogsResponse(response);
        }

        public async Task<PutLogsResponse> PutLogsV2(PutLogsV2Request request)
        {
            if (request == null || !request.CheckValidation())
            {
                throw new TlsError("Invalid request params, Please check it");
            }

            var logGroupList = new Pb.LogGroupList();
            var logGroup = new LogGroup
            {
                Source = request.Source,
                FileName = request.FileName,
            };

            for (int idx = 0; idx < request.Logs.Count; idx++)
            {
                var log = new Pb.Log
                {
                    Time = request.Logs[idx].Time
                };

                foreach (var item in request.Logs[idx].Contents)
                {
                    log.Contents.Add(new Pb.LogContent
                    {
                        Key = item.Key,
                        Value = item.Value,
                    });
                }

                logGroup.Logs.Add(log);
            }

            logGroupList.LogGroups.Add(logGroup);

            var putLogsRequest = new PutLogsRequest
            {
                TopicId = request.TopicId,
                HashKey = request.HashKey,
                CompressType = request.CompressType,
                LogGroupList = logGroupList,
            };

            return await PutLogs(putLogsRequest);
        }
    }
}
