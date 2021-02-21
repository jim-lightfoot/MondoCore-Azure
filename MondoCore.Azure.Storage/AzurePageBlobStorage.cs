/***************************************************************************
 *                                                                          
 *    The MondoCore Libraries  							                    
 *                                                                          
 *        Namespace: MondoCore.AzurStorage				            
 *             File: AzurePageBlobStorage.cs			 		    		    
 *        Class(es): AzurePageBlobStorage				           		        
 *          Purpose: Class for page blob storage in Azure Storge                           
 *                                                                          
 *  Original Author: Jim Lightfoot                                          
 *    Creation Date: 1 Jan 2021                                             
 *                                                                          
 *   Copyright (c) 2021 - Jim Lightfoot, All rights reserved                
 *                                                                          
 *  Licensed under the MIT license:                                         
 *    http://www.opensource.org/licenses/mit-license.php                    
 *                                                                          
 ****************************************************************************/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Azure.Core;
using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using AzureBlobs = Azure.Storage.Blobs;

using MondoCore.Common;

namespace MondoCore.Azure.Storage
{
    /****************************************************************************/
    /****************************************************************************/
    /// <summary>
    /// Azure blob storage
    /// </summary>
    public class AzurePageBlobStorage : BaseBlobStorage
    {
        internal const int PageSize = 512;
        internal const int MaxWrite = 1024 * 1024 * 4;

        public AzurePageBlobStorage(string connectionString, string blobContainerName) : base(connectionString, blobContainerName)
        {
        }

        public AzurePageBlobStorage(Uri uri, TokenCredential credential, string path) : base(uri, credential, path)
        {
        }

        public AzurePageBlobStorage(Uri uri, string path) : this(uri, new ManagedIdentityCredential(), path)
        {
        }

        #region IBlobStore

        /// <summary>
        /// Opens a writable stream to a blob with the given id/path 
        /// </summary>
        /// <param name="id">An identifier for the blob. This could be a path.</param>
        /// <returns>A writable stream to write to the blob</returns>
        protected internal override async Task<Stream> OpenWrite(BlobBaseClient client)
        {
            // ??? need to make PageBlobWriteStream,Output be a prop so I can reset with a new call to OpenWriteAsync after resizing
            var sizeable = new PageBlobSizeable(client as PageBlobClient);
            var storStrm = await (client as PageBlobClient).OpenWriteAsync(true, 0L, new PageBlobOpenWriteOptions { Size = PageSize }).ConfigureAwait(false);
            var stream   = new PageBlobWriteStream(storStrm, sizeable);

            sizeable.Stream = stream;

            return stream;
        }

        /// <summary>
        /// Puts the string into the blob storage
        /// </summary>
        /// <param name="id">An identifier for the blob. This could be a path.</param>
        /// <param name="contents">The contents to store</param>
        public override async Task Put(string id, Stream contents)
        {
            var len          = contents.Length;
            var adjustedSize = (int)(Math.Ceiling((double)len / (double)PageSize) * PageSize);
            var client       = (await GetBlobClient(id).ConfigureAwait(false)) as PageBlobClient;

            using(var strm = await client.OpenWriteAsync(false, 0L, new PageBlobOpenWriteOptions { Size = adjustedSize } ).ConfigureAwait(false))
            {
                var buffer = new byte[MaxWrite];
                var read   = 0L;
                
                while(read < len)
                { 
                    var thisRead = await contents.ReadAsync(buffer, 0, (int)Math.Min(MaxWrite, len - read)).ConfigureAwait(false);

                    if(thisRead == 0)
                        break;

                    var thisWrite = (int)(Math.Ceiling((double)thisRead / (double)PageSize) * PageSize);

                    for(long i = thisRead; i < thisWrite; ++i)
                        buffer[i] = 0;

                    await strm.WriteAsync(buffer, 0, thisWrite).ConfigureAwait(false);
                    read += thisRead;
                }
            }
        }

        #endregion

        protected override async Task<BlobBaseClient> GetBlobClient(string blobName, bool createIfNotExists = false)
        {
            PageBlobClient blob = null;
            
            if(this.Uri != null)
            { 
                var pageUri =  this.Uri.Combine(this.FolderName, blobName);
                
                blob = new PageBlobClient(pageUri, this.Credential);
            }
            else
                blob = new PageBlobClient(this.ConnectionString, this.ContainerName, Path.Combine(this.FolderName, blobName));

            if(createIfNotExists)
                await blob.CreateIfNotExistsAsync(1024).ConfigureAwait(false);

            return blob;
        }

        #region ISizeable

        private class PageBlobSizeable : ISizeable
        {
            private long _size = PageSize;
            private readonly PageBlobClient _client;

            internal PageBlobSizeable(PageBlobClient client)
            {
                _client = client;
            }

            public long Size => _size;
            internal PageBlobWriteStream Stream { get; set; }

            // total length  = 18874368
            // last position = 16777216
            // last write    = 2097152
            public async Task ResizeAsync(long newSize)
            {
                try
                { 
                   var position = this.Stream.Output.Position;

                   this.Stream.Output.Dispose();

                    await _client.ResizeAsync(newSize).ConfigureAwait(false);

                    _size = newSize;

                    var storStrm = await _client.OpenWriteAsync(false, position).ConfigureAwait(false);

                    this.Stream.Output = storStrm;
                }
                catch(Exception ex)
                {
                    throw;
                }

                return;
            }

            public void Resize(long newSize)
            {
                var position = this.Stream.Output.Position;

               this.Stream.Output.Dispose();

                _client.Resize(newSize);

                _size = newSize;

                var storStrm = _client.OpenWrite(false, position);

                this.Stream.Output = storStrm;

                return;
            }
        }

        #endregion
    }
}
