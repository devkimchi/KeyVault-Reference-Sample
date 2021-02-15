using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

using Newtonsoft.Json;

namespace KeyVaultReference.FunctionApp
{
    public static class SecretsHttpTrigger
    {
        [FunctionName("SecretsHttpTrigger")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "POST", Route = "secrets/disable")] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            // Get the KeyVault URI
            var uri = Environment.GetEnvironmentVariable("KeyVault__Uri");

            // Get the tenant ID where the KeyVault lives
            var tenantId = Environment.GetEnvironmentVariable("KeyVault__TenantId");

            // Set the tenant ID, in case your account has multiple tenants logged in
            var options = new DefaultAzureCredentialOptions()
            {
                SharedTokenCacheTenantId = tenantId,
                VisualStudioTenantId = tenantId,
                VisualStudioCodeTenantId = tenantId,
            };
            var client = new SecretClient(new Uri(uri), new DefaultAzureCredential(options));

            // Get the properties of all secrets
            var properties = await client.GetPropertiesOfSecretsAsync()
                                         .ToListAsync()
                                         .ConfigureAwait(false);

            var utcNow = DateTimeOffset.UtcNow;
            var results = new Dictionary<string, object>();
            foreach (var property in properties)
            {
                // Get the all versions of the given secret
                // Filter only enabled versions
                // Sort by the created date in a reverse order
                var secrets = await client.GetPropertiesOfSecretVersionsAsync(property.Name)
                                          .WhereAwait(p => new ValueTask<bool>(p.Enabled.GetValueOrDefault() == true))
                                          .OrderByDescendingAwait(p => new ValueTask<DateTimeOffset>(p.CreatedOn.GetValueOrDefault()))
                                          .ToListAsync()
                                          .ConfigureAwait(false);

                // Do nothing if there is no version enabled
                if (!secrets.Any())
                {
                    continue;
                }

                // Do nothing if there is only one version enabled
                if (secrets.Count < 2)
                {
                    continue;
                }

                // Do nothing if the latest version was generated less than a day ago
                if (secrets.First().CreatedOn.GetValueOrDefault() <= utcNow.AddDays(-1))
                {
                    continue;
                }

                // Disable all versions except the first (latest) one
                var candidates = secrets.Skip(1).ToList();
                var result = new List<SecretProperties>() { secrets.First() };
                foreach (var candidate in candidates)
                {
                    candidate.Enabled = false;
                    var response = await client.UpdateSecretPropertiesAsync(candidate).ConfigureAwait(false);

                    result.Add(response.Value);
                }

                results.Add(property.Name, result);
            }

            var res = new ContentResult()
            {
                Content = JsonConvert.SerializeObject(results, Formatting.Indented),
                ContentType = "application/json",
            };

            return res;
        }
    }
}
