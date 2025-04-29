// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { dotnet } from './_framework/dotnet.js'
import { startServer, stopServer } from './server.mjs'
import { sendResponse, expressHandler } from './middleware.mjs'

const { setModuleImports, getAssemblyExports, runMainAndExit } = await dotnet
    .withDiagnosticTracing(false)
    .create();

const exports = await getAssemblyExports("Express");
const managedRequestHandler = exports.Express.ExpressInterop.RequestHandler;

const handler = (req, res) => expressHandler(req, res, managedRequestHandler);
setModuleImports('middleware', {
    startServer: (httpPorts, httpsPorts, hosts) => startServer(httpPorts, httpsPorts, hosts, handler),
    stopServer,
    sendResponse,
});

await runMainAndExit();