using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using FixedThreadPool.Model;
using FixedThreadPool.Model.Enum;
using FixedThreadPool.Model.Interfaces;

namespace FixedThreadPool
{
    public class FixedThreadPoolService : IFixedThreadPool
    {
        private readonly int _maxThreadsCount;
        private bool _stopped;
        private readonly ConcurrentQueue<ITask> _lowPriorityTasks = new ConcurrentQueue<ITask>();
        private readonly ConcurrentQueue<ITask> _normalPriorityTasks = new ConcurrentQueue<ITask>();
        private readonly ConcurrentQueue<ITask> _highPriorityTasks = new ConcurrentQueue<ITask>();
        private readonly Dictionary<string, Thread> _runningThreads;
        private int _highPriorityTasksCounter;
        private int _createdThreads;

        private int TotalThreads
        {
            get
            {
                lock (_stateLock)
                {
                    return _runningThreads.Count;
                }
            }
        }

        private readonly object _stateLock = new object();
        
        public FixedThreadPoolService(int maxThreadsCount)
        {
            if (maxThreadsCount <= 0)
            {
                throw new ArgumentException("Number of threads can't be less or equal zero");
            }

            _maxThreadsCount = maxThreadsCount;
            _runningThreads = new Dictionary<string, Thread>(maxThreadsCount);
        }

        public bool Execute(ITask task, Priority priority)
        {
            if (task == null)
            {
                throw new ArgumentNullException(nameof(task));
            }

            if (_stopped)
                return false;

            RunOrQueueTask(task, priority);

            return !_stopped;
        }

        private void RunOrQueueTask(ITask task, Priority priority)
        {
            switch (priority)
            {
                case Priority.Low:
                    _lowPriorityTasks.Enqueue(task);
                    break;
                case Priority.Normal:
                    _normalPriorityTasks.Enqueue(task);
                    break;
                case Priority.High:
                    _highPriorityTasks.Enqueue(task);
                    break;
            }

            var sumOfTasks = _lowPriorityTasks.Count + _normalPriorityTasks.Count + _highPriorityTasks.Count;

            var newThreadNeeded = sumOfTasks > TotalThreads && TotalThreads < _maxThreadsCount;
            if (newThreadNeeded)
            {
                StartWorkThread(_maxThreadsCount);
            }

            lock (_stateLock)
            {
                Monitor.PulseAll(_stateLock);
            }
        }

        private void StartWorkThread(in int maxThreadsCount)
        {
            Thread thread;
            lock (_stateLock)
            {
                if (_runningThreads.Count >= maxThreadsCount)
                {
                    return;
                }

                _createdThreads++;

                var threadName = $"Thread {_createdThreads}";

                thread = new Thread(() => StartWorkThread(threadName)) {Name = threadName};
                _runningThreads.Add(thread.Name, thread);
            }

            thread.IsBackground = true;
            thread.Start();
        }

        private void StartWorkThread(string threadName)
        {
            try
            {
                while (true)
                {
                    if (TotalThreads > _maxThreadsCount)
                    {
                        return; 
                    }

                    var task = GetNextJobToProcess();
                    if (task == null)
                    {
                        lock (_stateLock)
                        {
                            Monitor.Wait(_stateLock, new TimeSpan(0,0,0,5));
                        }

                        task = GetNextJobToProcess();
                    }

                    if (task == null)
                    {
                        return;
                    }
                    else
                    {
                        task.Execute();
                    }
                }
            }
            finally
            {
                WorkerThreadExited(threadName);
            }
        }

        private ITask GetNextJobToProcess()
        {
            ITask task;

            if (!_highPriorityTasks.IsEmpty)
            {
                if (!_normalPriorityTasks.IsEmpty && _highPriorityTasksCounter >= 3)
                {
                    _normalPriorityTasks.TryDequeue(out task);
                    _highPriorityTasksCounter = 0;
                    return task;
                }

                _highPriorityTasks.TryDequeue(out task);
                _highPriorityTasksCounter++;
                return task;
            }

            if (!_normalPriorityTasks.IsEmpty)
            {
                _normalPriorityTasks.TryDequeue(out task);
                return task;
            }

            if (_highPriorityTasks.IsEmpty && _normalPriorityTasks.IsEmpty)
            {
                _lowPriorityTasks.TryDequeue(out task);
                return task;
            }

            return null;
        }

        private void WorkerThreadExited(string threadName)
        {
            lock (_stateLock)
            {
                _runningThreads.Remove(threadName);
            }
        }

        public void Stop()
        {
            _stopped = true;

            while (_runningThreads.Count > 0)
            {
                Thread.Sleep(1000);
            }
        }
    }
}
