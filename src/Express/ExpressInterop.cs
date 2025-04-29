// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.JavaScript;
using System.Text;
using System.Threading.Tasks;

namespace Express;

internal interface IContextWrapper
{
    IFeatureCollection ContextFeatures { get; }
}

partial class ExpressInterop
{
    private readonly IHttpApplication<IContextWrapper> _httpApplication;
    private static ExpressInterop? Instance;

    public static FeatureCollection FeaturesCollectionFactory()
    {
        var features = new FeatureCollection(10);
        features.Set<IHttpRequestFeature>(new HttpRequestFeature());
        features.Set<IHttpResponseFeature>(new HttpResponseFeature());
        features.Set<IHttpResponseBodyFeature>(new StreamResponseBodyFeature(new MemoryStream()));
        return features;
    }

    public ExpressInterop(IHttpApplication<IContextWrapper> httpApplication)
    {
        _httpApplication = httpApplication;
    }

    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicMethods)]
    public void StartServer(string[] addresses)
    {
        Instance = this;
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

        StartServerJs(httpPorts.ToArray(), httpsPorts.ToArray(), hosts.ToArray());
    }

    public void StopServer()
    {
        StopServerJs();
        Instance = null;
    }

    private async Task Handler(JSObject expressContext, string method, string path, string[] headerNames, string[] headerValues, byte[]? body)
    {
        IContextWrapper? httpWrapper = null;
        try
        {
            // Console.WriteLine($"Handler {expressContext} {method} {path} {headerNames.Length} {headerValues.Length} {body?.Length}");

            httpWrapper = _httpApplication!.CreateContext(FeaturesCollectionFactory());
            var requestFeature = httpWrapper.ContextFeatures.Get<IHttpRequestFeature>();
            if (requestFeature == null)
            {
                throw new InvalidOperationException("Request feature is not available.");
            }
            var responseFeature = httpWrapper.ContextFeatures.Get<IHttpResponseFeature>();
            if (responseFeature == null)
            {
                throw new InvalidOperationException("Response feature is not available.");
            }
            var responseBodyFeature = httpWrapper.ContextFeatures.Get<IHttpResponseBodyFeature>();

            requestFeature.Method = method;
            requestFeature.Path = path;
            for (int i = 0; i < headerNames.Length; i++)
            {
                requestFeature.Headers[headerNames[i]] = headerValues[i];
            }
            if (body != null)
            {
                requestFeature.Body = new MemoryStream(body);
            }
            await _httpApplication.ProcessRequestAsync(httpWrapper);

            byte[]? responseBody = null;
            if (responseBodyFeature != null && responseBodyFeature.Stream != null && responseBodyFeature.Stream.Length > 0)
            {
                responseBody = await ReadFully(responseBodyFeature.Stream);
                responseFeature.Headers.ContentLength = responseBody.Length;
            }

            var resHeaders = responseFeature.Headers;
            var responseHeaderNames = new string[resHeaders.Count];
            var responseHeaderValues = new string[resHeaders.Count];
            for (int i = 0; i < resHeaders.Count; i++)
            {
                var headerName = resHeaders.Keys.ElementAt(i);
                var headerValue = resHeaders[headerName].ToString();
                responseHeaderNames[i] = headerName;
                responseHeaderValues[i] = headerValue;
            }

            SendResponse(expressContext, responseFeature.StatusCode, responseHeaderNames, responseHeaderValues, responseBody);
            if (httpWrapper != null) _httpApplication?.DisposeContext(httpWrapper, null);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ExpressInterop failed: {ex.Message}");
            Console.WriteLine(ex);
            SendResponse(expressContext, 500, [], [], Encoding.UTF8.GetBytes(ex.Message));
            if (httpWrapper != null) _httpApplication?.DisposeContext(httpWrapper, ex);
        }
    }

    private static async Task<byte[]> ReadFully(Stream input)
    {
        input.Position = 0;
        /*if (input is MemoryStream)
        {
            return ((MemoryStream)input).ToArray();
        }*/

        using MemoryStream ms = new();
        await input.CopyToAsync(ms);
        return ms.ToArray();
    }

    #region JSInterop

    [JSExport]
    static Task RequestHandler(JSObject expressContext, string method, string path, string[] headerNames, string[] headerValues, byte[]? body)
    {
        if (Instance == null)
        {
            throw new NullReferenceException(nameof(Instance));
        }

        // NodeJS should be single threaded, right ?
        lock (Instance)
        {
            return Instance.Handler(expressContext, method, path, headerNames, headerValues, body);
        }
    }

    [JSImport("sendResponse", "middleware")]
    static partial void SendResponse(JSObject expressContext, int statusCode, string[] headerNames, string[] headerValues, byte[]? responseBody);

    [JSImport("startServer", "middleware")]
    static partial void StartServerJs(int[] httpPorts, int[] httpsPorts, string[] hosts);

    [JSImport("stopServer", "middleware")]
    static partial void StopServerJs();

    #endregion
}
