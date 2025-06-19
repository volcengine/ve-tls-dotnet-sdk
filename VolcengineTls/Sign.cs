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
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;

namespace VolcengineTls
{
    public class Sign
    {
        private readonly string _region;
        private readonly string _service;
        private readonly string _host;
        private readonly string _path;
        private readonly string _ak;
        private readonly string _sk;

        private static readonly Encoding Utf8 = Encoding.UTF8;

        public Sign(string service, string region, string endpoint, string path, string ak, string sk)
        {
            _region = region;
            _service = service;
            _path = path;
            _ak = ak;
            _sk = sk;

            Uri uri = new Uri(endpoint);
            _host = uri.Host;
        }

        public IDictionary<string, string> GetSignatureHeaders(
            HttpMethod method,
            string queryString,
            byte[] body,
            Dictionary<string, string> apiHeaders,
            DateTimeOffset date
        )
        {
            if (body == null)
                body = new byte[0];

            Dictionary<string, string> headersDict = new Dictionary<string, string>(apiHeaders);

            string contentType;
            if (!headersDict.TryGetValue(Consts.HeaderContentType, out contentType) ||
                string.IsNullOrWhiteSpace(contentType))
            {
                headersDict[Consts.HeaderContentType] = "application/x-www-form-urlencoded; charset=utf-8";
            }

            string xContentSha256 = ToHexString(HashSha256(body));
            string xDate = GetSignTime(date);
            string shortXDate = xDate.Substring(0, 8);

            // sign headers
            headersDict[Consts.HeaderHost] = _host;
            headersDict[Consts.HeaderXDate] = xDate;
            headersDict[Consts.HeaderXContentSha256] = xContentSha256;

            var sortHeaders = SortHeaders(headersDict);

            string signHeaderString = GetSignHeaderString(sortHeaders);
            string canonicalHeadersString = GetCanonicalHeadersString(sortHeaders, headersDict);

            // var realQueryList = new NameValueCollection();
            // queryList.ForEach(s => realQueryList.Add(s.Key, s.Value));

            // var sortedKeys = new SortedSet<string>(realQueryList.AllKeys.Where(k => k != null));
            // var queryBuilder = new StringBuilder();
            // bool isFirstParam = true;
            // foreach (var key in sortedKeys)
            // {
            //     var values = realQueryList.GetValues(key);
            //     if (values == null) continue;

            //     // 对值进行排序
            //     Array.Sort(values);

            //     foreach (var value in values)
            //     {
            //         // 添加连接符
            //         if (!isFirstParam)
            //             queryBuilder.Append("&");

            //         isFirstParam = false;

            //         // 添加编码后的键值对
            //         queryBuilder.AppendFormat(
            //             "{0}={1}",
            //             Uri.EscapeDataString(key),
            //             Uri.EscapeDataString(value)
            //         );
            //     }
            // }
            // string queryString = queryBuilder.ToString();

            string canonicalStringBuilder =
                $"{method}\n" +
                $"{_path}\n" +
                $"{queryString}\n" +
                $"{canonicalHeadersString}\n" +
                $"\n" +
                $"{signHeaderString}\n" +
                $"{xContentSha256}";

            string hashCanonicalString = ToHexString(HashSha256(Utf8.GetBytes(canonicalStringBuilder)));
            string credentialScope = $"{shortXDate}/{_region}/{_service}/request";
            string signString = $"HMAC-SHA256\n{xDate}\n{credentialScope}\n{hashCanonicalString}";

            byte[] signKey = GenSigningSecretKeyV4(_sk, shortXDate, _region, _service);
            string signature = ToHexString(HmacSha256(signKey, signString));

            var headers = new Dictionary<string, string>
            {
                { Consts.HeaderHost, _host },
                { Consts.HeaderXDate, xDate },
                { Consts.HeaderXContentSha256, xContentSha256 },
                { Consts.HeaderAuthorization, $"HMAC-SHA256 Credential={_ak}/{credentialScope}, SignedHeaders={signHeaderString}, Signature={signature}" },
            };

            return headers;
        }

        private string GetSignTime(DateTimeOffset date)
        {
            return date.UtcDateTime.ToString("yyyyMMdd'T'HHmmss'Z'");
        }

        private List<string> SortHeaders(Dictionary<string, string> headers)
        {
            var sortedHeaderKeys = new List<string>();

            foreach (var item in headers)
            {
                switch (item.Key)
                {
                    case Consts.HeaderContentType:
                    case Consts.HeaderContentMd5:
                    case Consts.HeaderHost:
                    case Consts.HeaderXSecurityToken:
                        break;
                    default:
                        if (!item.Key.StartsWith("X-", StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }
                        break;
                }

                sortedHeaderKeys.Add(item.Key.ToLowerInvariant());
            }

            sortedHeaderKeys.Sort(StringComparer.Ordinal);

            return sortedHeaderKeys;
        }

        private string GetSignHeaderString(List<string> sortHeaders)
        {
            return string.Join(";", sortHeaders);
        }

        private string GetCanonicalHeadersString(List<string> sortHeaders, Dictionary<string, string> headersDict)
        {
            var canonicalHeaderStringBuilder = new StringBuilder(string.Empty);

            var caseInsensitiveDict = new Dictionary<string, string>(headersDict, StringComparer.OrdinalIgnoreCase);

            foreach (var headerName in sortHeaders)
            {
                var headerValue = caseInsensitiveDict[headerName].Trim();

                if (headerName == Consts.HeaderHost)
                {
                    if (headerValue.Contains(":"))
                    {
                        var split = headerValue.Split(':');
                        var port = split[1];
                        if (port == "80" || port == "443")
                        {
                            headerValue = split[0];
                        }
                    }
                }

                canonicalHeaderStringBuilder.Append($"{headerName}:{headerValue}\n");
            }

            if (canonicalHeaderStringBuilder.Length > 0)
            {
                canonicalHeaderStringBuilder.Length--;
            }

            return canonicalHeaderStringBuilder.ToString();
        }

        private byte[] GenSigningSecretKeyV4(string secretKey, string date, string region, string service)
        {
            byte[] kDate = HmacSha256(Utf8.GetBytes(secretKey), date);
            byte[] kRegion = HmacSha256(kDate, region);
            byte[] kService = HmacSha256(kRegion, service);
            return HmacSha256(kService, "request");
        }

        private static byte[] HmacSha256(byte[] secret, string text)
        {
            using (HMACSHA256 mac = new HMACSHA256(secret))
            {
                var hash = mac.ComputeHash(Encoding.UTF8.GetBytes(text));
                return hash;
            }
        }

        private static byte[] HashSha256(byte[] data)
        {
            using (SHA256 sha = SHA256.Create())
            {
                var hash = sha.ComputeHash(data);
                return hash;
            }
        }

        private static string ToHexString(byte[] bytes)
        {
            if (bytes == null)
            {
                return "";
            }

            StringBuilder sb = new StringBuilder();
            foreach (var t in bytes)
            {
                sb.Append(t.ToString("X2"));
            }

            return sb.ToString().ToLower();
        }
    }
}
