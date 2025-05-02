// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Threading.Tasks;

namespace Express;

interface IExpressApplicationWrapper
{
    Task Handler(IDisposable expressContext, string method, string path, string[] headerNames, string[] headerValues, byte[]? body);
}

internal interface IExpressInterop
{
    void StartServer(IExpressApplicationWrapper httpApplication, int[] httpPorts, int[] httpsPorts, string[] hosts);
    void SendHeaders(IDisposable expressContext, int statusCode, string[] headerNames, string[] headerValues);
    void SendBuffer(IDisposable expressContext, byte[] buffer, int offset, int count);
    void SendEnd(IDisposable expressContext);
    void StopServer();
}

internal interface IExpressHttpContext : IDisposable
{
    Task ProcessRequest(string method, string path, string[] headerNames, string[] headerValues, byte[]? body);
    Task ProcessResponse();
}

abstract class ResponseStreamWrapper : Stream
{
    public abstract void SendHeaders(int statusCode, string[] headerNames, string[] headerValues);
}