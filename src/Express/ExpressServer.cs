// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http.Features;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Express;

internal sealed class ExpressServer : IServer
{
    private IExpressInterop _expressInterop;
    private bool _disposed = false;
    private static readonly IFeatureCollection _features;
    public IFeatureCollection Features { get; } = _features;

    static ExpressServer()
    {
        _features = new FeatureCollection(10);
        var addr = new ServerAddressesFeature();
        addr.Addresses.Add("http://localhost:3001");
        _features.Set<IServerAddressesFeature>(addr);
    }

    public ExpressServer(IExpressInterop expressInterop)
    {
        _expressInterop = expressInterop;
    }

    public Task StartAsync<TContext>(IHttpApplication<TContext> application, CancellationToken cancellationToken) where TContext : notnull
    {
        IExpressApplicationWrapper appWrapper = new ExpressApplicationWrapper<TContext>(application, _expressInterop);

        StartServer(appWrapper);
        return Task.CompletedTask;
    }

    private void StartServer(IExpressApplicationWrapper appWrapper)
    {
        var addressesFeature = _features.Get<IServerAddressesFeature>();
        var addresses = addressesFeature != null ? addressesFeature.Addresses.ToArray() : [];
        List<int> httpPorts = new();
        List<int> httpsPorts = new();
        List<string> hosts = new();
        foreach (var address in addresses)
        {
            var uri = new Uri(address);
            if (uri.Scheme == "http")
            {
                httpPorts.Add(uri.Port);
                hosts.Add(uri.Host);
            }
            else if (uri.Scheme == "https")
            {
                httpsPorts.Add(uri.Port);
            }
            else
            {
                throw new NotSupportedException($"Unsupported scheme: {uri.Scheme}");
            }
        }
        _expressInterop.StartServer(appWrapper, httpPorts.ToArray(), httpsPorts.ToArray(), hosts.ToArray());
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _expressInterop?.StopServer();
        _expressInterop = null!;
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
        }
    }
}
