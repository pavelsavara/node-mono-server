// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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
    private static readonly IFeatureCollection _features;
    public IFeatureCollection Features { get; } = _features;

    static ExpressServer()
    {
        _features = new FeatureCollection(10);
        var addr = new ServerAddressesFeature();
        addr.Addresses.Add("http://localhost:3001");
        _features.Set<IServerAddressesFeature>(addr);
    }

    public Task StartAsync<TContext>(IHttpApplication<TContext> application, CancellationToken cancellationToken) where TContext : notnull
    {
        var addressesFeature = _features.Get<IServerAddressesFeature>();
        var addresses = addressesFeature != null ? addressesFeature.Addresses.ToArray() : [];

        _interop = new ExpressInterop(new HttpApplicationWrapper<TContext>(application));
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
}

internal class HttpApplicationWrapper<TContext> : IHttpApplicationWrapper
    where TContext : notnull
{
    private class HttpContextWrapper : IHttpContextWrapper
    {
        internal TContext? Context;
        ResponseStreamWrapper _responseStream;

        private HttpRequestFeature req;
        private HttpResponseFeature res;
        private StreamResponseBodyFeature resBody;

        IHttpApplication<TContext> _application;
        private bool disposedValue;

        public HttpContextWrapper(ResponseStreamWrapper responseStream, IHttpApplication<TContext> application)
        {
            _application = application;
            _responseStream = responseStream;

            var features = new FeatureCollection(10);
            req = new HttpRequestFeature(); 
            res = new HttpResponseFeature();
            resBody = new StreamResponseBodyFeature(responseStream);
            features.Set<IHttpRequestFeature>(req);
            features.Set<IHttpResponseFeature>(res);
            features.Set<IHttpResponseBodyFeature>(resBody);

            var ctx = _application.CreateContext(features);
            Context = ctx;

        }

        public Task ProcessRequest(string method, string path, string[] headerNames, string[] headerValues, byte[]? body)
        {
            req.Method = method;
            req.Path = path;
            for (int i = 0; i < headerNames.Length; i++)
            {
                req.Headers[headerNames[i]] = headerValues[i];
            }
            if (body != null)
            {
                req.Body = new MemoryStream(body);
            }
            return _application.ProcessRequestAsync(Context!);
        }

        public async Task ProcessResponse()
        {
            await resBody.StartAsync();
            var resHeaders = res.Headers;
            var responseHeaderNames = new string[resHeaders.Count];
            var responseHeaderValues = new string[resHeaders.Count];
            for (int i = 0; i < resHeaders.Count; i++)
            {
                var headerName = resHeaders.Keys.ElementAt(i);
                var headerValue = resHeaders[headerName].ToString();
                responseHeaderNames[i] = headerName;
                responseHeaderValues[i] = headerValue;
            }
            _responseStream.SendHeaders(res.StatusCode, responseHeaderNames, responseHeaderValues);
            await resBody.CompleteAsync();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    resBody.Dispose();
                    _responseStream.Dispose();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }

    private readonly IHttpApplication<TContext> _application;

    public HttpApplicationWrapper(IHttpApplication<TContext> application)
    {
        _application = application;
    }

    public IHttpContextWrapper CreateContext(ResponseStreamWrapper responseStream)
    {
        return new HttpContextWrapper(responseStream, _application);
    }

    public void DisposeContext(IHttpContextWrapper contextWrapper, Exception? exception)
    {
        HttpContextWrapper wrapper = (HttpContextWrapper)contextWrapper;
        _application.DisposeContext(wrapper.Context!, exception);
    }

    public Task ProcessRequestAsync(IHttpContextWrapper contextWrapper)
    {
        HttpContextWrapper wrapper = (HttpContextWrapper)contextWrapper;
        return _application.ProcessRequestAsync(wrapper.Context!);
    }
}

internal interface IHttpContextWrapper : IDisposable
{
    Task ProcessRequest(string method, string path, string[] headerNames, string[] headerValues, byte[]? body);
    Task ProcessResponse();
}

internal abstract class ResponseStreamWrapper : Stream
{
    public abstract void SendHeaders(int statusCode, string[] headerNames, string[] headerValues);
}

internal interface IHttpApplicationWrapper
{
    IHttpContextWrapper CreateContext(ResponseStreamWrapper expressContext);

    Task ProcessRequestAsync(IHttpContextWrapper context);

    void DisposeContext(IHttpContextWrapper context, Exception? exception);
}