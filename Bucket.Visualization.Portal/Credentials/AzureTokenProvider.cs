namespace Bucket.Visualization.Portal.Credentials
{
    using System.Net.Http.Headers;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Services.AppAuthentication;
    using Microsoft.Rest;

    class AzureTokenProvider : ITokenProvider
    {
        private readonly AzureServiceTokenProvider azureServiceTokenProvider;

        private readonly string resource;

        public AzureTokenProvider(AzureServiceTokenProvider azureServiceTokenProvider, string resource)
        {
            this.azureServiceTokenProvider = azureServiceTokenProvider;
            this.resource = resource;
        }

        public async Task<AuthenticationHeaderValue> GetAuthenticationHeaderAsync(CancellationToken cancellationToken)
        {
            var token = await this.azureServiceTokenProvider.GetAccessTokenAsync(resource, "microsoft.onmicrosoft.com").ConfigureAwait(false);
            return new AuthenticationHeaderValue("Bearer", token);
        }
    }
}
