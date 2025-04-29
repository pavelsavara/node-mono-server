export async function expressHandler(req, res, managedRequestHandler) {
    try {
        const headerNames = [];
        const headerValues = [];
        for (const name of Object.keys(req.headers)) {
            headerNames.push(name);
            headerValues.push(req.headers[name]);
        }

        const body = req.body ? Uint8Array.from(req.body) : new Uint8Array(0);

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

export function sendResponse(httpContext, statusCode, headerNames, headerValues, responseBody) {
    const res = httpContext.res;
    for (let i = 0; i < headerNames.length; i++) {
        const field = headerNames[i];
        const value = headerValues[i];
        res.header(field, value);
    }
    res.status(statusCode);
    if (responseBody) {
        res.send(Buffer.from(responseBody));
    } else {
        res.send();
    }
    res.end();
}