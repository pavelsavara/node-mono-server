// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;
using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace Express.McpEcho;

public class Program
{
    // DynamicDependency is used to ensure that the EchoTool class is not removed by the linker.
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicMethods, typeof(EchoTool))]
    public static async Task Main(string[] args)
    {
        try
        {
            var host = new WebHostBuilder()
                .UseExpress()
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
        services.AddRouting();
        services.AddControllers();
        services
            .AddMcpServer()
            .WithHttpTransport()
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