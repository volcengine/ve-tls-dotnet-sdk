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
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace VolcengineTls.Error
{
    [DataContract]
    public class TlsError : Exception
    {
        [DataMember(Name = "httpCode")]
        public int HttpCode { get; set; }

        [DataMember(Name = "errorCode")]
        public string Code { get; set; }

        [DataMember(Name = "errorMessage")]
        public string ErrorMessage { get; set; }

        [DataMember(Name = "requestID")]
        public string RequestId { get; set; }

        public TlsError(string errorMessage)
            : base(errorMessage)
        {
            HttpCode = -1;
            Code = "ClientError";
            ErrorMessage = errorMessage;
        }

        public TlsError(int httpcode, string errorCode, string errorMessage, string requestId)
        {
            HttpCode = httpcode;
            Code = errorCode;
            ErrorMessage = errorMessage;
            RequestId = requestId;
        }

        public static TlsError ToTlsError(Exception err)
        {
            if (err == null)
                return null;

            var clientError = err as TlsError;
            if (clientError != null)
                return clientError;

            return new TlsError(err.Message);
        }

        public override string ToString()
        {
            try
            {
                var serializer = new DataContractJsonSerializer(typeof(TlsError));
                using (var ms = new MemoryStream())
                {
                    serializer.WriteObject(ms, this);
                    return Encoding.UTF8.GetString(ms.ToArray());
                }
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
