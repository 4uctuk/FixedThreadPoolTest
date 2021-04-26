using System;
using FixedThreadPool;
using FixedThreadPool.Model.Enum;
using FixedThreadPool.Model.Interfaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace FixedThreadPoolTests
{
    [TestClass]
    public class FixedThreadPoolUnitTests
    {
        [TestMethod]
        public void Constructor_Throws_Argument_Exception()
        {
            try
            {
                var _ = new FixedThreadPoolService(0);
            }
            catch (Exception e)
            {
                Assert.IsInstanceOfType(e, typeof(ArgumentException));
            }
        }

        [TestMethod]
        public void Execute_Throws_ArgNullException()
        {
            var fixedThreadPoolService = new FixedThreadPoolService(1);

            try
            {
                fixedThreadPoolService.Execute(null, Priority.Low);
            }
            catch (Exception e)
            {
                Assert.IsInstanceOfType(e, typeof(ArgumentNullException));
            }
        }

        [TestMethod]
        public void Execute_Returns_True_If_Not_Stopped()
        {
            var fixedThreadPoolService = new FixedThreadPoolService(1);
            var task = new Mock<ITask>();

            var result = fixedThreadPoolService.Execute(task.Object, Priority.Normal);

            Assert.IsTrue(result, "Execution task for started pool returned false");
        }

        [TestMethod]
        public void Execute_Returns_False_If_Stopped()
        {
            var fixedThreadPoolService = new FixedThreadPoolService(1);
            var task = new Mock<ITask>();
            
            fixedThreadPoolService.Stop();
            var result = fixedThreadPoolService.Execute(task.Object, Priority.Normal);

            Assert.IsFalse(result, "Execution task for stopped pool returned true");
        }
    }
}
