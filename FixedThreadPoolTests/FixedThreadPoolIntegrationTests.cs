using System.Threading;
using FixedThreadPool;
using FixedThreadPool.Model.Enum;
using FixedThreadPool.Model.Interfaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace FixedThreadPoolTests
{
    [TestClass]
    public class FixedThreadPoolIntegrationTests
    {
        /// <summary>
        /// Gets or sets the test context which provides
        /// information about and functionality for the current test run.
        /// </summary>
        public TestContext TestContext { get; set; }

        [TestMethod]
        public void NormalPriorityTask_Executes_After_Three_HighPriority()
        {
            var fixedThreadPoolService = new FixedThreadPoolService(4);
            var highPriorityTask = new Mock<ITask>();
            highPriorityTask.Setup(c => c.Execute()).Callback(() =>
            {
                TestContext.WriteLine("Executing high priority task");
                Thread.Sleep(2000);
            });
            var normalPriorityTask = new Mock<ITask>();
            normalPriorityTask.Setup(c => c.Execute()).Callback(() =>
            {
                TestContext.WriteLine("Executing normal priority task");
                Thread.Sleep(2000);
            });
            fixedThreadPoolService.Execute(highPriorityTask.Object, Priority.High);
            fixedThreadPoolService.Execute(highPriorityTask.Object, Priority.High);
            Thread.Sleep(1000);
            fixedThreadPoolService.Execute(normalPriorityTask.Object, Priority.Normal);
            fixedThreadPoolService.Execute(highPriorityTask.Object, Priority.High);
            fixedThreadPoolService.Execute(highPriorityTask.Object, Priority.High);
            Thread.Sleep(1000);
            fixedThreadPoolService.Execute(normalPriorityTask.Object, Priority.Normal);
            fixedThreadPoolService.Execute(highPriorityTask.Object, Priority.High);
            fixedThreadPoolService.Execute(highPriorityTask.Object, Priority.High);
            fixedThreadPoolService.Execute(highPriorityTask.Object, Priority.High);
            fixedThreadPoolService.Execute(highPriorityTask.Object, Priority.High);
            
            fixedThreadPoolService.Stop();
        }

        [TestMethod]
        public void LowPriorityTask_Not_Executes_Before_Normal_Or_High()
        {
            var fixedThreadPoolService = new FixedThreadPoolService(4);
            var highPriorityTask = new Mock<ITask>();
            highPriorityTask.Setup(c => c.Execute()).Callback(() =>
            {
                TestContext.WriteLine("Executing high priority task");
                Thread.Sleep(2000);
            });
            var normalPriorityTask = new Mock<ITask>();
            normalPriorityTask.Setup(c => c.Execute()).Callback(() =>
            {
                Thread.Sleep(2000);
                TestContext.WriteLine("Executing normal priority task");
            });
            var lowPriorityTask = new Mock<ITask>();
            normalPriorityTask.Setup(c => c.Execute()).Callback(() =>
            {
                Thread.Sleep(1000);
                TestContext.WriteLine("Executing low priority task");
            });

            fixedThreadPoolService.Execute(lowPriorityTask.Object, Priority.Low);
            Thread.Sleep(1000);
            fixedThreadPoolService.Execute(highPriorityTask.Object, Priority.High);
            fixedThreadPoolService.Execute(highPriorityTask.Object, Priority.High);
            fixedThreadPoolService.Execute(normalPriorityTask.Object, Priority.Normal);
            fixedThreadPoolService.Execute(highPriorityTask.Object, Priority.High);
            Thread.Sleep(1000);
            fixedThreadPoolService.Execute(lowPriorityTask.Object, Priority.Low); ;
            fixedThreadPoolService.Execute(highPriorityTask.Object, Priority.High);
            fixedThreadPoolService.Execute(normalPriorityTask.Object, Priority.Normal);
            fixedThreadPoolService.Execute(highPriorityTask.Object, Priority.High);
            fixedThreadPoolService.Execute(highPriorityTask.Object, Priority.High);
            Thread.Sleep(1000);
            fixedThreadPoolService.Execute(highPriorityTask.Object, Priority.High);
            fixedThreadPoolService.Execute(highPriorityTask.Object, Priority.High);

            fixedThreadPoolService.Stop();
        }
    }
}
