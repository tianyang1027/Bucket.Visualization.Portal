namespace Bucket.Visualization.Portal.Credentials
{
    using System.Security.Cryptography.X509Certificates;
    using Microsoft.Rest;

    class CosmosCredentials
    {
        public CosmosCredentials(X509Certificate2 certificate)
        {
            Certificate = certificate;
        }

        public CosmosCredentials(ServiceClientCredentials serviceClientCredentials)
        {
            ServiceClientCredentials = serviceClientCredentials;
        }

        public X509Certificate2 Certificate { get; }

        public ServiceClientCredentials ServiceClientCredentials { get; }
    }
}
