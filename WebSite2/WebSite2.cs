using System;
using System.Collections.Generic;
using System.Fabric;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.ServiceFabric.Services.Communication.AspNetCore;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;
using WebSite2.Models;

namespace WebSite2
{
    /// <summary>
    /// The FabricRuntime creates an instance of this class for each service type instance. 
    /// </summary>
    internal sealed class WebSite2 : StatefulService
    {
        private IReliableDictionary<string, CacheRecord> _webCache;
        private IReliableDictionary<string, WebModel> _webModel;
        private const int ENTRIES_TO_CACHE = 180;

        public WebSite2(StatefulServiceContext context)
            : base(context)
        { }

        /// <summary>
        /// Optional override to create listeners (like tcp, http) for this service instance.
        /// </summary>
        /// <returns>The collection of listeners.</returns>
        protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
        {
            return new ServiceReplicaListener[]
            {
                new ServiceReplicaListener(serviceContext =>
                    new KestrelCommunicationListener(serviceContext, "ServiceEndpoint", (url, listener) =>
                    {
                        ServiceEventSource.Current.ServiceMessage(serviceContext, $"WebSite2 Starting Kestrel on {url}");

                        return new WebHostBuilder()
                                    .UseKestrel()
                                    .ConfigureServices(
                                        services => services
                                            .AddSingleton<StatefulServiceContext>(serviceContext)
                                            .AddSingleton<IReliableStateManager>(this.StateManager))
                                    .UseContentRoot(Directory.GetCurrentDirectory())
                                    .UseStartup<Startup>()
                                    .UseServiceFabricIntegration(listener, ServiceFabricIntegrationOptions.None)
                                    .UseUrls(url)
                                    .Build();
                    }))
            };
        }

        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            _webModel = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, WebModel>>("ModelDictionary");
            _webCache = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, CacheRecord>>("CacheDictionary");

            cancellationToken.ThrowIfCancellationRequested();

            ServiceEventSource.Current.ServiceMessage(this.Context, "WebSite2 Checking for cached records");
            using (var tx = this.StateManager.CreateTransaction())
            {
                var currentWebModel = await _webModel.TryGetValueAsync(tx, "CurrentModel");
                if ((currentWebModel.HasValue) && (currentWebModel.Value.CacheLoaded))
                {
                    ServiceEventSource.Current.ServiceMessage(this.Context, "WebSite2 Cache is up-to-date, {0} words loaded", currentWebModel.Value.WordCount);
                }
                else
                {
                    ServiceEventSource.Current.ServiceMessage(this.Context, "WebSite2 Begin caching records");

                    for (int i = 0; i < ENTRIES_TO_CACHE; i++)
                    {
                        Thread.Sleep(1000);
                        await _webCache.AddAsync(tx, i.ToString(), new CacheRecord { Word1 = i.ToString(), Word2 = i.ToString() });

                        ServiceEventSource.Current.ServiceMessage(this.Context, "WebSite2 Adding cache entry Value: {0}", i);
                    }

                    var updateModel = new WebModel
                    {
                        CacheLoaded = true,
                        WordCount = ENTRIES_TO_CACHE
                    };

                    await _webModel.AddOrUpdateAsync(tx, "CurrentModel", updateModel, (key, value) => updateModel);

                    ServiceEventSource.Current.ServiceMessage(this.Context, "WebSite2 COMPLETED Caching Records");
                }

                // If an exception is thrown before calling CommitAsync, the transaction aborts, all changes are 
                // discarded, and nothing is saved to the secondary replicas.
                await tx.CommitAsync();
            }
        }
    }
}
