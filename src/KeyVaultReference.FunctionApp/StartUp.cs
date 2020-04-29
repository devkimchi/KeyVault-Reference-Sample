using KeyVaultReference.FunctionApp.Handlers;

using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(KeyVaultReference.FunctionApp.StartUp))]
namespace KeyVaultReference.FunctionApp
{
    public class StartUp : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            this.ConfigureKeyVault(builder.Services);
            this.ConfigureHandlers(builder.Services);
        }

        private void ConfigureKeyVault(IServiceCollection services)
        {
            var azureServiceTokenProvider = new AzureServiceTokenProvider();
            var kv = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(azureServiceTokenProvider.KeyVaultTokenCallback));

            services.AddSingleton<IKeyVaultClient>(kv);
        }

        private void ConfigureHandlers(IServiceCollection services)
        {
            services.AddSingleton<IAppSettingsHandler, AppSettingsHandler>();

        }
    }
}