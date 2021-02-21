using Microsoft.VisualStudio.TestTools.UnitTesting;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text;
using System.IO;

using MondoCore.Common;
using MondoCore.Azure.Storage;

using MondoCore.Azure.TestHelpers;

namespace MondoCore.Azure.Storage.FunctionalTests
{
    [TestClass]
    [TestCategory("Functional Tests")]
    public class AzurePageBlobStorageTests
    {
        private string _container = "test2";

        [TestMethod]
        public async Task AzurePageBlobStorage_Put_string()
        {
            var store = CreateStorage();
            var val   = CreateLongString();
            var len   = val.Length;

            await store.Put("bob", val);

            Assert.AreEqual(val, await store.Get("bob"));

            await store.Delete("bob");
        }

        [TestMethod]
        public async Task AzurePageBlobStorage_Put_stream()
        {
            var store = CreateStorage();
            var encoding = UTF8Encoding.UTF8;

            using(var stream = new MemoryStream(encoding.GetBytes("fred")))
            { 
                await store.Put("bob", stream);
            }

            Assert.AreEqual("fred", await store.Get("bob"));

            await store.Delete("bob");
        }

        [TestMethod]
        public async Task AzurePageBlobStorage_GetBytes()
        {
            var store = CreateStorage();
            var encoding = UTF8Encoding.UTF8;

            await store.Put("bob", "fred");

            Assert.AreEqual("fred", encoding.GetString(await store.GetBytes("bob")).SubstringBefore("\0"));

            await store.Delete("bob");
        }

        [TestMethod]
        public async Task AzurePageBlobStorage_Get()
        {
            var store = CreateStorage();

            await store.Put("bob", "fred");

            Assert.AreEqual("fred", await store.Get("bob"));

            await store.Delete("bob");
        }

        [TestMethod]
        [ExpectedException(typeof(FileNotFoundException))]
        public async Task AzurePageBlobStorage_Get_notfound()
        {
            var store = CreateStorage();

            await store.Delete("bob");
            await store.Put("bob", "fred");

            Assert.AreEqual("fred", await store.Get("george"));

            await store.Delete("bob");
        }

        [TestMethod]
        [ExpectedException(typeof(FileNotFoundException))]
        public async Task AzurePageBlobStorage_GetBytes_notfound()
        {
            var store = CreateStorage();
            var encoding = UTF8Encoding.UTF8;

            await store.Delete("bob");
            await store.Put("bob", "fred");

            Assert.AreEqual("fred", encoding.GetString(await store.GetBytes("george")));

            await store.Delete("bob");
        }

        [TestMethod]
        public async Task AzurePageBlobStorage_Get_stream()
        {
            var store = CreateStorage();

            var uid = Guid.NewGuid().ToString();

            await store.Put(uid, "fred");

            using(var strm = new MemoryStream())
            { 
                await store.Get(uid, strm);

                Assert.AreEqual("fred", await strm.ReadStringAsync());
            }

            await store.Delete(uid);
        }

        private string CreateLongString(int length = (int)(4096 * 1024 * 4.5))
        {
            StringBuilder sb = new StringBuilder();
            var iterations = length / 36;

            for(var i = 0; i < iterations; ++i)
                sb.Append("abcdefghijklmnopqrstuvwxyz0123456789");

            return sb.ToString();
        }

        [TestMethod]
        public async Task AzurePageBlobStorage_OpenRead()
        {
            var store = CreateStorage();
            var uid   = Guid.NewGuid().ToString();
            var val   = CreateLongString();
            var len   = val.Length;

            await store.Put(uid, val);

            using(var strm = await store.OpenRead(uid))
            { 
                var canSeek = strm.CanSeek;

                Assert.AreEqual(val, await strm.ReadStringAsync());
            }

            await store.Delete(uid);
        }

        [TestMethod]
        public async Task AzurePageBlobStorage_OpenRead_read_in_sections()
        {
            var store = CreateStorage();
            var uid   = Guid.NewGuid().ToString();
            var val   = CreateLongString((int)(1024 * 1024 * 2.1));
            var val2  = "";
            var len   = val.Length;
            var bufferSize = 3199;
            var buffer = new byte[bufferSize];
            var read = 0;

            await store.Put(uid, val);

            using(var memStream = new MemoryStream())
            { 
                using(var strm = await store.OpenRead(uid))
                { 
                    var canSeek = strm.CanSeek;

                    while(read < len)
                    { 
                        var thisRead = Math.Min(bufferSize, len - read);

                        await strm.ReadAsync(buffer, 0, thisRead); 

                        await memStream.WriteAsync(buffer, 0, thisRead);

                        read += thisRead;
                    }
                }

                val2 = await memStream.ReadStringAsync();
            }

            Assert.AreEqual(val, val2);

            await store.Delete(uid);
        }

        [TestMethod]
        public async Task AzurePageBlobStorage_OpenWrite()
        {
            var store = CreateStorage();
            var uid   = Guid.NewGuid().ToString();
            var val   = CreateLongString(36);

            using(var strm = await store.OpenWrite(uid))
            { 
                var canSeek = strm.CanSeek;
                var bytes   = UTF8Encoding.UTF8.GetBytes(val);

                await strm.WriteAsync(bytes, 0, bytes.Length);
            }

            Assert.AreEqual(val, await store.Get(uid));

            await store.Delete(uid);
        }

        [TestMethod]
        public async Task AzurePageBlobStorage_OpenWrite_long()
        {
            var store = CreateStorage();
            var uid   = Guid.NewGuid().ToString();

            try
            { 
                var val = CreateLongString();

                using(var strm = await store.OpenWrite(uid))
                { 
                    var canSeek = strm.CanSeek;
                    var bytes   = UTF8Encoding.UTF8.GetBytes(val);

                    await strm.WriteAsync(bytes, 0, bytes.Length);
                }

                Assert.AreEqual(val, await store.Get(uid));
            }
            finally
            { 
                await store.Delete(uid);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(FileNotFoundException))]
        public async Task AzurePageBlobStorage_Delete()
        {
            var store = CreateStorage();

            await store.Delete("bob");
            await store.Put("bob", "fred");

            Assert.AreEqual("fred", await store.Get("bob"));

            await store.Delete("bob");

            await Task.Delay(100);
            Assert.AreEqual("fred", await store.Get("bob"));
        }

        [TestMethod]
        public async Task AzurePageBlobStorage_FindAll()
        {
            var store = CreateStorage();

            await store.Put("bio.doc",       "fred");
            await store.Put("photo.jpg",     "flintstone");
            await store.Put("resume.pdf",    "bedrock");
            await store.Put("portfolio.pdf", "stuff");

            var result = await store.Find("*.*");

            Assert.AreEqual(4, result.Count());

            result = await store.Find("*.*");

            Assert.AreEqual(4, result.Count());

            await store.Delete("bio.doc");
            await store.Delete("photo.jpg");
            await store.Delete("resume.pdf");
            await store.Delete("portfolio.pdf");
        }

        [TestMethod]
        public async Task AzurePageBlobStorage_Find()
        {
            var store = CreateStorage();

            await store.Put("bio.doc",       "fred");
            await store.Put("photo.jpg",     "flintstone");
            await store.Put("resume.pdf",    "bedrock");
            await store.Put("portfolio.pdf", "stuff");

            var result = await store.Find("*.*");

            Assert.AreEqual(4, result.Count());

            result = await store.Find("*.pdf");

            Assert.AreEqual(2, result.Count());

            await store.Delete("bio.doc");
            await store.Delete("photo.jpg");
            await store.Delete("resume.pdf");
            await store.Delete("portfolio.pdf");
        }

        [TestMethod]
        public async Task AzurePageBlobStorage_Enumerate()
        {
            var store = CreateStorage();

            await store.Put("docs/bio.doc",       "fred");
            await store.Put("photos/photo.jpg",     "flintstone");
            await store.Put("resumes/resume.pdf",    "bedrock");
            await store.Put("stuff/portfolio.pdf", "stuff");

            var result = new List<string>();

            await store.Enumerate("*.*", async (blob)=>
            {
                result.Add(blob.Name);

                await Task.CompletedTask;
            }, 
            false);

            Assert.AreEqual(4, result.Count());

            Assert.IsTrue(result.Contains("docs/bio.doc"));
            Assert.IsTrue(result.Contains("photos/photo.jpg"));
            Assert.IsTrue(result.Contains("resumes/resume.pdf"));
            Assert.IsTrue(result.Contains("stuff/portfolio.pdf"));

            await store.Delete("docs/bio.doc");
            await store.Delete("photos\\photo.jpg");
            await store.Delete("resumes/resume.pdf");
            await store.Delete("stuff/portfolio.pdf");
        }

        [TestMethod]
        public async Task AzurePageBlobStorage_Enumerate_folder()
        {
            var store  = CreateStorage("cars/chevy");
            var store2 = CreateStorage("cars/pontiac");

            await store2.Put("firebird.tiff",       "fred");
            await store.Put("docs/bio.doc",       "fred");
            await store.Put("photos/photo.jpg",     "flintstone");
            await store.Put("resumes/resume.pdf",    "bedrock");
            await store.Put("stuff/portfolio.pdf", "stuff");

            var result = new List<string>();

            await store.Enumerate("*.*", async (blob)=>
            {
                result.Add(blob.Name);

                await Task.CompletedTask;
            }, 
            false);

            Assert.AreEqual(4, result.Count());

            Assert.IsTrue(result.Contains("docs/bio.doc"));
            Assert.IsTrue(result.Contains("photos/photo.jpg"));
            Assert.IsTrue(result.Contains("resumes/resume.pdf"));
            Assert.IsTrue(result.Contains("stuff/portfolio.pdf"));

            await store.Delete("docs/bio.doc");
            await store.Delete("photos/photo.jpg");
            await store.Delete("resumes/resume.pdf");
            await store.Delete("stuff/portfolio.pdf");
            await store2.Delete("firebird.tiff");
        }

        private IBlobStore CreateStorage(string folder = "")
        { 
            var uid    = Guid.NewGuid().ToString();
            var path   = Path.Combine(_container, uid, folder).Replace("\\", "/");
            var config = TestConfiguration.Load();

            return new AzurePageBlobStorage(config.ConnectionString, path);
        }
    }
}
