using Bucket.Visualization.Portal.Credentials;
using Microsoft.Azure.Management.CosmosDB.Models;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Rest;
using System.Security.Cryptography.X509Certificates;
using VcClient;

namespace Bucket.Visualization.Portal
{
    public class CosmosFileProvider
    {
        private readonly X509Certificate2? _certificate;
        public bool StreamExists(CosmosResourceLocator crl)
        {
            Setup(crl);
            return VC.StreamExists(crl.Url);
        }

        private void Setup(CosmosResourceLocator crl)
        {
            if (_certificate != null)
            {
                VC.Setup(crl.VirtualCluster, _certificate);
            }
            else
            {
                var cred = AadCredentialHelper.GetCredentialFromPrompt();
                VC.SetupAadCredentials(null, null, cred);
            }
        }

        public StreamInfo GetStream(CosmosResourceLocator crl)
        {
            Setup(crl);
            return VC.GetStreamInfo(crl.Url, false);
        }

        public Stream ReadStream(CosmosResourceLocator crl)
        {
            Setup(crl);
            return VC.ReadStream(crl.Url, false);
        }

    }
}
