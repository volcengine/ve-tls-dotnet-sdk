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

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace VolcengineTls.Producer
{
    public class ProducerRetryQueue
    {
        private readonly List<ProducerBatch> _batch;
        private readonly IComparer<ProducerBatch> _comparer;
        private readonly ReaderWriterLockSlim _lock;
        private readonly ILogger _logger;

        public ProducerRetryQueue(ILogger logger)
        {
            _batch = new List<ProducerBatch>();
            _comparer = Comparer<ProducerBatch>.Create((x, y) => x.NextRetryMs.CompareTo(y.NextRetryMs));
            _lock = new ReaderWriterLockSlim();
            _logger = logger;
        }

        public void AddToRetryQueue(ProducerBatch batch)
        {
            _logger.LogDebug("Send to retry queue");

            _lock.EnterWriteLock();
            try
            {
                if (batch != null)
                {
                    Push(batch);
                }
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public List<ProducerBatch> GetRetryBatch(bool stopFlag)
        {
            var result = new List<ProducerBatch>();

            _lock.EnterWriteLock();
            try
            {
                if (!stopFlag)
                {
                    long currentTimeMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                    while (_batch.Count > 0 && _batch[0].NextRetryMs < currentTimeMs)
                    {
                        result.Add(Pop());
                    }
                }
                else
                {
                    // 停止标志为true时，取出所有批次
                    while (_batch.Count > 0)
                    {
                        result.Add(Pop());
                    }
                }
            }
            finally
            {
                _lock.ExitWriteLock();
            }

            return result;
        }

        // 堆操作：添加元素
        private void Push(ProducerBatch item)
        {
            _batch.Add(item);
            int childIndex = _batch.Count - 1;

            while (childIndex > 0)
            {
                int parentIndex = (childIndex - 1) / 2;

                if (_comparer.Compare(_batch[childIndex], _batch[parentIndex]) >= 0)
                    break;

                Swap(childIndex, parentIndex);
                childIndex = parentIndex;
            }
        }

        // 堆操作：获取并移除堆顶元素
        private ProducerBatch Pop()
        {
            if (_batch.Count == 0)
                throw new InvalidOperationException("Heap is empty");

            int lastIndex = _batch.Count - 1;
            ProducerBatch result = _batch[0];

            _batch[0] = _batch[lastIndex];
            _batch.RemoveAt(lastIndex);
            lastIndex--;

            int parentIndex = 0;

            while (true)
            {
                int leftChildIndex = parentIndex * 2 + 1;
                if (leftChildIndex > lastIndex)
                    break;

                int rightChildIndex = leftChildIndex + 1;
                int minChildIndex = (rightChildIndex <= lastIndex &&
                                     _comparer.Compare(_batch[rightChildIndex], _batch[leftChildIndex]) < 0)
                    ? rightChildIndex
                    : leftChildIndex;

                if (_comparer.Compare(_batch[parentIndex], _batch[minChildIndex]) <= 0)
                    break;

                Swap(parentIndex, minChildIndex);
                parentIndex = minChildIndex;
            }

            return result;
        }

        // 交换元素位置
        private void Swap(int i, int j)
        {
            ProducerBatch temp = _batch[i];
            _batch[i] = _batch[j];
            _batch[j] = temp;
        }
    }
}
