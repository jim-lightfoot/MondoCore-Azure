/***************************************************************************
 *                                                                          
 *    The MondoCore Libraries  							                    
 *                                                                          
 *        Namespace: MondoCore.AzurStorage				            
 *             File: BaseBlobStorage.cs			 		    		    
 *        Class(es): BaseBlobStorage				           		        
 *          Purpose: Base class for blob storage accounts in Azure Storge                           
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

using Azure;
using Azure.Core;
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
    public abstract class BaseBlobStorage : IBlobStore
    {
        protected BaseBlobStorage(string connectionString, string blobContainerName)
        {
            var folderParts = blobContainerName.Split('/');

            this.ContainerName    = folderParts[0];
            this.ConnectionString = connectionString;

            if(folderParts.Length > 1)
                this.FolderName = string.Join("/", folderParts.Skip(1)).EnsureEndsWith("/");
            else
                this.FolderName = "";
        }

        protected BaseBlobStorage(Uri uri, TokenCredential credential, string path)
        {
            this.Uri        = uri;
            this.Credential = credential;

            if(!string.IsNullOrWhiteSpace(path))
                this.FolderName = path.EnsureEndsWith("/");
        }

        protected string          ConnectionString  { get; }
        protected string          ContainerName     { get; }
        protected string          FolderName        { get; }
        protected Uri             Uri               { get; }
        protected TokenCredential Credential        { get; }

        #region IBlobStore

        /// <summary>
        /// Gets a blob with the given id/path
        /// </summary>
        /// <param name="id">An identifier for the blob. This could be a path.</param>
        /// <param name="encoding">A text encode to use to encode the text</param>
        /// <returns>A string that is the blob</returns>
        public async Task<string> Get(string id, Encoding encoding = null)
        {
            try
            { 
                var blob     = await GetBlobClient(id).ConfigureAwait(false);
                var response = await blob.DownloadAsync().ConfigureAwait(false);

                try
                {
                    return await response.Value.Content.ReadStringAsync(encoding).ConfigureAwait(false);
                }
                finally
                {
                    response.Value.Dispose();
                }
            }
            catch(RequestFailedException ex)
            {
                if(ex.Status == 404)
                    throw new FileNotFoundException("Blob not found", ex);

                throw;
            }
        }

        /// <summary>
        /// Gets a blob with the given id/path
        /// </summary>
        /// <param name="id">An identifier for the blob. This could be a path.</param>
        /// <returns>The blob as an array of bytes</returns>
        public async Task<byte[]> GetBytes(string id)
        {
            try
            { 
                var blob = await GetBlobClient(id).ConfigureAwait(false);

                using(var mem = new MemoryStream())
                { 
                    await blob.DownloadToAsync(mem).ConfigureAwait(false);

                    return mem.ToArray();
                }
            }
            catch(RequestFailedException ex)
            {
                if(ex.Status == 404)
                    throw new FileNotFoundException("Blob not found", ex);

                throw;
            }
        }

        /// <summary>
        /// Reads a blob with the given id/path and writes to the given stream
        /// </summary>
        /// <param name="id">An identifier for the blob. This could be a path.</param>
        /// <param name="destination">Destination stream to write blob to</param>
        public async Task Get(string id, Stream destination)
        {
            try
            { 
                var blob = await GetBlobClient(id).ConfigureAwait(false);

                await blob.DownloadToAsync(destination).ConfigureAwait(false);
            }
            catch(RequestFailedException ex)
            {
                if(ex.Status == 404)
                    throw new FileNotFoundException("Blob not found", ex);

                throw;
            }
        }

        /// <summary>
        /// Opens a readonly stream to a blob with the given id/path 
        /// </summary>
        /// <param name="id">An identifier for the blob. This could be a path.</param>
        /// <returns>A readonly stream to read the blob from</returns>
        public async Task<Stream> OpenRead(string id)
        {
            try
            { 
                var blob = await GetBlobClient(id).ConfigureAwait(false);

                return await blob.OpenReadAsync(new BlobOpenReadOptions(false)).ConfigureAwait(false);
            }
            catch(RequestFailedException ex)
            {
                if(ex.Status == 404)
                    throw new FileNotFoundException("Blob not found", ex);

                throw;
            }
        }

        /// <summary>
        /// Opens a writable stream to a blob with the given id/path 
        /// </summary>
        /// <param name="id">An identifier for the blob. This could be a path.</param>
        /// <returns>A writable stream to write to the blob</returns>
        public async Task<Stream> OpenWrite(string id)
        {
            try
            { 
                var blob = await GetBlobClient(id, true).ConfigureAwait(false);

                return await OpenWrite(blob).ConfigureAwait(false);
            }
            catch(RequestFailedException ex)
            {
                if(ex.Status == 404)
                    throw new FileNotFoundException("Blob not found", ex);

                throw;
            }
        }

        protected internal abstract Task<Stream> OpenWrite(BlobBaseClient client);

        /// <summary>
        /// Puts the string into the blob storage
        /// </summary>
        /// <param name="id">The identifier for the blob. This could be a path.</param>
        /// <param name="blob">The string to store</param>
        public async Task Put(string id, string contents, Encoding encoding = null)
        {
            encoding = encoding ?? UTF8Encoding.UTF8;

            using(var mem = new MemoryStream(encoding.GetBytes(contents)))
            { 
                await Put(id, mem).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Puts the string into the blob storage
        /// </summary>
        /// <param name="id">An identifier for the blob. This could be a path.</param>
        /// <param name="contents">The contents to store</param>
        public abstract Task Put(string id, Stream contents);

        /// <summary>
        /// Deletes the blob from storage
        /// </summary>
        /// <param name="id">An identifier for the blob. This could be a path.</param>
        public async Task Delete(string id)
        {
            try
            { 
                var blob = await GetBlobClient(id).ConfigureAwait(false);

                await blob.DeleteIfExistsAsync().ConfigureAwait(false);
            }
            catch(RequestFailedException ex)
            {
                if(ex.Status == 404)
                    return;

                throw;
            }
        }

        /// <summary>
        /// Finds all blobs that meet the filter 
        /// </summary>
        /// <param name="filter">A file path type filter, e.g. "Policies*.*"</param>
        /// <returns>A collection of the blob ids/paths</returns>
        public async Task<IEnumerable<string>> Find(string filter)
        {
            var result = new List<string>();

            await this.Enumerate(filter, async (blob)=>
            {
                result.Add(blob.Name);

                await Task.CompletedTask;
            },
            
            false).ConfigureAwait(false);

            return result;
        }

        /// <summary>
        /// Finds all blobs that meet the filter 
        /// </summary>
        /// <param name="filter">A file path type filter, e.g. "Policies*.*"</param>
        /// <returns>A collection of the blob ids/paths</returns>
        public async Task Enumerate(string filter, Func<IBlob, Task> fnEach, bool asynchronous = true)
        {
            var        container = this.ContainerClient;
            var        pages     = container.GetBlobs(AzureBlobs.Models.BlobTraits.Metadata).AsPages();
            List<Task> tasks     = asynchronous ? new List<Task>() : null;

            foreach(var page in pages)
            {
                var blobs = page.Values;

                foreach(var blob in blobs)
                {
                    if(blob.Name.MatchesWildcard(filter) && (string.IsNullOrWhiteSpace(this.FolderName) || blob.Name.StartsWith(this.FolderName, StringComparison.InvariantCultureIgnoreCase)))
                    { 
                        var ablob = new AzureBlob(blob);

                        ablob.Name = ablob.Name.Substring(this.FolderName.Length);

                        var task = fnEach(ablob);

                        if(asynchronous)
                            tasks.Add(task);
                        else
                            await task.ConfigureAwait(false);
                    }
                }
            }

            if(asynchronous)
                await Task.WhenAll(tasks).ConfigureAwait(false);

            return;
        }

        #endregion

        #region Private

        protected BlobContainerClient ContainerClient
        {
            get
            { 
                if(this.Uri != null)
                    return new BlobContainerClient(this.Uri, this.Credential);

                return new BlobContainerClient(this.ConnectionString, this.ContainerName);
            }
        }

        protected abstract Task<BlobBaseClient> GetBlobClient(string blobName, bool createIfNotExists = false);

        private class AzureBlob : IBlob
        { 
            private readonly BlobItem _blob;

            internal AzureBlob(BlobItem blob)
            {
                _blob = blob;
                this.Name = blob.Name;
            }

            public string                      Name     { get; set; }
            public bool                        Deleted  => _blob.Deleted;
            public IDictionary<string, string> Metadata => _blob.Metadata;
            public IDictionary<string, string> Tags     => _blob.Tags;
        }

        #endregion
    }
}
