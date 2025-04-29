import express from "express";
import http from "http";
import https from "https";

export function startServer(httpPorts, httpsPorts, hosts, expressHandler) {
    const app = express();

    app.all(/.*/, async (req, res) => {
        await expressHandler(req, res);
    });

    // Start the server
    if (httpPorts && httpPorts.length > 0) {
        const httpServer = http.createServer(app);
        for (const port of httpPorts) {
            for (const host of hosts) {
                console.log(`HTTP server listening on ${host}:${port}`);
                httpServer.listen(port, host);
            }
        }
    }
    if (httpsPorts && httpsPorts.length > 0) {
        const httpServer = https.createServer(app);
        for (const port of httpsPorts) {
            for (const host of hosts) {
                console.log(`HTTP server listening on ${host}:${port}`);
                httpServer.listen(port, host);
            }
        }
    }

    // Handle server shutdown
    process.on('SIGINT', async () => {
        process.exit(0);
    });
}

export function stopServer() {
    process.exit(0);
}