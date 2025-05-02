// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

export async function expressHandler(req, res, managedRequestHandler) {
    try {

        const headerNames = [];
        const headerValues = [];
        for (const name of Object.keys(req.headers)) {
            headerNames.push(name);
            headerValues.push(req.headers[name]);
        }

        const body = req.body ? Uint8Array.from(req.body) : new Uint8Array(0);

        console.log("Express handler: ", JSON.stringify({ method: req.method, path: req.path, headerNames, headerValues, body }));

        const httpContext = {
            req,
            res,
        };

        await managedRequestHandler(httpContext, req.method, req.path, headerNames, headerValues, body);
    }
    catch (error) {
        console.log("Express handler failed: " + error);
    }
}

export function sendHeaders(httpContext, statusCode, headerNames, headerValues) {
    console.log("Express sendResponseHeaders: ", statusCode);
    
    const headers = new Map();
    const res = httpContext.res;
    for (let i = 0; i < headerNames.length; i++) {
        const field = headerNames[i];
        const value = headerValues[i];
        headers.set(field, value);
    }
    res.setHeaders(headers);
    res.status(statusCode);

    res.flushHeaders();
}

export function sendBuffer(httpContext, responseBuffer, offset, count) {
    console.log("Express sendBuffer: ", { responseBuffer, offset, count });
    const res = httpContext.res;
    if (responseBuffer) {
        const buffer = Buffer.from(responseBuffer, offset, count);
        res.write(buffer);
    }
}

export function sendEnd(httpContext) {
    console.log("Express sendEnd: ");
    const res = httpContext.res;
    res.end();
}
