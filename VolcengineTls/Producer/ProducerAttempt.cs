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

namespace VolcengineTls.Producer
{
    public class ProducerAttempt
    {
        public bool SuccessFlag;
        public string RequestId;
        public string ErrorCode;
        public string ErrorMessage;
        public long TimestampMs;

        public ProducerAttempt(bool successFlag, string requestId, string errorCode, string errorMessage, long timestampMs)
        {
            SuccessFlag = successFlag;
            RequestId = requestId;
            ErrorCode = errorCode;
            ErrorMessage = errorMessage;
            TimestampMs = timestampMs;
        }

        public override string ToString()
        {
            return $"SuccessFlag: {SuccessFlag}, " +
                $"RequestId: {RequestId}, " +
                $"ErrorCode: {ErrorCode}, " +
                $"ErrorMessage: {ErrorMessage}, " +
                $"TimestampMs: {TimestampMs}";
        }
    }
}
