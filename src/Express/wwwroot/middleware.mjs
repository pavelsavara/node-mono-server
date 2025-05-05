// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

export async function expressHandler(req, res, managedRequestHandler) {
    try {

        const absoluteUrl = `${req.protocol}://${req.host}${req.originalUrl}`;
        const headerNames = [];
        const headerValues = [];
        for (const name of Object.keys(req.headers)) {
            headerNames.push(name);
            headerValues.push(req.headers[name]);
        }

        const chunks = [];
        // TODO request streaming to C# stream
        await new Promise((resolve, reject) => {
            req.on('data', (chunk) => {
                chunks.push(chunk);
            });
            req.on('end', resolve);
            req.on('error', reject);
        });

        const body = Uint8Array.from(Buffer.concat(chunks));

        // console.log("Express handler: ", JSON.stringify({ method: req.method, absoluteUrl, body:body.length }));

        const httpContext = {
            req,
            res,
        };

        await managedRequestHandler(httpContext, req.method, absoluteUrl, headerNames, headerValues, body);
    }
    catch (error) {
        console.log("Express handler failed: " + error);
    }
}

export function sendHeaders(httpContext, statusCode, headerNames, headerValues) {
    // console.log("Express sendResponseHeaders: ", statusCode);
    
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
    //console.log("Express sendBuffer: ", { responseBuffer, offset, count });
    const res = httpContext.res;
    if (responseBuffer) {
        const buffer = Buffer.from(responseBuffer, offset, count);
        res.write(buffer);
    }
}

export function sendEnd(httpContext) {
    //console.log("Express sendEnd: ");
    const res = httpContext.res;
    res.end();
}
