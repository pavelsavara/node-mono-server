// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;

namespace Express;

class ExpressResponseStream : ResponseStreamWrapper
{
    private MemoryStream? _memoryStream;
    private int _position;
    private IDisposable _expressJsContext;
    IExpressInterop _expressInterop;

    public ExpressResponseStream(IDisposable expressJsContext, IExpressInterop expressInterop)
    {
        _expressJsContext = expressJsContext;
        _expressInterop = expressInterop;
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
        _expressInterop.SendHeaders(_expressJsContext, statusCode, headerNames, headerValues);

        if (_memoryStream?.Length > 0)
        {
            _memoryStream.Position = 0;
            _expressInterop.SendBuffer(_expressJsContext, _memoryStream.ToArray(), 0, (int)_memoryStream.Length);
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
            _expressInterop.SendBuffer(_expressJsContext, buffer, offset, count);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _expressInterop.SendEnd(_expressJsContext);
            _expressJsContext?.Dispose();
        }
        base.Dispose(disposing);
    }
}
