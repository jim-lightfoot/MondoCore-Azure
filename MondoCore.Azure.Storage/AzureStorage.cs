/***************************************************************************
 *                                                                          
 *    The MondoCore Libraries  							                    
 *                                                                          
 *        Namespace: MondoCore.AzurStorage				            
 *             File: AzureStorage.cs			 		    		    
 *        Class(es): AzureStorage				           		        
 *          Purpose: Class for blob storage in Azure Storge                           
 *                                                                          
 *  Original Author: Jim Lightfoot                                          
 *    Creation Date: 15 Aug 2020                                             
 *                                                                          
 *   Copyright (c) 2020-2021 - Jim Lightfoot, All rights reserved                
 *                                                                          
 *  Licensed under the MIT license:                                         
 *    http://www.opensource.org/licenses/mit-license.php                    
 *                                                                          
 ****************************************************************************/

using System;
using System.IO;
using System.Threading.Tasks;

using Azure.Core;
using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;

using MondoCore.Common;

namespace MondoCore.Azure.Storage
{
    /****************************************************************************/
    /****************************************************************************/
    /// <summary>
    /// Azure blob storage
    /// </summary>
    public class AzureStorage : BaseBlobStorage
    {
        public AzureStorage(string connectionString, string blobContainerName) : base(connectionString, blobContainerName)
        {
        }

        public AzureStorage(Uri uri, TokenCredential credential, string path) : base(uri, credential, path)
        {
        }

        public AzureStorage(Uri uri, string tenantId, string clientId, string secret, string path) : base(uri, new ClientSecretCredential(tenantId, clientId, secret), path)
        {
        }

        public AzureStorage(Uri uri, string path) : this(uri, new ManagedIdentityCredential(), path)
        {
        }

        #region IBlobStore

        /// <summary>
        /// Opens a writable stream to a blob with the given id/path 
        /// </summary>
        /// <param name="id">An identifier for the blob. This could be a path.</param>
        /// <returns>A writable stream to write to the blob</returns>
        protected internal override Task<Stream> OpenWrite(BlobBaseClient client)
        {
            throw new NotSupportedException("Cannot open a write stream on this type of Azure Blob Storage. Use AzurePageBlobStorage.");
        }

        /// <summary>
        /// Puts the string into the blob storage
        /// </summary>
        /// <param name="id">An identifier for the blob. This could be a path.</param>
        /// <param name="contents">The contents to store</param>
        public override async Task Put(string id, Stream contents)
        {
            var blob = (await GetBlobClient(id).ConfigureAwait(false)) as BlobClient;

            await blob.UploadAsync(contents).ConfigureAwait(false);
        }

        #endregion

        protected override Task<BlobBaseClient> GetBlobClient(string blobName, bool createIfNotExists = false)
        { 
            BlobClient blob = null;

            if(this.Uri != null)
            { 
                var pageUri =  this.Uri.Combine(this.FolderName, blobName);

                blob = new BlobClient(pageUri, this.Credential);
            }
            else
                blob = new BlobClient(this.ConnectionString, this.ContainerName, Path.Combine(this.FolderName, blobName));

            return Task.FromResult(blob as BlobBaseClient);
        }
    }
}
