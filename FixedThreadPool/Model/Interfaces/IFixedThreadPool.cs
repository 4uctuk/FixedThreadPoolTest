using FixedThreadPool.Model.Enum;

namespace FixedThreadPool.Model.Interfaces
{
    public interface IFixedThreadPool
    {
        bool Execute(ITask task, Priority priority);

        void Stop();
    }
}
