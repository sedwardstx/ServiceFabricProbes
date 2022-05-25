using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Fabric;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WebSite1.Models;

namespace WebSite1.Controllers
{
    /// <summary>
    /// Main website endpoint, depends on properly configured HTTP LB health probe hitting /api/Health endpoint
    /// </summary>
    public class HomeController : Controller
    {
        private readonly StatefulServiceContext _serviceContext;
        private readonly IReliableStateManager _stateManager;
        private readonly ILogger<HomeController> _logger;
        
        //dictionaries
        private IReliableDictionary<string, CacheRecord> _webCache;

        public HomeController(ILogger<HomeController> logger, StatefulServiceContext serviceContext, IReliableStateManager stateManager)
        {
            _logger = logger;
            _serviceContext = serviceContext;
            _stateManager = stateManager;
        }

        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            _webCache = await _stateManager.GetOrAddAsync<IReliableDictionary<string, CacheRecord>>("CacheDictionary");
            var cacheEntries = new List<CacheRecord>();

            ServiceEventSource.Current.ServiceMessage(_serviceContext, "Website1 Retrieving cached records");
            // Enumerate dictionary and return all cached values
            using (var tx = _stateManager.CreateTransaction())
            {
                var agentList = await _webCache.CreateEnumerableAsync(tx);
                var enumerator = agentList.GetAsyncEnumerator();

                while (await enumerator.MoveNextAsync(cancellationToken))
                {
                    cacheEntries.Add(enumerator.Current.Value);
                }

                await tx.CommitAsync();
            }

            ServiceEventSource.Current.ServiceMessage(_serviceContext, "Website1 Found {0} cached records", cacheEntries.Count);

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
