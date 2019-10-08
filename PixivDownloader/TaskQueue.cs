using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PixivDownloader
{
    public class TaskQueue
    {
        private readonly ConcurrentQueue<Func<Task>> _waitingQueue = new ConcurrentQueue<Func<Task>>();
        private readonly int _maxParallelizationCount;

        private int _associatedParallelizationCount;
        private object _syncObj = new object();

        public TaskQueue(int? maxParallelizationCount = null)
        {
            _maxParallelizationCount = maxParallelizationCount ?? int.MaxValue;
        }

        public async Task Queue(Func<Task> futureTask)
        {
            if (_associatedParallelizationCount < _maxParallelizationCount)
            {
                lock (_syncObj)
                {
                    _associatedParallelizationCount++;
                }
                await WrapTask(futureTask)();
            }
            else
            {
                _waitingQueue.Enqueue(futureTask);
            }
        }

        private Func<Task> WrapTask(Func<Task> futureTask)
        {
            return async () =>
            {
                await futureTask();
                if (_waitingQueue.TryDequeue(out var waitTask))
                {
                    WrapTask(waitTask)();
                }
                else
                {
                    lock (_syncObj)
                    {
                        _associatedParallelizationCount--;
                    }
                }
            };
        }

    }
}
