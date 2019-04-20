using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Prometheus.DotNetRuntime;

namespace RpcMid {

    internal static class Program {

        private static async Task Main(string[] args) {
            DotNetRuntimeStatsBuilder.Default().StartCollecting();
            await CreateWebHostBuilder(args).Build().RunAsync();
        }

        private static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            new WebHostBuilder()
                .UseLibuv(options => options.ThreadCount=8)
                .UseKestrel()
                .ConfigureKestrel((context, options) => {
                    options.ConfigureEndpointDefaults(opts => opts.NoDelay = true);
                })
                .UseStartup<Startup>()
                .UseUrls("http://*:8085/");

    }

}