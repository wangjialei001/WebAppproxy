using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebAppProxy.Config;
using Yarp.ReverseProxy.Abstractions;
using Yarp.ReverseProxy.Service;

namespace WebAppProxy.Services
{
    public class LoadConfigService: ILoadConfigService
    {
        private readonly IServiceProvider _serviceProvider;
        public LoadConfigService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }
        public async Task GetChange()
        {
            var inMemoryConfig = (ConfigProvider)_serviceProvider.GetRequiredService<IProxyConfigProvider>();
            inMemoryConfig.Update(
                new List<ProxyRoute>() { new ProxyRoute { ClusterId = "r1", Match = new RouteMatch { Path = "/baidu" } } },
                new List<Cluster>
                {
                    new Cluster{ Id="r1",Destinations=new Dictionary<string,Destination>(StringComparer.OrdinalIgnoreCase){ { "destination1", new Destination { Address = "http://baidu.com" } } } }
                });
        }
    }
    public interface ILoadConfigService
    {
        Task GetChange();
    }
}
