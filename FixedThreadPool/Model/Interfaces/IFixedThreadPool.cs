using FixedThreadPool.Model.Enum;

namespace FixedThreadPool.Model.Interfaces
{
    /// <summary>
    /// Thread pool with fixed amount of threads
    /// </summary>
    public interface IFixedThreadPool
    {
        /// <summary>
        /// Executing task
        /// </summary>
        /// <param name="task">Task</param>
        /// <param name="priority">Priority</param>
        /// <returns></returns>
        bool Execute(ITask task, Priority priority);

        /// <summary>
        /// Stop executing and wait for complete started threads
        /// </summary>
        void Stop();
    }
}
