using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Yarp.ReverseProxy.Abstractions;
using Yarp.ReverseProxy.RuntimeModel;
using Yarp.ReverseProxy.Service;

namespace WebAppProxy.Config
{
    /// <summary>
    /// Extends the IReverseProxyBuilder to support the InMemoryConfigProvider
    /// </summary>
    public static class ConfigProviderExtensions
    {
        public static IReverseProxyBuilder Load(this IReverseProxyBuilder builder,IReadOnlyList<ProxyRoute> routes,IReadOnlyList<Cluster> clusters)
        {
            builder.Services.AddSingleton<IProxyConfigProvider>(new ConfigProvider(routes, clusters));
            return builder;
        }
    }
    public class ConfigProvider : IProxyConfigProvider
    {
        private volatile ConfigData _config;
        public ConfigProvider(IReadOnlyList<ProxyRoute> routes, IReadOnlyList<Cluster> clusters)
        {
            _config = new ConfigData(routes, clusters);
        }
        /// <summary>
        /// Implementation of the IProxyConfigProvider.GetConfig method to supply the current snapshot of configuration
        /// </summary>
        /// <returns></returns>
        public IProxyConfig GetConfig()
        {
            return _config;
        }
        /// <summary>
        /// Swaps the config state with a new snapshot of the configuration, then signals the change
        /// </summary>
        /// <param name="routes"></param>
        /// <param name="clusters"></param>
        public void Update(IReadOnlyList<ProxyRoute> routes, IReadOnlyList<Cluster> clusters)
        {
            var oldConfig = _config;
            _config = new ConfigData(routes, clusters);
            oldConfig.SignalChange();
        }
        /// <summary>
        /// Implementation of IProxyConfig which is a snapshot of the current config state. The data for this class should be immutable.
        /// </summary>
        private class ConfigData : IProxyConfig
        {
            private readonly CancellationTokenSource _cts = new CancellationTokenSource();
            public ConfigData(IReadOnlyList<ProxyRoute> routes, IReadOnlyList<Cluster> clusters)
            {
                Routes = routes;
                Clusters = clusters;
                ChangeToken = new CancellationChangeToken(_cts.Token);
            }
            /// <summary>
            /// A snapshot of the list of routes for the proxy
            /// </summary>
            public IReadOnlyList<ProxyRoute> Routes { get; }
            /// <summary>
            /// A snapshot of the list of Clusters which are collections of interchangable destination endpoints
            /// </summary>
            public IReadOnlyList<Cluster> Clusters { get; }
            /// <summary>
            /// Fired to indicate the the proxy state has changed, and that this snapshot is now stale
            /// </summary>
            public IChangeToken ChangeToken { get; }
            internal void SignalChange()
            {
                _cts.Cancel();
            }
        }
    }
}