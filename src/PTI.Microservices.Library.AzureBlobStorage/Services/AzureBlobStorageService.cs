using Azure.Core.Pipeline;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Azure.Storage.Sas;
using Microsoft.Extensions.Logging;
using PTI.Microservices.Library.Configuration;
using PTI.Microservices.Library.Interceptors;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PTI.Microservices.Library.Services
{
    /// <summary>
    /// Service in cahrge of exposing access to Azure Blob Storage
    /// </summary>
    public class AzureBlobStorageService
    {
        private ILogger<AzureBlobStorageService> Logger { get; }
        private AzureBlobStorageConfiguration AzureBlobStorageConfiguration { get; }
        private CustomHttpClient CustomHttpClient { get; }
        private BlobServiceClient BlobServiceClient { get; }

        /// <summary>
        /// Creates a new instance of <see cref="AzureBlobStorageService"/>
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="azureBlobStorageConfiguration"></param>
        /// <param name="customHttpClient"></param>
        public AzureBlobStorageService(ILogger<AzureBlobStorageService> logger, AzureBlobStorageConfiguration azureBlobStorageConfiguration,
            CustomHttpClient customHttpClient, int retries = 3)
        {
            this.Logger = logger;
            this.AzureBlobStorageConfiguration = azureBlobStorageConfiguration;
            this.CustomHttpClient = customHttpClient;
            BlobClientOptions blobClientOptions = new BlobClientOptions()
            {
                Transport = new HttpClientTransport(customHttpClient),
            };
            blobClientOptions.Retry.MaxRetries = retries;
            blobClientOptions.Retry.NetworkTimeout = customHttpClient.Timeout;
            this.BlobServiceClient =
                new BlobServiceClient(this.AzureBlobStorageConfiguration.ConnectionString,
                options: blobClientOptions);
        }


        /// <summary>
        /// Upload a file to the specified container
        /// </summary>
        /// <param name="containerName"></param>
        /// <param name="fileRelativePath"></param>
        /// <param name="file">File relative path e.g. folder/subfolder/file</param>
        /// <param name="overwrite"></param>
        /// <param name="cancellationToken"></param>
        public async Task<BlobContentInfo> UploadFileAsync(string containerName, string fileRelativePath, Stream file,
            bool overwrite,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var blobClient = this.BlobServiceClient.GetBlobContainerClient(containerName)
                    .GetBlobClient(fileRelativePath);
                var response = await blobClient.UploadAsync(file, overwrite: overwrite, cancellationToken);
                return response.Value;
            }
            catch (Exception ex)
            {
                this.Logger?.LogError(ex.Message, ex);
                throw;
            }
        }


        /// <summary>
        /// Lists all files in the given container
        /// </summary>
        /// <param name="containerName"></param>
        /// <param name="prefix"></param>
        /// <param name="delimiter"></param>
        /// <param name="continuationToken"></param>
        /// <param name="pageSizeHint"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public IAsyncEnumerable<Azure.Page<BlobHierarchyItem>> ListFilesAsync(string containerName,
            string prefix = null, string delimiter = null,
            string continuationToken = null,
            int pageSizeHint = 10, CancellationToken cancellationToken = default)
        {
            try
            {
                var blobContainerClient = this.BlobServiceClient.GetBlobContainerClient(containerName);
                var pagesSegment = blobContainerClient.GetBlobsByHierarchyAsync(
                    prefix: prefix,
                    delimiter: delimiter, cancellationToken: cancellationToken)
                    .AsPages(continuationToken, pageSizeHint);
                return pagesSegment;
            }
            catch (Exception ex)
            {
                this.Logger?.LogError(ex.Message, ex);
                throw;
            }
        }


        /// <summary>
        /// Deletes the specified image
        /// </summary>
        /// <param name="containerName"></param>
        /// <param name="blobName"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<Azure.Response> DeleteFileAsync(string containerName, string blobName,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var bloblContainerClient = this.BlobServiceClient.GetBlobContainerClient(containerName);
                if (!await bloblContainerClient.ExistsAsync())
                    throw new Exception($"Container: {containerName} does not exist");
                var blobClient = bloblContainerClient.GetBlobClient(blobName);
                if (!await blobClient.ExistsAsync())
                    throw new Exception($"Blob: {bloblContainerClient} does not exist in container: {containerName}");
                var response = await bloblContainerClient.DeleteBlobAsync(blobName);
                return response;
            }
            catch (Exception ex)
            {
                this.Logger?.LogError(ex.Message, ex);
                throw;
            }
        }

        /// <summary>
        /// Retrieves the specified file
        /// </summary>
        /// <param name="containerName"></param>
        /// <param name="blobName"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<Azure.Response> GetFileStreamAsync(string containerName, string blobName, Stream outputStream,
            CancellationToken cancellationToken = default)
        {
            var bloblContainerClient = this.BlobServiceClient.GetBlobContainerClient(containerName);
            if (!await bloblContainerClient.ExistsAsync())
                throw new Exception($"Container: {containerName} does not exist");
            var blobClient = bloblContainerClient.GetBlobClient(blobName);
            if (!await blobClient.ExistsAsync())
                throw new Exception($"Blob: {bloblContainerClient} does not exist in container: {containerName}");
            var response = await blobClient.DownloadToAsync(outputStream, cancellationToken);
            return response;
        }

        public async Task<Uri> GetServiceSasUriForContainerAsync(string containerName,
            BlobContainerSasPermissions blobContainerSasPermissions,
            string storedPolicyName = null, CancellationToken cancellationToken = default)
        {
            var containerClient = this.BlobServiceClient.GetBlobContainerClient(containerName);
            if (!await containerClient.ExistsAsync(cancellationToken: cancellationToken))
                throw new Exception($"Container: {containerName} does not exist");
            // Check whether this BlobContainerClient object has been authorized with Shared Key.
            if (containerClient.CanGenerateSasUri)
            {
                // Create a SAS token that's valid for one hour.
                BlobSasBuilder sasBuilder = new BlobSasBuilder()
                {
                    BlobContainerName = containerClient.Name,
                    Resource = "c"
                };

                if (storedPolicyName == null)
                {
                    sasBuilder.ExpiresOn = DateTimeOffset.UtcNow.AddHours(1);
                    sasBuilder.SetPermissions(blobContainerSasPermissions);
                }
                else
                {
                    sasBuilder.Identifier = storedPolicyName;
                }

                Uri sasUri = containerClient.GenerateSasUri(sasBuilder);

                return sasUri;
            }
            else
            {
                //Console.WriteLine(@"BlobContainerClient must be authorized with Shared Key 
                //          credentials to create a service SAS.");
                return null;
            }
        }

        public async Task<Uri> GetServiceSasUriForBlobAsync(string containerName, string blobName,
            BlobSasPermissions blobSasPermissions,
            string storedPolicyName = null, CancellationToken cancellationToken = default)
        {
            var containerClient = this.BlobServiceClient.GetBlobContainerClient(containerName);
            if (!await containerClient.ExistsAsync(cancellationToken: cancellationToken))
                throw new Exception($"Container: {containerName} does not exist");
            var blobClient = containerClient.GetBlobClient(blobName);
            if (!await blobClient.ExistsAsync(cancellationToken: cancellationToken))
                throw new Exception($"Blob: {containerClient} does not exist in container: {containerName}");
            // Check whether this BlobClient object has been authorized with Shared Key.
            if (blobClient.CanGenerateSasUri)
            {
                // Create a SAS token that's valid for one hour.
                BlobSasBuilder sasBuilder = new BlobSasBuilder()
                {
                    BlobContainerName = blobClient.GetParentBlobContainerClient().Name,
                    BlobName = blobClient.Name,
                    Resource = "b"
                };

                if (storedPolicyName == null)
                {
                    sasBuilder.ExpiresOn = DateTimeOffset.UtcNow.AddHours(1);
                    sasBuilder.SetPermissions(blobSasPermissions);
                }
                else
                {
                    sasBuilder.Identifier = storedPolicyName;
                }

                Uri sasUri = blobClient.GenerateSasUri(sasBuilder);

                return sasUri;
            }
            else
            {
                //Console.WriteLine(@"BlobClient must be authorized with Shared Key 
                //          credentials to create a service SAS.");
                return null;
            }
        }
    }
}
