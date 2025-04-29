// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Express.HelloServer;

public class Program
{
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
    }

    public virtual void Configure(IApplicationBuilder app)
    {
        app.UseRouting();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapGet("/health", () => Results.Ok("Server is running"));
            endpoints.MapGet("/", () => "Hello World from ExpressServer!");
            endpoints.MapDefaultControllerRoute();
        });

    }

    protected virtual void ConfigureMvcOptions(MvcOptions options)
    {
    }
}