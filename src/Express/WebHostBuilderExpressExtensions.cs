using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.Extensions.DependencyInjection;

namespace Express;

public static class WebHostBuilderExpressExtensions
{
    public static IWebHostBuilder UseExpress(this IWebHostBuilder hostBuilder)
    {
        return hostBuilder.ConfigureServices(services =>
        {
            services.AddSingleton<IExpressInterop, ExpressInterop>();
            services.AddSingleton<IServer, ExpressServer>();
        });
    }
}
