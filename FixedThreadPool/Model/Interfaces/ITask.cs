namespace FixedThreadPool.Model.Interfaces
{
    /// <summary>
    /// Taks for execution
    /// </summary>
    public interface ITask
    {
        /// <summary>
        /// Executing method
        /// </summary>
        void Execute();
    }
}
