using Microsoft.VisualStudio.TestTools.UnitTesting;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text;
using System.IO;

using MondoCore.Common;
using MondoCore.Log;
using MondoCore.Azure.Storage;
using MondoCore.Azure.TestHelpers;

namespace MondoCore.Azure.Storage.FunctionalTests
{
    [TestClass]
    [TestCategory("Functional Tests")]
    public class AzureStoreLogTest
    {
        private ILog _log;

        private string _container = "logs";

        public AzureStoreLogTest()
        {
            _log = new MyStorage(CreateStorage("acmewidgets"));
        }

        [TestMethod]
        public async Task AzureStoreLog_WriteError()
        {
            await _log.WriteError(new Exception("Unable to connect to db"));
        }

        [TestMethod]
        public async Task AzureStoreLog_WriteError_wData()
        {
            var ex = new Exception("File not found");

            ex.Data["Make"] = "Chevy";
            ex.Data["Model"] = "Corvette";

            await _log.WriteError(ex);
        }

        [TestMethod]
        public async Task AzureStoreLog_WriteEvent()
        {
            await _log.WriteEvent("Race", new { RaceDate = DateTime.Now.AddDays(-2), Make = "Chevy", Model = "Corvette", Year = 1964 });
        }

        [TestMethod]
        public async Task AzureStoreLog_WriteTrace()
        {
            await _log.WriteTrace("Message sent to client", Telemetry.LogSeverity.Critical, new { Make = "Chevy", Model = "Corvette", Year = 1964 });
        }

        private IBlobStore CreateStorage(string folder = "")
        { 
            var path   = Path.Combine(_container, folder).Replace("\\", "/");
            var config = TestConfiguration.Load();

            return new AzureStorage(config.DataLakeConnectionString, path);
        }

        private class MyStorage : BlobStoreLog
        {
            public MyStorage(IBlobStore store) : base(store)
            {

            }

            protected override string CreateKey(Telemetry telemetry)
            {
                return telemetry.Message + "_" + base.CreateKey(telemetry);
            }
        }
    }
}
