## .NET WebAssembly Node server

```
npm install
dotnet workload restore
dotnet run --project ./src/Express/
```

## Details

This is demonstrating how to run ASP.NET Core middleware pipeline on top of NodeJS/Express HTTP server with zore native files. 
The motivation is to make it easier to run C# MCP server via `nxp` without dotnet native installation on the target architecture.

The dotnet VM is Mono interpreter compiled into WASM in for the Browser target.
The integration with Express HTTP server is done via `[JSImport]/[JSExport]` interop.
At the moment the implementation is naive HTTP/1.1 without streaming.

## Todo
- demonstrate actual MCP server
- provide steps which publish this as NPM package