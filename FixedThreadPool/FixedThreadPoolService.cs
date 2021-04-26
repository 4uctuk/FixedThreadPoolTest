using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using FixedThreadPool.Model.Enum;
using FixedThreadPool.Model.Interfaces;

namespace FixedThreadPool
{
    public class FixedThreadPoolService : IFixedThreadPool
    {
        #region Props and fields

        /// <summary>
        /// Max thread count
        /// </summary>
        private readonly int _maxThreadsCount;

        /// <summary>
        /// Is pool was stopped
        /// </summary>
        private bool _stopped;
        
        /// <summary>
        /// Concurrent queue tasks with low priority
        /// </summary>
        private readonly ConcurrentQueue<ITask> _lowPriorityTasks = new ConcurrentQueue<ITask>();

        /// <summary>
        /// Concurrent queue tasks with normal priority
        /// </summary>
        private readonly ConcurrentQueue<ITask> _normalPriorityTasks = new ConcurrentQueue<ITask>();

        /// <summary>
        /// Concurrent queue tasks with High priority
        /// </summary>
        private readonly ConcurrentQueue<ITask> _highPriorityTasks = new ConcurrentQueue<ITask>();
        
        /// <summary>
        /// Currently running threads
        /// </summary>
        private readonly Dictionary<string, Thread> _runningThreads;

        /// <summary>
        /// Counter for executed high priority task
        /// </summary>
        private int _highPriorityTasksCounter;

        /// <summary>
        /// Counter for created threads
        /// </summary>
        private int _createdThreads;

        /// <summary>
        /// Count of currently running threads
        /// </summary>
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
        #endregion

        /// <summary>
        /// Thread pool with fixed amount of threads
        /// </summary>
        /// <param name="maxThreadsCount">Maximum threads</param>
        public FixedThreadPoolService(int maxThreadsCount)
        {
            if (maxThreadsCount <= 0)
            {
                throw new ArgumentException("Number of threads can't be less or equal zero");
            }

            _maxThreadsCount = maxThreadsCount;
            _runningThreads = new Dictionary<string, Thread>(maxThreadsCount);
        }

        #region IFixedThreadPool

        public bool Execute(ITask task, Priority priority)
        {
            if (task == null)
            {
                throw new ArgumentNullException(nameof(task));
            }

            if (_stopped)
                return false;

            QueueTask(task, priority);

            return !_stopped;
        }

        public void Stop()
        {
            _stopped = true;

            while (_runningThreads.Count > 0)
            {
                Thread.Sleep(1000);
            }
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Queue task
        /// </summary>
        /// <param name="task">Task</param>
        /// <param name="priority">Priority</param>
        private void QueueTask(ITask task, Priority priority)
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
                StartWorkThread();
            }

            lock (_stateLock)
            {
                Monitor.PulseAll(_stateLock);
            }
        }

        /// <summary>
        /// Starts new thread
        /// </summary>
        private void StartWorkThread()
        {
            Thread thread;
            lock (_stateLock)
            {
                if (_runningThreads.Count >= _maxThreadsCount)
                {
                    return;
                }

                _createdThreads++;

                var threadName = $"Thread {_createdThreads}";

                thread = new Thread(() => StartWorkThread(threadName)) { Name = threadName };
                _runningThreads.Add(thread.Name, thread);
            }

            thread.IsBackground = true;
            thread.Start();
        }

        /// <summary>
        /// Task executing main logic
        /// </summary>
        /// <param name="threadName">Thread name</param>
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
                            Monitor.Wait(_stateLock, new TimeSpan(0, 0, 0, 5));
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

        /// <summary>
        /// Task queue priority logic
        /// </summary>
        /// <returns></returns>
        private ITask GetNextJobToProcess()
        {
            ITask task;

            if (!_highPriorityTasks.IsEmpty)
            {
                if (!_normalPriorityTasks.IsEmpty && _highPriorityTasksCounter >= 3)//if more than 3 high priority tasks was executed
                {
                    lock (_stateLock)
                    {
                        _highPriorityTasksCounter = 0;
                    }

                    _normalPriorityTasks.TryDequeue(out task);
                   return task;
                }

                lock (_stateLock)
                {
                    _highPriorityTasksCounter++;
                }

                _highPriorityTasks.TryDequeue(out task);
                return task;
            }

            if (!_normalPriorityTasks.IsEmpty)
            {
                _normalPriorityTasks.TryDequeue(out task);
                return task;
            }

            if (_highPriorityTasks.IsEmpty && _normalPriorityTasks.IsEmpty)//low priority starts only if other priorities empty
            {
                _lowPriorityTasks.TryDequeue(out task);
                return task;
            }

            return null;
        }

        /// <summary>
        /// Removing thread
        /// </summary>
        /// <param name="threadName">thread name</param>
        private void WorkerThreadExited(string threadName)
        {
            lock (_stateLock)
            {
                _runningThreads.Remove(threadName);
            }
        }

        #endregion
    }
}
