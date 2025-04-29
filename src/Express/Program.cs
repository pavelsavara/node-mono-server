using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;

namespace Express;

public class Program
{
    public static async Task Main(string[] args) 
    {
        var builder = WebApplication.CreateBuilder(args);
        
        builder.Services.AddSingleton<IServer, ExpressServer>();

        var app = builder.Build();
        
        app.MapGet("/health", () => Results.Ok("Server is running"));
        app.MapGet("/", () => "Hello World from ExpressServer!");

        await app.RunAsync();
    }
}
