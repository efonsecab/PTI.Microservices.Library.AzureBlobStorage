using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PTI.Microservices.Library.Configuration
{
    /// <summary>
    /// Configuration for Azure Blob Storage Service
    /// </summary>
    public class AzureBlobStorageConfiguration
    {
        /// <summary>
        /// Connection String
        /// </summary>
        public string ConnectionString { get; set; }
    }
}
