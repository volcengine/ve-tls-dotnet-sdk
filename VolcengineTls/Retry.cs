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
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;
using VolcengineTls.Error;

namespace VolcengineTls
{
    public class RetryHandler
    {
        private static TimeSpan defaultRequestTimeout = TimeSpan.FromSeconds(90);
        private static int currentRetryCounter = 0;
        private static readonly int defaultRetryCounterMaximum = 50;
        private static readonly object _retryLock = new object();
        private static readonly TimeSpan _defaultRetryInterval = TimeSpan.FromMilliseconds(200);

        public static async Task<HttpResponseMessage> ExecuteWithRetryAsync(
            Func<Task<HttpResponseMessage>> httpCall
        )
        {
            int tryCount = 0;
            DateTime expectedQuitTime = DateTime.Now.Add(defaultRequestTimeout);
            Exception lastException = null;
            HttpResponseMessage response = null;

            while (true)
            {
                tryCount++;

                try
                {
                    response = await httpCall();
                }
                catch (TlsError e)
                {
                    if (!NeedRetry(e))
                    {
                        DecreaseGlobalRetry();
                        return response;
                    }
                }
                catch (SocketException e)
                {
                    if (e.SocketErrorCode != SocketError.TimedOut)
                    {
                        throw e;
                    }
                }
                catch (Exception e)
                {
                    throw e;
                }

                if (response.IsSuccessStatusCode)
                {
                    DecreaseGlobalRetry();
                    return response;
                }

                if (tryCount >= 5 || GetGlobalRetryCount() >= defaultRetryCounterMaximum || DateTime.Now > expectedQuitTime)
                {
                    if (lastException != null)
                        throw lastException;

                    if (response != null)
                        return response;

                    throw new TlsError("Request failed after retries");
                }

                // 增加全局 retry
                IncreaseGlobalRetry();

                // 退避等待（带随机）
                var retrySleepInterval = new Random().NextDouble() * tryCount * _defaultRetryInterval.TotalMilliseconds;
                await Task.Delay(TimeSpan.FromMilliseconds(retrySleepInterval));
            }
        }

        private static bool NeedRetry(TlsError err)
        {
            if (err == null) {
                return false;
            }

            return err.HttpCode == 429
            || err.HttpCode == (int)HttpStatusCode.InternalServerError
            || err.HttpCode == (int)HttpStatusCode.BadGateway
            || err.HttpCode == (int)HttpStatusCode.ServiceUnavailable;
        }

        private static int GetGlobalRetryCount()
        {
            lock (_retryLock)
            {
                return currentRetryCounter;
            }
        }

        private static void IncreaseGlobalRetry()
        {
            lock (_retryLock)
            {
                if (currentRetryCounter < defaultRetryCounterMaximum)
                    currentRetryCounter++;
            }
        }

        private static void DecreaseGlobalRetry()
        {
            lock (_retryLock)
            {
                if (currentRetryCounter > 0)
                    currentRetryCounter--;
            }
        }
    }

}
