using Microsoft.VisualStudio.TestTools.UnitTesting;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using MondoCore.Azure.CosmosDB;
using MondoCore.Common;
using MondoCore.Data;

using System.Text.Json;
using System.Text.Json.Serialization;

using MondoCore.Azure.TestHelpers;

namespace MondoCore.Azure.CosmosDB.FunctionalTests
{
    [TestClass]
    public class CosmosContainerTests 
    {
        private IDatabase                         _db;
        private IReadRepository<string, Automobile>  _reader;
        private IWriteRepository<string, Automobile> _writer;

        private readonly string _repoName = "cars";

        private List<string> _idCollection = new List<string>();

        private DelimitedIdentifierStrategy<string> _idStrategy = new DelimitedIdentifierStrategy<string>();

        public CosmosContainerTests()
        {
            var idStrategy = new DelimitedIdentifierStrategy<string>();
            var config = TestConfiguration.Load();

            _db     = new CosmosDB("functionaltests", config.ConnectionString);
            _writer = _db.GetRepositoryWriter<string, Automobile>("cars", _idStrategy);
            _reader = _db.GetRepositoryReader<string, Automobile>("cars", _idStrategy);
        }

        [TestInitialize]
        public async Task Initialize()
        {
            await _writer.Delete( _=> true );

            _idCollection.Clear();

            for(var i = 0; i < 6; ++i)
                _idCollection.Add(Guid.NewGuid().ToString());

            await _writer.Insert(new Automobile { Id = _idCollection[0], Make = "Chevy",      Color = "Blue",  Model = "Camaro",    Year = 1969 });
            await _writer.Insert(new Automobile { Id = _idCollection[1], Make = "Pontiac",    Color = "Black", Model = "Firebird",  Year = 1972 });
            await _writer.Insert(new Automobile { Id = _idCollection[2], Make = "Chevy",      Color = "Green", Model = "Corvette",  Year = 1964 });
            await _writer.Insert(new Automobile { Id = _idCollection[3], Make = "Audi",       Color = "Blue",  Model = "S5",        Year = 2021 });
            await _writer.Insert(new Automobile { Id = _idCollection[4], Make = "Studebaker", Color = "Black", Model = "Speedster", Year = 1914 });
            await _writer.Insert(new Automobile { Id = _idCollection[5], Make = "Arrow",      Color = "Green", Model = "Glow",      Year = 1917 });
        }

        [TestCleanup]
        public async Task Cleanup()
        {
            await _writer.Delete( _=> true );

            _idCollection.Clear();
        }

        [TestMethod]
        public async Task Writer_Insert()
        {
            var id = Guid.NewGuid().ToString();

            await _writer.Insert(new Automobile 
            {
               Id = id,
               Make = "Pontiac",
               Model = "GTO",
               Color = "Dark Blue",
               Year = 1972
            });

            var result = await _reader.Get(_idStrategy.ToPartitionedId(id, "Pontiac"));

            Assert.IsNotNull(result);
            Assert.AreEqual(id, result.Id);
            Assert.AreEqual("GTO", result.Model);

            await _writer.Delete(_idStrategy.ToPartitionedId(id, "Pontiac"));
        }

        [TestMethod]
        public async Task Writer_Insert_many()
        {
            var id1 = Guid.NewGuid().ToString();
            var id2 = Guid.NewGuid().ToString();

            await _writer.Insert(new List<Automobile> 
            {
                new Automobile 
                {
                   Id = id1,
                   Make = "Pontiac",
                   Model = "GTO",
                   Color = "Dark Blue",
                   Year = 1972
                },
                new Automobile 
                {
                   Id = id2,
                   Make = "Aston-Martin",
                   Model = "DB9",
                   Color = "Cobalt",
                   Year = 1968
                }
            });

            _reader = _db.GetRepositoryReader<string, Automobile>(_repoName, "Pontiac");

            var result1 = await _reader.Get(id1);

            Assert.IsNotNull(result1);
            Assert.AreEqual(id1, result1.Id);
            Assert.AreEqual("GTO", result1.Model);

            _reader = _db.GetRepositoryReader<string, Automobile>(_repoName, "Aston-Martin");

            var result2 = await _reader.Get(id2);

            Assert.IsNotNull(result2);
            Assert.AreEqual(id2, result2.Id);
            Assert.AreEqual("DB9", result2.Model);
        }

        [TestMethod]
        public async Task Reader_Get_notfound()
        {
            var id = Guid.NewGuid().ToString();
            await _writer.Insert(new Automobile { Id = id, Make = "Chevy", Model = "Camaro" });

            await Assert.ThrowsExceptionAsync<NotFoundException>( async ()=> await _reader.Get(_idStrategy.ToPartitionedId(Guid.NewGuid().ToString(), "Chevy")));
        }

        [TestMethod]
        public void Reader_Where()
        {
            var result = _reader.Where( o=> o.Make == "Chevy").ToList();

            Assert.AreEqual(2, result.Count());
            Assert.IsTrue(result.Where( c=> c.Model == "Corvette").Any());
            Assert.IsTrue(result.Where( c=> c.Model == "Camaro").Any());
        }

        [TestMethod]
        public async Task Reader_Get_wExpression()
        {
            var result = await _reader.Get( o=> o.Make == "Chevy").ToList();

            Assert.AreEqual(2, result.Count());
            Assert.IsTrue(result.Where( c=> c.Model == "Corvette").Any());
            Assert.IsTrue(result.Where( c=> c.Model == "Camaro").Any());

            var result2 = await _reader.Get( o=> o.Year < 1970).ToList();

            Assert.AreEqual(4, result2.Count());
        }

        [TestMethod]
        public async Task Reader_Get_wId_list()
        {
            var id1 = _idCollection[1];
            var id2 = _idCollection[2];

            var result = await _reader.Get( new List<string> { _idStrategy.ToPartitionedId(id1, "Pontiac"),
                                                               _idStrategy.ToPartitionedId(id2, "Chevy") 
                                                             } ).ToList();

            Assert.AreEqual(2, result.Count());

            Assert.IsTrue(result.Where( c=> c.Model == "Corvette").Any());
            Assert.IsTrue(result.Where( c=> c.Model == "Firebird").Any());
        }

        [TestMethod]
        public async Task Reader_Get_wExpression_notfound()
        {
            var result = await _reader.Get( o=> o.Make == "Chevy").ToList();

            Assert.AreEqual(2, result.Count());
            Assert.IsTrue(result.Where( c=> c.Model == "Corvette").Any());
            Assert.IsTrue(result.Where( c=> c.Model == "Camaro").Any());

            var result2 = await _reader.Get( o=> o.Year < 1900).ToList();

            Assert.AreEqual(0, result2.Count);
        }

        [TestMethod]
        public void Reader_Count()
        {
            Assert.AreEqual(6, _reader.Count());
        }

        [TestMethod]
        public void Reader_Count_wQuery()
        {
            Assert.AreEqual(2, _reader.Count( i=> i.Color == "Blue"));
        }

        [TestMethod]
        public async Task Writer_Update()
        {
            Assert.IsTrue(await _writer.Update(new Automobile { Id = _idCollection[0], Make = "Chevy", Model = "Camaro", Year = 1970 }));  

            var result = await _reader.Get(_idStrategy.ToPartitionedId(_idCollection[0], "Chevy"));

            Assert.AreEqual(1970, result.Year);
        }

        [TestMethod]
        public async Task Writer_Update_wGuard_succeeds()
        {
            var result = await _writer.Update(new Automobile { Id = _idCollection[0], Make = "Chevy", Model = "Camaro", Color = "Blue", Year = 1970 }, (i)=> i.Color == "Blue");  

            Assert.IsTrue(result);  

            var result1 = await _reader.Get(_idStrategy.ToPartitionedId(_idCollection[0], "Chevy"));
            var result2 = await _reader.Get(_idStrategy.ToPartitionedId(_idCollection[1], "Pontiac"));
            var result3 = await _reader.Get(_idStrategy.ToPartitionedId(_idCollection[2], "Chevy")); 
            var result4 = await _reader.Get(_idStrategy.ToPartitionedId(_idCollection[3], "Audi"));

            Assert.AreEqual(1970, result1.Year);
            Assert.AreEqual(1972, result2.Year);
            Assert.AreEqual(1964, result3.Year);
            Assert.AreEqual(2021, result4.Year);
        }

        [TestMethod]
        public async Task Writer_Update_wGuard_fails()
        {
            Assert.IsFalse(await _writer.Update(new Automobile { Id = _idCollection[0], Make = "Chevy", Model = "Camaro", Year = 1970 }, (i)=> i.Color == "Periwinkle"));  

            var result1 = await _reader.Get(_idStrategy.ToPartitionedId(_idCollection[0], "Chevy"));
            var result2 = await _reader.Get(_idStrategy.ToPartitionedId(_idCollection[1], "Pontiac"));
            var result3 = await _reader.Get(_idStrategy.ToPartitionedId(_idCollection[2], "Chevy")); 
            var result4 = await _reader.Get(_idStrategy.ToPartitionedId(_idCollection[3], "Audi"));

            Assert.AreEqual(1969, result1.Year);
            Assert.AreEqual(1972, result2.Year);
            Assert.AreEqual(1964, result3.Year);
            Assert.AreEqual(2021, result4.Year);
        }
        
        [TestMethod]
        public async Task Writer_Update_properties_succeeds()
        {
            Assert.AreEqual(2, await _writer.Update(new { Year = 1970 }, (i)=> i.Color == "Blue"));  

            var result1 = await _reader.Get(_idStrategy.ToPartitionedId(_idCollection[0], "Chevy"));
            var result2 = await _reader.Get(_idStrategy.ToPartitionedId(_idCollection[1], "Pontiac"));
            var result3 = await _reader.Get(_idStrategy.ToPartitionedId(_idCollection[2], "Chevy")); 
            var result4 = await _reader.Get(_idStrategy.ToPartitionedId(_idCollection[3], "Audi"));
            var result5 = await _reader.Get(_idStrategy.ToPartitionedId(_idCollection[4], "Studebaker")); 
            var result6 = await _reader.Get(_idStrategy.ToPartitionedId(_idCollection[5], "Arrow"));

            Assert.AreEqual(1970, result1.Year);
            Assert.AreEqual(1972, result2.Year);
            Assert.AreEqual(1964, result3.Year);
            Assert.AreEqual(1970, result4.Year);
            Assert.AreEqual(1914, result5.Year);
            Assert.AreEqual(1917, result6.Year);
        }

        [TestMethod]
        public async Task Writer_Update_properties_2vals_succeeds()
        {
            Assert.AreEqual(2, await _writer.Update(new { Year = 1970, Color = "Red" }, (i)=> i.Color == "Blue"));  

            var result1 = await _reader.Get(_idStrategy.ToPartitionedId(_idCollection[0], "Chevy"));
            var result2 = await _reader.Get(_idStrategy.ToPartitionedId(_idCollection[1], "Pontiac"));
            var result3 = await _reader.Get(_idStrategy.ToPartitionedId(_idCollection[2], "Chevy")); 
            var result4 = await _reader.Get(_idStrategy.ToPartitionedId(_idCollection[3], "Audi"));
            var result5 = await _reader.Get(_idStrategy.ToPartitionedId(_idCollection[4], "Studebaker")); 
            var result6 = await _reader.Get(_idStrategy.ToPartitionedId(_idCollection[5], "Arrow"));

            Assert.AreEqual(1970, result1.Year);
            Assert.AreEqual(1972, result2.Year);
            Assert.AreEqual(1964, result3.Year);
            Assert.AreEqual(1970, result4.Year);
            Assert.AreEqual(1914, result5.Year);
            Assert.AreEqual(1917, result6.Year);

            Assert.AreEqual("Red",   result1.Color);
            Assert.AreEqual("Black", result2.Color);
            Assert.AreEqual("Green", result3.Color);
            Assert.AreEqual("Red",   result4.Color);
            Assert.AreEqual("Black", result5.Color);
            Assert.AreEqual("Green", result6.Color);
        }

        public class Automobile : IPartitionable<string>
        {
            [JsonPropertyName("id")]
            public string   Id    {get; set;}
            public string   id    => Id;

            public string   Make  {get; set;}
            public string   Model {get; set;}
            public string   Color {get; set;}
            public int      Year  {get; set;} = 1964;

            public string GetPartitionKey()
            {
                return this.Make;
            }

            public override string ToString()
            {
                return JsonSerializer.Serialize(this);
            }
        }
    }
}
