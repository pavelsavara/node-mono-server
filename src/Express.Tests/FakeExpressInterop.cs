// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Express.Tests
{
    internal class FakeExpressInterop : IExpressInterop
    {
        public IExpressApplicationWrapper? HttpApplication;

        public void StartServer(IExpressApplicationWrapper httpApplication, int[] httpPorts, int[] httpsPorts, string[] hosts)
        {
            HttpApplication = httpApplication;
        }

        public void SendBuffer(IDisposable expressContext, byte[] buffer, int offset, int count)
        {
        }

        public void SendEnd(IDisposable expressContext)
        {
        }

        public void SendHeaders(IDisposable expressContext, int statusCode, string[] headerNames, string[] headerValues)
        {
        }

        public void StopServer()
        {
        }
    }
}
