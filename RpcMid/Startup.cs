using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Prometheus;

namespace RpcMid {

    public partial class Startup {

        public void Configure(IApplicationBuilder appBuilder, IServiceProvider serviceProvider) {
            serviceProvider.GetService<RpcExecutor>().Init();
            
            appBuilder
                .UseMetricServer()
                .UseHttpMetrics()
                .UseWsTransport()
                .UseHttpTransport();
        }

        public void ConfigureServices(IServiceCollection services) {
            services.AddSingleton(typeof(IRpcService), typeof(FirstService));
            services.AddSingleton(typeof(IRpcService), typeof(SecondService));
            services.AddSingleton(typeof(RpcExecutor));
        }

    }

}