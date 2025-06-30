# sample-mcp

This repository contains sample projects demonstrating the Model Context Protocol (MCP).

## Transport STDIO
- ae-sample-mcp-srvstdio (Startup project)
- ae-sample-mcp-lib
- ae-sample-mcp-unittests

This project is a sample MCP server that communicates over standard input/output (stdio).

### MCP Inspector

It is designed to work with the Model Context Protocol (MCP) Inspector, which lets you inspect and debug the communication between your application and the MCP server.

To start the inspector, navigate to the application folder and run the following command using Node.js:
```bash
npx @modelcontextprotocol/inspector dotnet run
```

## Transport StreamableHttp. Server-Sent Events (SSE)
- ae-sample-mcp-srvsse (Startup project)
- ae-sample-mcp-lib
- ae-sample-mcp-unittests

This project is a sample MCP server that communicates over HTTP using Server-Sent Events (SSE). It exposes tools for managing identity claims and requires token-based authentication.


New SSE connection request. NOTE: The sse transport is deprecated and has been replaced by StreamableHttp
Query parameters: {"url":"http://localhost:3001/sse","transportType":"sse"}
SSE transport: url=http://localhost:3001/sse, headers={"Accept":"text/event-stream"}

New StreamableHttp connection request
Query parameters: {"url":"http://localhost:3001/sse","transportType":"streamable-http"}

