using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
    public static class DisableSecretHttpTrigger
    {
        [FunctionName("DisableSecretHttpTrigger")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "POST", Route = "secrets/{name}/disable/{count:int?}")] HttpRequest req,
            string name,
            int? count,
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

            // Get the all versions of the given secret
            // Filter only enabled versions
            // Sort by the created date in a reverse order
            var versions = await client.GetPropertiesOfSecretVersionsAsync(name)
                                       .WhereAwait(p => new ValueTask<bool>(p.Enabled.GetValueOrDefault() == true))
                                       .OrderByDescendingAwait(p => new ValueTask<DateTimeOffset>(p.CreatedOn.GetValueOrDefault()))
                                       .ToListAsync()
                                       .ConfigureAwait(false);

            // Do nothing if there is no version enabled
            if (!versions.Any())
            {
                return new AcceptedResult();
            }

            if (!count.HasValue)
            {
                count = 2;
            }

            // Do nothing if there is only given number of versions enabled
            if (versions.Count < count.Value + 1)
            {
                return new AcceptedResult();
            }

            // Disable all versions except the first (latest) given number of versions
            var candidates = versions.Skip(count.Value).ToList();
            var results = new List<SecretProperties>();
            results.AddRange(versions.Take(count.Value));
            foreach (var candidate in candidates)
            {
                candidate.Enabled = false;
                var response = await client.UpdateSecretPropertiesAsync(candidate).ConfigureAwait(false);

                results.Add(response.Value);
            }

            var res = new ContentResult()
            {
                Content = JsonConvert.SerializeObject(results, Formatting.Indented),
                ContentType = "application/json",
                StatusCode = (int)HttpStatusCode.OK,
            };

            return res;
        }
    }
}
