using System;
using System.Threading.Tasks;

using KeyVaultReference.FunctionApp.Handlers;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace KeyVaultReference.FunctionApp
{
    public class SampleHttpTrigger
    {
        private readonly IAppSettingsHandler _handler;
        private readonly ILogger<SampleHttpTrigger> _logger;

        public SampleHttpTrigger(IAppSettingsHandler handler, ILogger<SampleHttpTrigger> logger)
        {
            this._handler = handler ?? throw new ArgumentNullException(nameof(handler));
            this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [FunctionName(nameof(SampleHttpTrigger.GetSecrets))]
        public async Task<IActionResult> GetSecrets(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "secrets")] HttpRequest req)
        {
            this._logger.LogInformation("C# HTTP trigger function processed a request.");

            var hello = await this._handler.GetValueAsync("Hello").ConfigureAwait(false);
            var hello2 = await this._handler.GetValueAsync("Hello2").ConfigureAwait(false);
            var lorem = await this._handler.GetValueAsync("Lorem").ConfigureAwait(false);
            var lorem2 = await this._handler.GetValueAsync("Lorem2").ConfigureAwait(false);

            var result = new { hello = hello, hello2 = hello2, lorem = lorem, lorem2 = lorem2 };

            return new OkObjectResult(result);
        }
    }
}
