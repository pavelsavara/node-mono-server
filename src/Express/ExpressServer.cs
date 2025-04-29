// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using System;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;

namespace Express;

internal interface IExpressHttpContextFactory
{
    object Create(IFeatureCollection featureCollection);
    void Dispose(object httpContext);
}

internal sealed class ExpressServer : IServer
{
    private ExpressInterop? _interop;
    private bool _disposed = false;
    private static readonly IFeatureCollection _features = ExpressInterop.FeaturesCollectionFactory();

    public ExpressServer()
    {
        var addr = new ServerAddressesFeature();
        addr.Addresses.Add("http://localhost:3000");
        _features.Set<IServerAddressesFeature>(addr);
    }


    public IFeatureCollection Features { get; } = _features;

    public Task StartAsync<TContext>(IHttpApplication<TContext> application, CancellationToken cancellationToken) where TContext : notnull
    {
        var addressesFeature = _features.Get<IServerAddressesFeature>();
        var addresses = addressesFeature != null ? addressesFeature.Addresses.ToArray() : [];

        _interop = new ExpressInterop(new ApplicationAdapter<TContext>(application));
        _interop.StartServer(addresses);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _interop?.StopServer();
        _interop = null;
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
        }
    }

    private class ApplicationAdapter<TContext> : IHttpApplication<IContextWrapper>
        where TContext : notnull
    {
        private class ContextWrapper : IContextWrapper
        {
            public required IFeatureCollection ContextFeatures { get; set; }
            public required TContext Context;
        }

        private readonly IHttpApplication<TContext> _application;

        public ApplicationAdapter(IHttpApplication<TContext> application)
        {
            _application = application;
        }

        public IContextWrapper CreateContext(IFeatureCollection contextFeatures)
        {
            return new ContextWrapper
            {
                Context = _application.CreateContext(contextFeatures),
                ContextFeatures = contextFeatures
            };
        }

        public void DisposeContext(IContextWrapper contextWrapper, Exception? exception)
        {
            ContextWrapper wrapper = (ContextWrapper)contextWrapper;
            _application.DisposeContext(wrapper.Context, exception);
        }

        public Task ProcessRequestAsync(IContextWrapper contextWrapper)
        {
            ContextWrapper wrapper = (ContextWrapper)contextWrapper;
            return _application.ProcessRequestAsync(wrapper.Context);
        }
    }
}
