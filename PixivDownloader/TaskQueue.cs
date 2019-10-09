using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PixivDownloader
{
    //TODO: add task cancel by timeout feature
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

        public async ValueTask Queue(Func<Task> futureTask)
        {
            if (_associatedParallelizationCount < _maxParallelizationCount)
            {
                lock (_syncObj)
                {
                    Console.WriteLine("并行数未满 可以执行 " + _associatedParallelizationCount);
                    _associatedParallelizationCount++;
                }
                await WrapTask(futureTask).Invoke();
            }
            else
            {
                Console.WriteLine("并行数已满 加入等待队列 " + _associatedParallelizationCount);
                _waitingQueue.Enqueue(futureTask);
            }
        }

        private Func<Task> WrapTask(Func<Task> futureTask)
        {
            return async () =>
            {
                await futureTask();
                Console.WriteLine("任务完成 等待队列还有" + _waitingQueue.Count);
                if (_waitingQueue.TryDequeue(out var waitTask))
                {
                    WrapTask(waitTask).Invoke();
                }
                else
                {
                    lock (_syncObj)
                    {
                        Console.WriteLine("等待队列没有任务 parallel count:" + _associatedParallelizationCount);
                        _associatedParallelizationCount--;
                    }
                }
            };
        }

    }
}
