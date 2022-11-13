# PTI.Microservices.Library.AzureBlobStorage

This is part of PTI.Microservices.Library set of packages

The purpose of this package is to facilitate the calls to Azure Blob Storage APIs, while maintaining a consistent usage pattern among the different services in the group

**Examples:**

## Get Service Sas Uri For Container

    AzureBlobStorageService azureBlobStorageService =
       new AzureBlobStorageService(null, this.AzureBlobStorageConfiguration,
       new Microservices.Library.Interceptors.CustomHttpClient(
       new Microservices.Library.Interceptors.CustomHttpClientHandler(null))
                           );
    var containerSasToken = await azureBlobStorageService.GetServiceSasUriForContainerAsync("testscontainer",
       Azure.Storage.Sas.BlobContainerSasPermissions.Read);

## Get Service Sas Uri For Blob
    AzureBlobStorageService azureBlobStorageService =
       new AzureBlobStorageService(null, this.AzureBlobStorageConfiguration,
       new Microservices.Library.Interceptors.CustomHttpClient(
       new Microservices.Library.Interceptors.CustomHttpClientHandler(null)));
	
    var blobSasToken = await azureBlobStorageService.GetServiceSasUriForBlobAsync("testscontainer",
       "unittests/subdir/testfile.png", Azure.Storage.Sas.BlobSasPermissions.Read);

## Get File Stream
    AzureBlobStorageService azureBlobStorageService =
       new AzureBlobStorageService(null, this.AzureBlobStorageConfiguration,
       new Microservices.Library.Interceptors.CustomHttpClient(
       new Microservices.Library.Interceptors.CustomHttpClientHandler(null)));
	
    List<BlobHierarchyItem> allBlobs = new List<BlobHierarchyItem>();
    var fileStream = System.IO.File.OpenRead(GetFileStreamFilePath);
    var result =
       await azureBlobStorageService.UploadFileAsync("testscontainer", "unittests/subdir/testfile.png", fileStream,
       overwrite: true);
    MemoryStream memoryStream = new MemoryStream();
    var response = await azureBlobStorageService.GetFileStreamAsync("testscontainer", 
       "unittests/subdir/testfile.png", memoryStream);

## Upload File
    CustomHttpClient customHttpClient = new CustomHttpClient(
       new CustomHttpClientHandler(null));
    customHttpClient.Timeout = TimeSpan.FromMinutes(60);
    AzureBlobStorageService azureBlobStorageService =
       new AzureBlobStorageService(null, this.AzureBlobStorageConfiguration,
       customHttpClient);
    var fileStream = System.IO.File.OpenRead(LocalFilePath);
    var result =
       await azureBlobStorageService.UploadFileAsync("testscontainer", "unittests/subdir/testfile.png", fileStream,
       overwrite: true);