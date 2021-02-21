using Microsoft.VisualStudio.TestTools.UnitTesting;

using System;
using System.Linq;
using System.Threading.Tasks;
using System.Text;
using System.IO;

using MondoCore.Common;
using MondoCore.Azure.Storage;
using System.Reflection.Metadata;
using System.Collections.Generic;

namespace MondoCore.Azure.Storage.FunctionalTests
{
    [TestClass]
    [TestCategory("Functional Tests")]
    public class PageBlobWriteStreamTests : ISizeable
    {
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task PageBlobWriteStream_WriteAsync_null()
        {
            using(var output = new MemoryStream())
            { 
                var val = CreateLongString(127);

                using(var strm = CreateStream(output))
                { 
                    var bytes = UTF8Encoding.UTF8.GetBytes(val);

                    await strm.WriteAsync(null, 0, bytes.Length);
                }
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public async Task PageBlobWriteStream_WriteAsync_outofrange()
        {
            using(var output = new MemoryStream())
            { 
                var val = CreateLongString(127);

                using(var strm = CreateStream(output))
                { 
                    var bytes = UTF8Encoding.UTF8.GetBytes(val);

                    await strm.WriteAsync(bytes, bytes.Length, bytes.Length);
                }
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public async Task PageBlobWriteStream_WriteAsync_count()
        {
            using(var output = new MemoryStream())
            { 
                var val = CreateLongString(127);

                using(var strm = CreateStream(output))
                { 
                    var bytes = UTF8Encoding.UTF8.GetBytes(val);

                    await strm.WriteAsync(bytes, 130, 98);
                }
            }
        }

        [TestMethod]
        public async Task PageBlobWriteStream_WriteAsync()
        {
            using(var output = new MemoryStream())
            { 
                var val = CreateLongString(127);

                await using(var strm = CreateStream(output))
                { 
                    var bytes = UTF8Encoding.UTF8.GetBytes(val);

                    await strm.WriteAsync(bytes, 0, bytes.Length);
                }

                Assert.AreEqual(val, await output.ReadStringAsync());
            }
        }

        [TestMethod]
        public async Task PageBlobWriteStream_WriteAsync_long()
        {
            using(var output = new MemoryStream())
            { 
                var val = CreateLongString();

                using(var strm = CreateStream(output))
                { 
                    var bytes = UTF8Encoding.UTF8.GetBytes(val);

                    await strm.WriteAsync(bytes, 0, bytes.Length);
                }

                Assert.AreEqual(val, await output.ReadStringAsync());
            }
        }

        private Stream CreateStream(Stream output)
        {
            return new PageBlobWriteStream(output, this);
        }

        private string CreateLongString(int length = (int)(4096 * 1024 * 4.5))
        {
            StringBuilder sb = new StringBuilder();
            var iterations = length / 36;

            for(var i = 0; i < iterations; ++i)
                sb.Append("abcdefghijklmnopqrstuvwxyz0123456789");

            return sb.ToString();
        }

        private long _size = 512;

        public long Size => _size;


        public Task ResizeAsync(long newSize)
        {
            _size = newSize;

            return Task.CompletedTask;
        }

        public void Resize(long newSize)
        {
            _size = newSize;
        }
    }
}
