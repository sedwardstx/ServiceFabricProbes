using Microsoft.AspNetCore.Mvc;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;
using System.Fabric;
using System.Threading.Tasks;
using WebSite2.Models;

namespace WebSite2.Controllers
{
    /// <summary>
    /// Requires you to configure an HTTP probe on the Load Balancer to hit this controller, which will return 200OK after cache has completed loading
    /// Deployed in azure cluster LB will not send traffic to Home endpoint until healthprobe returns 200OK
    /// Deployed in dev (onebox) hard to test since no LB is in the mix, so both Health and Home endpoints are always responding
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class HealthController : ControllerBase
    {
        private readonly StatefulServiceContext _serviceContext;
        private readonly IReliableStateManager _stateManager;

        //dictionaries
        private IReliableDictionary<string, WebModel> _webModel;

        public HealthController(StatefulServiceContext serviceContext, IReliableStateManager stateManager)
        {
            _serviceContext = serviceContext;
            _stateManager = stateManager;
        }

        // GET: api/<HealthController>
        [HttpGet]
        public async Task<ActionResult> Get()
        {
            _webModel = await _stateManager.GetOrAddAsync<IReliableDictionary<string, WebModel>>("ModelDictionary");

            // HTTP health probe, return OK when cache has completed loading.  See RunAsync for cache
            using (var tx = _stateManager.CreateTransaction())
            {
                var currentWebModel = await _webModel.TryGetValueAsync(tx, "CurrentModel");
                if ((currentWebModel.HasValue) && (currentWebModel.Value.CacheLoaded))
                {
                    ServiceEventSource.Current.ServiceMessage(_serviceContext, "Cache is up-to-date, {0} word's loaded", currentWebModel.Value.WordCount);
                    return Ok();
                }
                else
                {
                    ServiceEventSource.Current.ServiceMessage(_serviceContext, "Cache still loading....");
                    return BadRequest(); // new BadRequestResult();
                }
            }
        }
    }
}
