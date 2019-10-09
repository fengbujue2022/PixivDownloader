using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PixivDownloader
{
    public class TaskQueue
    {
        private readonly Queue<Func<Task>> _waitingQueue = new Queue<Func<Task>>();
        private readonly int _maxParallelizationCount;

        private int _associatedParallelizationCount;
        private object _syncObj = new object();

        public TaskQueue(int? maxParallelizationCount = null)
        {
            _maxParallelizationCount = maxParallelizationCount ?? int.MaxValue;
        }

        public void Queue(Func<Task> futureTask)
        {
            lock (_syncObj)
            {
                if (_associatedParallelizationCount < _maxParallelizationCount)
                {
                    _associatedParallelizationCount++;
                    WrapTask(futureTask).Invoke();
                }
                else
                {
                    _waitingQueue.Enqueue(futureTask);
                }
            }
        }

        private Func<Task> WrapTask(Func<Task> futureTask)
        {
            return async () =>
            {
                await futureTask();
                if (_waitingQueue.TryDequeue(out var waitTask))
                {
                    WrapTask(waitTask).Invoke();
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