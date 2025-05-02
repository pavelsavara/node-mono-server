// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Express;

internal class ExpressApplicationWrapper<TContext> : IExpressApplicationWrapper
    where TContext : notnull
{
    private class ExpressHttpContext : IExpressHttpContext
    {
        internal TContext? Context;
        ResponseStreamWrapper _responseStream;

        private HttpRequestFeature req;
        private HttpResponseFeature res;
        private StreamResponseBodyFeature resBody;

        IHttpApplication<TContext> _application;
        private bool disposedValue;

        public ExpressHttpContext(ResponseStreamWrapper responseStream, IHttpApplication<TContext> application)
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
    private readonly IExpressInterop _expressInterop;

    public ExpressApplicationWrapper(IHttpApplication<TContext> application, IExpressInterop expressInterop)
    {
        _application = application;
        _expressInterop = expressInterop;
    }

    public async Task Handler(IDisposable expressContext, string method, string path, string[] headerNames, string[] headerValues, byte[]? body)
    {
        IExpressHttpContext? httpWrapper = null;
        ExpressResponseStream? responseStream = null;
        try
        {
            Console.WriteLine($"Express {expressContext} {method} {path} {headerNames.Length} {headerValues.Length} {body?.Length}");
            responseStream = new ExpressResponseStream(expressContext, _expressInterop);
            httpWrapper = CreateContext(responseStream);

            var mwTask = httpWrapper.ProcessRequest(method, path, headerNames, headerValues, body);

            await httpWrapper.ProcessResponse();

            if (mwTask.IsCompleted)
            {
                await mwTask;
                responseStream.Dispose();
            }

            Console.WriteLine($"Express Complete");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Express failed: {ex.Message}");
            Console.WriteLine(ex);

            var bytes = Encoding.UTF8.GetBytes(ex.ToString());

            _expressInterop.SendHeaders(expressContext, 500, [], []);
            _expressInterop.SendBuffer(expressContext, bytes, 0, bytes.Length);
            _expressInterop.SendEnd(expressContext);

            responseStream?.Dispose();
        }
    }

    private IExpressHttpContext CreateContext(ResponseStreamWrapper responseStream)
    {
        return new ExpressHttpContext(responseStream, _application);
    }

    private void DisposeContext(IExpressHttpContext contextWrapper, Exception? exception)
    {
        ExpressHttpContext wrapper = (ExpressHttpContext)contextWrapper;
        _application.DisposeContext(wrapper.Context!, exception);
    }

    private Task ProcessRequestAsync(IExpressHttpContext contextWrapper)
    {
        ExpressHttpContext wrapper = (ExpressHttpContext)contextWrapper;
        return _application.ProcessRequestAsync(wrapper.Context!);
    }
}
