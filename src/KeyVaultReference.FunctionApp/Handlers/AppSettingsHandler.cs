using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Microsoft.Azure.KeyVault;
using Microsoft.Azure.KeyVault.Models;

namespace KeyVaultReference.FunctionApp.Handlers
{
    public interface IAppSettingsHandler
    {
        Task<string> GetValueAsync(string key);
    }

    public class AppSettingsHandler : IAppSettingsHandler
    {
        private static Regex regexSecretUri = new Regex(@"\@Microsoft\.KeyVault\(SecretUri\=(.*)\)", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);
        private static Regex regexVaultName = new Regex(@"\@Microsoft\.KeyVault\(VaultName\=(.*);\s*SecretName\=(.*);\s*SecretVersion\=(.*)\)", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);

        private readonly IKeyVaultClient _kv;

        public AppSettingsHandler(IKeyVaultClient kv)
        {
            this._kv = kv ?? throw new ArgumentNullException(nameof(kv));
        }

        public async Task<string> GetValueAsync(string key)
        {
            var reference = Environment.GetEnvironmentVariable(key);
            if (!this.IsKeyVaultReference(reference))
            {
                return reference;
            }

            var bundle = default(SecretBundle);
            var match = regexSecretUri.Match(reference);
            if (match.Success)
            {
                var uri = match.Groups[1].Value;
                bundle = await this._kv.GetSecretAsync(uri).ConfigureAwait(false);

                return bundle.Value;
            }

            match = regexVaultName.Match(reference);
            if (match.Success)
            {
                var vaultName = match.Groups[1].Value;
                var secretName = match.Groups[2].Value;
                var secretVersion = match.Groups[3].Value;
                bundle = await this._kv.GetSecretAsync($"https://{vaultName}.vault.azure.net", secretName, secretVersion).ConfigureAwait(false);

                return bundle.Value;
            }

            return null;
        }

        private bool IsKeyVaultReference(string value)
        {
            return value.StartsWith("@Microsoft.KeyVault(");
        }
    }
}