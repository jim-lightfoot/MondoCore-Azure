using System;
using System.Threading.Tasks;

using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using MondoCore.ApplicationInsights;
using MondoCore.Log;

using MondoCore.Azure.TestHelpers;

namespace MondoCore.ApplicationInsights.FunctionalTests
{
    [TestClass]
    [TestCategory("Functional Tests")]
    public class ApplicationInsightsTests
    {
        private string _correlationId = Guid.NewGuid().ToString().ToLower();

        [TestMethod]
        public async Task ApplicationInsights_WriteException()
        {
            var log = CreateAppInsights();
             
            await log.WriteError(new System.Exception("Test Exception"), properties: new { Make = "Chevy", Model = "Camaro", Year = 1969 });
        }

        [TestMethod]
        public async Task ApplicationInsights_WriteEvent()
        {
            var log = CreateAppInsights();
             
            await log.WriteEvent("Test Event", properties: new { Make = "Chevy", Model = "Camaro", Year = 1969 });
        }

        [TestMethod]
        public async Task ApplicationInsights_WriteError()
        {
            using(var log = SetupRequest("WriteError"))
            { 
                log.SetProperty("Model", "Corvette");

                await log.WriteError(new Exception("Bob's hair is on fire"), properties: new {Make = "Chevy", Engine = new { Cylinders = 8, Displacement = 350, Piston = new { RodMaterial = "Chrome Moly", Material = "Stainless Steel", Diameter = 9200 } } } );
                await log.WriteError(new Exception("Fred's hair is on fire"), properties: new {Make = "Chevy" } );
                await log.WriteError(new Exception("Wilma's hair is on fire"), properties: new {Make = "Chevy" } );
            }
        }

        [TestMethod]
        public async Task ApplicationInsights_WriteError_dotted()
        {
            using(var log = SetupRequest("WriteError", false))
            { 
                log.SetProperty("Model", "Corvette");

                await log.WriteError(new Exception("Bob's hair is on fire"), properties: new {Make = "Chevy", Engine = new { Cylinders = 8, Displacement = 350, Piston = new { RodMaterial = "Chrome Moly", Material = "Stainless Steel", Diameter = 9200 } } } );
                await log.WriteError(new Exception("Fred's hair is on fire"), properties: new {Make = "Chevy" } );
                await log.WriteError(new Exception("Wilma's hair is on fire"), properties: new {Make = "Chevy" } );
            }
        }

        [TestMethod]
        public async Task ApplicationInsights_WriteError2()
        {
            using(var log = SetupRequest("WriteError2"))
            { 
                log.SetProperty("Make", "Chevy");
                log.SetProperty("Model", "Corvette");

                await log.WriteEvent("John's feet are wet");
                await log.WriteError(new Exception("George's hair is on fire"));
                await log.WriteError(new Exception("Franks's hair is on fire"));
                await log.WriteError(new Exception("Naomi's hair is on fire"), Telemetry.LogSeverity.Critical);
            }
        }

        [TestMethod]
        public async Task ApplicationInsights_WriteError3()
        {
            using(var log = SetupRequest("WriteError3"))
            { 
                 await log.WriteEvent("John's hair is on fire", properties: new {Make = "Chevy", Model = "Corvette" } );
                 await log.WriteError(new Exception("Linda's hair is on fire"), properties: new {Make = "Chevy", Model = "Corvette" } );
            }
        }

        #region Private

        private IRequestLog SetupRequest(string operationName, bool childrenAsJson = true)
        {
            var baseLog = new MondoCore.Log.Log();

            baseLog.Register(CreateAppInsights(childrenAsJson));

            return new RequestLog(baseLog, operationName, _correlationId);
        }


        private ApplicationInsights CreateAppInsights(bool childrenAsJson = true)
        { 
            var config = TestConfiguration.Load();

            return new ApplicationInsights(new TelemetryConfiguration(config.InstrumentationKey), childrenAsJson);
        }

        #endregion
    }
}
