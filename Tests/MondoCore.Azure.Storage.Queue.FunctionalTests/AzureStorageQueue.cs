using Microsoft.VisualStudio.TestTools.UnitTesting;

using System.Threading.Tasks;

using MondoCore.Common;
using MondoCore.Azure.Storage.Queue;

using MondoCore.Azure.TestHelpers;

namespace MondoCore.Azure.Storage.Queue.FunctionalTests
{
    [TestClass]
    [TestCategory("Functional Tests")]
    public class AzureStorageQueueTests
    {
        private string _queue = "test";

        [TestMethod]
        public async Task AzureStorageQueue_Send()
        {
            var queue = CreateQueue();

            await queue.Send("Chevy Corvette");

            var msg = await queue.Retrieve();

            Assert.AreEqual("Chevy Corvette", msg.Value);

            await queue.Delete(msg);
        }

        private IMessageQueue CreateQueue()
        { 
            var config = TestConfiguration.Load();

            return new AzureStorageQueue(config.ConnectionString, _queue);
        }    
    }
}
