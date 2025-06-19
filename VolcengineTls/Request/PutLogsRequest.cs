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
using K4os.Compression.LZ4;
using Pb;
using VolcengineTls.Error;

namespace VolcengineTls.Request
{
    public class PutLogsRequest
    {
        public LogGroupList LogGroupList { get; set; }
        public string TopicId { get; set; }
        public string HashKey { get; set; }
        public string CompressType { get; set; } = Consts.CompressLz4;

        public PutLogsRequest()
        {
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="logGroupList">日志列表</param>
        /// <param name="topicId">日志主题 ID</param>
        public PutLogsRequest(LogGroupList logGroupList, string topicId)
        {
            LogGroupList = logGroupList;
            TopicId = topicId;
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="logGroupList">日志列表</param>
        /// <param name="topicId">日志主题 ID</param>
        /// <param name="hashKey">路由 Shard 的 key</param>
        /// <param name="compressType">压缩格式，支持 lz4、zlib</param>
        public PutLogsRequest(LogGroupList logGroupList, string topicId, string hashKey, string compressType)
        {
            LogGroupList = logGroupList;
            TopicId = topicId;
            HashKey = hashKey;
            CompressType = compressType;
        }

        // 校验必填参数是否合法
        public bool CheckValidation()
        {
            if (string.IsNullOrEmpty(TopicId))
                return false;

            if (LogGroupList == null || LogGroupList.LogGroups.Count == 0)
                return false;

            return true;
        }

        /// <summary>
        /// 压缩输入的原始字节数据，并返回压缩后的结果。
        /// 当前仅支持 LZ4 压缩算法，其他类型将返回原始数据。
        /// </summary>
        /// <param name="body">待压缩的原始字节数据。</param>
        /// <returns>压缩后的字节数组（若压缩失败或不支持压缩类型，则返回原始数据）。</returns>
        /// <exception cref="TlsError">当使用 LZ4 压缩时，如果压缩结果长度为 0，抛出此异常。</exception>
        public byte[] GetCompressBodyBytes(byte[] body)
        {
            byte[] compressBytes = body;

            switch (CompressType)
            {
                case Consts.CompressLz4:
                    int maxCompressedLength = LZ4Codec.MaximumOutputSize(body.Length);
                    compressBytes = new byte[maxCompressedLength];

                    int compressedLength = LZ4Codec.Encode(
                        body, 0, body.Length,
                        compressBytes, 0, compressBytes.Length
                    );

                    if (compressedLength == 0)
                    {
                        throw new TlsError("lz4 compress failed");
                    }

                    Array.Resize(ref compressBytes, compressedLength);
                    break;

                default:
                    break;
            }

            return compressBytes;
        }
    }
}
