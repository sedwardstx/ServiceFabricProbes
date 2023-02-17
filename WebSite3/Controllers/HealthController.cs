using Microsoft.AspNetCore.Mvc;
using System;
using System.Fabric;
using System.Net.Http;
using System.Threading.Tasks;
using System.Threading;
using WebSite3.Utility;
using Microsoft.Extensions.Logging;

namespace WebSite3.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HealthController : ControllerBase
    {
        private readonly ILogger<HealthController> _logger;
        private readonly StatelessServiceContext _serviceContext;
        private readonly HttpClient _httpClient;

        public HealthController(ILogger<HealthController> logger, StatelessServiceContext serviceContext, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _serviceContext = serviceContext;
            _httpClient = httpClientFactory.CreateClient();
        }
        // GET: api/<HealthController>
        [HttpGet]
        public async Task<ActionResult> Get(CancellationToken cancellationToken)
        {
            try
            {
                var address = await ServiceResolver.ResolveParitionedServiceEndpoint(_serviceContext.CodePackageActivationContext.ApplicationName, "WebSite2", 0, cancellationToken);

                // create our Uri
                var latestEpocUri = new Uri(address + "/api/Health");

                using HttpResponseMessage response = await _httpClient.GetAsync(latestEpocUri, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    return Ok();
                } else
                {
                    await Task.Delay(2001, cancellationToken);
                    return BadRequest();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(string.Format("Exception: {0}", ex.Message), ex);
                throw;
            }
        }
    }
}
