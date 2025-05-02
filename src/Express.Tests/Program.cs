// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace Express.Tests;

public class Program
{
    public static async Task Main(string[] args)
    {
        try
        {
            var host = new WebHostBuilder()
                .UseStartup<Startup>()
                .Build();
            var cts = new CancellationTokenSource();
            await host.RunAsync(cts.Token);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error starting server: {ex.Message}");
            Console.WriteLine(ex);
            Environment.Exit(1);
        }
    }
}

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IExpressInterop, FakeExpressInterop>();
        services.AddSingleton<IServer, ExpressServer>();
        services.AddRouting();
        services.AddControllers();
        services
            .AddMcpServer()
            .WithHttpTransport(transportOptions =>
            {
                transportOptions.ConfigureSessionOptions = (HttpContext ctx, McpServerOptions options, CancellationToken token) =>
                {
                    Console.WriteLine("Configuring session options...");
                    return Task.CompletedTask;
                };
            })
            .WithToolsFromAssembly();
    }

    public virtual void Configure(IApplicationBuilder app)
    {
        app.UseRouting();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapMcp();
        });
    }
}

[McpServerToolType]
public static class EchoTool
{
    [McpServerTool, Description("Echoes the message back to the client.")]
    public static string Echo(string message) => $"hello {message}";
}