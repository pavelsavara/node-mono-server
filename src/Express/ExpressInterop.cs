// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;

namespace Express;

[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicMethods)]
partial class ExpressInterop : IExpressInterop
{
    private static IExpressApplicationWrapper? _httpApplicationInstance;

    public void StartServer(IExpressApplicationWrapper httpApplication, int[] httpPorts, int[] httpsPorts, string[] hosts)
    {
        _httpApplicationInstance = httpApplication;
        StartServerJs(httpPorts, httpsPorts, hosts);
    }

    public void StopServer()
    {
        StopServerJs();
        _httpApplicationInstance = null;
    }

    public void SendHeaders(IDisposable expressContext, int statusCode, string[] headerNames, string[] headerValues)
    {
        SendHeadersJs((JSObject)expressContext, statusCode, headerNames, headerValues);
    }

    public void SendBuffer(IDisposable expressContext, byte[] buffer, int offset, int count)
    {
        SendBufferJs((JSObject)expressContext, buffer, offset, count);
    }

    public void SendEnd(IDisposable expressContext)
    {
        SendEndJs((JSObject)expressContext);
    }


    #region JSInterop

    [JSExport]
    static Task RequestHandler(JSObject expressContext, string method, string url, string[] headerNames, string[] headerValues, byte[]? body)
    {
        if (_httpApplicationInstance == null)
        {
            throw new NullReferenceException(nameof(_httpApplicationInstance));
        }

        // NodeJS should be single threaded, right ?
        lock (_httpApplicationInstance!)
        {
            return _httpApplicationInstance.Handler(expressContext, method, url, headerNames, headerValues, body);
        }
    }

    [JSImport("sendHeaders", "middleware")]
    static partial void SendHeadersJs(JSObject expressContext, int statusCode, string[] headerNames, string[] headerValues);

    [JSImport("sendBuffer", "middleware")]
    static partial void SendBufferJs(JSObject expressContext, byte[] buffer, int offset, int count);

    [JSImport("sendEnd", "middleware")]
    static partial void SendEndJs(JSObject expressContext);

    [JSImport("startServer", "middleware")]
    static partial void StartServerJs(int[] httpPorts, int[] httpsPorts, string[] hosts);

    [JSImport("stopServer", "middleware")]
    static partial void StopServerJs();

    #endregion
}
