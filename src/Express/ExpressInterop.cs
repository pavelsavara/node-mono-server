// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices.JavaScript;
using System.Text;
using System.Threading.Tasks;

namespace Express;

partial class ExpressInterop
{
    private readonly IHttpApplicationWrapper _httpApplication;
    private static ExpressInterop? Instance;

    public ExpressInterop(IHttpApplicationWrapper httpApplication)
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
        IHttpContextWrapper? httpWrapper = null;
        ExpressResponseStream? responseStream = null;
        try
        {
            Console.WriteLine($"Express {expressContext} {method} {path} {headerNames.Length} {headerValues.Length} {body?.Length}");
            responseStream = new ExpressResponseStream(expressContext);
            httpWrapper = _httpApplication!.CreateContext(responseStream);

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

            SendHeadersJs(expressContext, 500, [], []);
            SendBuffer(expressContext, bytes, 0, bytes.Length);
            SendEnd(expressContext);

            responseStream?.Dispose();
        }
    }

    class ExpressResponseStream : ResponseStreamWrapper
    {
        private MemoryStream? _memoryStream;
        private int _position;
        private JSObject _expressContext;

        public ExpressResponseStream(JSObject expressContext)
        {
            _expressContext = expressContext;
            _memoryStream = new MemoryStream();
        }

        public override bool CanRead => false;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override long Length => _position;

        public override long Position
        {
            get => _position;
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
            // no need to flush, we are sending the data immediately
        }

        public override int Read(byte[] buffer, int offset, int count) => throw new NotImplementedException();

        public override long Seek(long offset, SeekOrigin origin) => throw new NotImplementedException();

        public override void SetLength(long value) => throw new NotImplementedException();

        public override void SendHeaders(int statusCode, string[] headerNames, string[] headerValues)
        {
            SendHeadersJs(_expressContext, statusCode, headerNames, headerValues);

            if (_memoryStream?.Length > 0)
            {
                _memoryStream.Position = 0;
                SendBuffer(_expressContext, _memoryStream.ToArray(), 0, (int)_memoryStream.Length);
                _memoryStream.Dispose();
                _memoryStream = null;
            }
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _position += count;
            // write into memory stream until headers are sent
            if (_memoryStream != null)
            {
                _memoryStream.Write(buffer, offset, count);
            }
            else
            {
                SendBuffer(_expressContext, buffer, offset, count);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                SendEnd(_expressContext);
                _expressContext.Dispose();
            }
            base.Dispose(disposing);
        }
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

    [JSImport("sendHeaders", "middleware")]
    static partial void SendHeadersJs(JSObject expressContext, int statusCode, string[] headerNames, string[] headerValues);

    [JSImport("sendBuffer", "middleware")]
    static partial void SendBuffer(JSObject expressContext, byte[] buffer, int offset, int count);

    [JSImport("sendEnd", "middleware")]
    static partial void SendEnd(JSObject expressContext);

    [JSImport("startServer", "middleware")]
    static partial void StartServerJs(int[] httpPorts, int[] httpsPorts, string[] hosts);

    [JSImport("stopServer", "middleware")]
    static partial void StopServerJs();

    #endregion
}
