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

namespace VolcengineTls
{
    public static class Util
    {
        /// <summary>
        /// 确保值在指定范围内，超出范围时返回默认值
        /// </summary>
        /// <param name="valueToCheck">待验证的值</param>
        /// <param name="min">最小值（包含）</param>
        /// <param name="max">最大值（包含）</param>
        /// <param name="defaultValue">无效时的默认值</param>
        /// <returns>验证后的值</returns>
        public static T GetValueEnsureInRange<T>(T valueToCheck, T min, T max, T defaultValue)
            where T : struct, IComparable<T>
        {
            if (valueToCheck.CompareTo(min) < 0 || valueToCheck.CompareTo(max) > 0)
            {
                return defaultValue;
            }
            return valueToCheck;
        }
    }
}
