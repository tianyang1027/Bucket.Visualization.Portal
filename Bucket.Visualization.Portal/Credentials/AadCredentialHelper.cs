namespace Bucket.Visualization.Portal.Credentials
{
    using System;
    using System.Security.Cryptography;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;

    using Microsoft.IdentityModel.Clients.ActiveDirectory;
    using Microsoft.Rest;
    using Microsoft.Rest.Azure.Authentication;
    using System.DirectoryServices.AccountManagement;
    using System.DirectoryServices.ActiveDirectory;

    /// <summary>
    /// This is just a simple sample adapted from https://github.com/Azure-Samples/data-lake-analytics-dotnet-auth-options
    /// It's not intended to cover all AAD authenticaion scenarios. Please use it at your own risk.
    /// For more information on AAD authentication please refer to:
    /// https://github.com/Azure/azure-sdk-for-net/
    /// https://docs.microsoft.com/en-us/azure/active-directory/ 
    /// </summary>
    public static class AadCredentialHelper
    {
        /// <summary>
        /// tenant ID: 72f988bf-86f1-41af-91ab-2d7cd011db47
        /// Please change this if you would like to use another domain/tenant.
        /// </summary>
        public const string DomainOrTenantId = "microsoft.onmicrosoft.com";
        private const string ClientId = "1950a258-227b-4e31-a9cf-717495945fc2"; // App ID for Microsoft Azure PowerShell

        private static readonly TokenCacheWrapper WrapperedTokenCache = new TokenCacheWrapper();
        /// <summary>
        /// Client id also called application id on Azure portal. 1950a258-227b-4e31-a9cf-717495945fc2 is the App ID for Microsoft Azure PowerShell.
        /// This is used in the GetCredentialFromPrompt() method only. No need to change this.
        /// If you do need to create a new application for GetCredentialFromPrompt(), please take a look at https://microsoft.sharepoint.com/teams/CSEOAAD first.
        /// 
        /// For more information on AAD authentication please refer to:
        /// https://docs.microsoft.com/en-us/azure/data-lake-store/data-lake-store-end-user-authenticate-using-active-directory
        /// https://docs.microsoft.com/en-us/azure/data-lake-store/data-lake-store-service-to-service-authenticate-using-active-directory
        /// https://docs.microsoft.com/en-us/azure/active-directory/develop/howto-create-service-principal-portal
        /// https://docs.microsoft.com/en-us/azure/active-directory/fundamentals/active-directory-whatis
        /// </summary>
        public const string GetCredentialFromPromptClientId = "1950a258-227b-4e31-a9cf-717495945fc2";

        /// <summary>
        /// RedirectUri is a property of the App. 
        /// The URI below is the value of the above Microsoft Azure PowerShell App.
        /// Please change this to the RedirectUri of your App if you are using your own.
        /// For more information please refer to 
        /// https://docs.microsoft.com/en-us/azure/data-lake-store/data-lake-store-end-user-authenticate-using-active-directory
        /// </summary>
        public static readonly Uri ClientRedirectUri = new Uri("urn:ietf:wg:oauth:2.0:oob");

        /// <summary>
        /// To grant a newly created App the permission to use this ADL audience, please refer to
        /// https://docs.microsoft.com/en-us/azure/data-lake-store/data-lake-store-end-user-authenticate-using-active-directory
        /// </summary>
        public static readonly Uri AdlTokenAudience = new Uri(@"https://datalake.azure.net/");

        internal static readonly ActiveDirectoryClientSettings GetCredentialFromPromptClientSettings = new ActiveDirectoryClientSettings()
        {
            ClientId = GetCredentialFromPromptClientId,
            ClientRedirectUri = ClientRedirectUri,
        };

        internal static readonly ActiveDirectoryServiceSettings ServiceSettings;

        static AadCredentialHelper()
        {
            ServiceSettings = ActiveDirectoryServiceSettings.Azure;
            ServiceSettings.TokenAudience = AdlTokenAudience;
        }

#if net452
        public static ServiceClientCredentials GetCredentialFromPrompt()
        {
            SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());

            // might need to specify the username as the 4th param if you need to switch user instead of the cached one
            return UserTokenProvider.LoginWithPromptAsync(DomainOrTenantId, GetCredentialFromPromptClientSettings, ServiceSettings).GetAwaiter().GetResult();
        }
#endif

#if !net452
        public static ServiceClientCredentials GetCredentialFromPrompt()
        {
            SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());
            return UserTokenProvider.LoginByDeviceCodeAsync(ClientId, DomainOrTenantId, ServiceSettings, WrapperedTokenCache.TokenCache, DeviceCodeHandler).GetAwaiter().GetResult();
        }
#endif

        public static ServiceClientCredentials GetCredentialFromClientSecret(string clientId, string clientSecret)
        {
            SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());
            return ApplicationTokenProvider.LoginSilentAsync(DomainOrTenantId, clientId, clientSecret, ServiceSettings).GetAwaiter().GetResult();
        }

        public static ServiceClientCredentials GetCredentialFromCertificate(string clientId, X509Certificate2 cert)
        {
            SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());

#if net452
            return ApplicationTokenProvider.LoginSilentWithCertificateAsync(DomainOrTenantId, new ClientAssertionCertificate(clientId, cert), ServiceSettings).GetAwaiter().GetResult();
#endif

#if !net452
            return ApplicationTokenProvider.LoginSilentWithCertificateAsync(DomainOrTenantId, new Microsoft.Rest.Azure.Authentication.ClientAssertionCertificate(clientId, cert), ServiceSettings).GetAwaiter().GetResult();
#endif
        }

#if !net452
        private static bool DeviceCodeHandler(DeviceCodeResult result)
        {
            Console.WriteLine(result.Message);
            return true;
        }

        public static ServiceClientCredentials GetCredentialCache()
        {
            SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());
            string localUserUpn;
            try
            {
                localUserUpn = UserPrincipal.Current.UserPrincipalName;
            }
            catch (InvalidCastException)
            {
                throw;
            }

            return UserTokenProvider.CreateCredentialsFromCache(ClientId, DomainOrTenantId, localUserUpn, ServiceSettings, WrapperedTokenCache.TokenCache).GetAwaiter().GetResult();
        }

        public static ServiceClientCredentials GetCredential()
        {
            ServiceClientCredentials cred;
            if (!WrapperedTokenCache.Loaded)
            {
                cred = GetCredentialFromPrompt();
            }
            else
            {
                try
                {
                    cred = GetCredentialCache();
                }
                catch (AuthenticationException)
                {
                    Console.WriteLine("Failed to authenticate with cached token. Use device code authentication instead");
                    cred = GetCredentialFromPrompt();
                }
            }

            return cred;
        }
#endif
    }


    class TokenCacheWrapper
    {
        private static readonly byte[] AdditionalEntropy = { 9, 8, 7, 6, 5 };

        public TokenCacheWrapper()
        {
            TokenCache = new TokenCache();
            var tokenCachePath = GetTokenCachePath();
            byte[] diskTokenBytes = null;
            if (File.Exists(tokenCachePath))
            {
                var encryptedTokenBytes = File.ReadAllBytes(tokenCachePath);
                try
                {
                    diskTokenBytes = ProtectedData.Unprotect(encryptedData: encryptedTokenBytes, optionalEntropy: AdditionalEntropy, scope: DataProtectionScope.CurrentUser);
                }
                catch (CryptographicException)
                {
                    Console.WriteLine("Failed to decrypt old token cache, ignoring it");
                }
            }

            if (diskTokenBytes != null)
            {
                TokenCache.Deserialize(diskTokenBytes);
                Loaded = true;
            }

            TokenCache.AfterAccess += notificationArgs =>
            {
                if (TokenCache.HasStateChanged)
                {
                    var tokenBytes = notificationArgs.TokenCache.Serialize();
                    var encryptedTokenBytes = ProtectedData.Protect(userData: tokenBytes, optionalEntropy: AdditionalEntropy, scope: DataProtectionScope.CurrentUser);
                    File.WriteAllBytes(tokenCachePath, encryptedTokenBytes);
                    TokenCache.HasStateChanged = false;
                }
            };
        }

        public TokenCache TokenCache { get; }

        public bool Loaded { get; private set; }

        private static string GetTokenCachePath()
        {
            var myDocumentsFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var tokenCacheFolder = Path.Combine(myDocumentsFolder, "TokenCache");
            if (!Directory.Exists(tokenCacheFolder))
            {
                Directory.CreateDirectory(tokenCacheFolder);
            }

            var tokenCachePath = Path.Combine(tokenCacheFolder, "my.buketvisualportal.aadl.tokencache");
            return tokenCachePath;
        }
    }
}
