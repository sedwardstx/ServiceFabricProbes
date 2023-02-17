using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Fabric;
using System.Threading;
using System.Threading.Tasks;
using WebSite2.Models;

namespace WebSite2.Controllers
{
    public class HomeController : Controller
    {
        private readonly StatefulServiceContext _serviceContext;
        private readonly IReliableStateManager _stateManager;
        private readonly ILogger<HomeController> _logger;

        //dictionaries
        private IReliableDictionary<string, WebModel> _webModel;
        private IReliableDictionary<string, CacheRecord> _webCache;

        public HomeController(ILogger<HomeController> logger, StatefulServiceContext serviceContext, IReliableStateManager stateManager)
        {
            _logger = logger;
            _serviceContext = serviceContext;
            _stateManager = stateManager;
        }

        /// <summary>
        /// Main website endpoint, will return BadRequest until cache loading is complete
        /// see https://docs.microsoft.com/en-us/azure/service-fabric/probes-codepackage#http-probe
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            _webCache = await _stateManager.GetOrAddAsync<IReliableDictionary<string, CacheRecord>>("CacheDictionary");
            _webModel = await _stateManager.GetOrAddAsync<IReliableDictionary<string, WebModel>>("ModelDictionary");

            var cacheEntries = new List<CacheRecord>();

            // Enumerate dictionary and return all cached values
            using (var tx = _stateManager.CreateTransaction())
            {
                var currentWebModel = await _webModel.TryGetValueAsync(tx, "CurrentModel");
                if ((currentWebModel.HasValue) && (currentWebModel.Value.CacheLoaded))
                {
                    ServiceEventSource.Current.ServiceMessage(_serviceContext, "Cache is up-to-date, {0} word's loaded", currentWebModel.Value.WordCount);
                    ServiceEventSource.Current.ServiceMessage(_serviceContext, "Website2 Retrieving cached records");

                    var agentList = await _webCache.CreateEnumerableAsync(tx);
                    var enumerator = agentList.GetAsyncEnumerator();

                    while (await enumerator.MoveNextAsync(cancellationToken))
                    {
                        cacheEntries.Add(enumerator.Current.Value);
                    }
                    await tx.CommitAsync();
                }
                else
                {
                    ServiceEventSource.Current.ServiceMessage(_serviceContext, "Cache still loading....");
                    await tx.CommitAsync();

                    return View();

                    //return new BadRequestResult();
                }
            }

            ServiceEventSource.Current.ServiceMessage(_serviceContext, "Website2 Found {0} cached records", cacheEntries.Count);

            return View(cacheEntries);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
