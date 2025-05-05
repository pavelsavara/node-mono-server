## .NET WebAssembly Node server

The motivation is to make it easier to run C# MCP SSE server via `nxp` without dotnet native installation on the target architecture.
Perhaps there are other use-cases.


```sh
npm install
dotnet workload restore
dotnet run --project ./src/Express.HelloServer
curl -v http://localhost:3001/health
```

```sh
dotnet run --project ./src/Express.McpEcho
npx @modelcontextprotocol/inspector
```

## Implementation details and underlying support

This is demonstrating how to run ASP.NET Core middleware pipeline on top of NodeJS/Express HTTP server with zero native files. 

The dotnet VM is Mono interpreter compiled into WASM in for the Browser target.
The integration with Express HTTP server is done via `[JSImport]/[JSExport]` interop.
At the moment the implementation is naive HTTP/1.1 only with response streaming, no WS, no HTTP/2.
The other MCP transport types are not implemented yet.

The dotnet runtime supports Browser "operating system" and it also works on NodeJS. 
But NodeJS it's not supported target in Net10 or older.
The aspnetcore server side doesn't support browser "operating system" at all. 
This demo is hacking it's MSBuild scripts in unsupported and undocumented way in order to fabricate `Microsoft.NET.Sdk.Web` for `browser-wasm` RID.
Both limitations could be fixed.

## Alternative

The same demo could be implemented on top of WASI "operating system" target of dotnet runtime.
The `wasi:http` preview 2 handler also works in similar way.

Downsides compared to Browser target are
- WASI is not yet a supported product/target of dotnet
- WASI HTTP server host is creating new component instance (memory) for each request. 
That's not compatible with running long lived MCP session in-process. 
This could be fixed by implementing session persistence via another WASI component, such as `wasi:keyvalue`

Benefits
- WASI granular security model could isolate the component from touching unexpected resources on the local machine. Such as local disk, environment variables, network, HTTP endpoints etc. Making it easier to trust 3rd party MCP server implemented in WASI than to trust running NPM module on your machine.


## Publish NPM package

```sh
dotnet publish ./src/Express.McpEcho -c Release
cd C:/Dev/node-mono-server/src/Express.McpEcho/bin/Release/net9.0/publish/wwwroot
npm login
npm publish --access public
```
