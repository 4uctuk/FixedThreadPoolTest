using System;
using System.Threading;
using FixedThreadPool.Model.Interfaces;

namespace FixedThreadPool.Model
{
    public class UserTask : ITask
    {
        public void Execute()
        {
            Console.WriteLine("Executing UserTask");
            Thread.Sleep(3000);
        }
    }
}
