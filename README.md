# sample-mcp
Sample MCP-server  (Model Context Protocol)

## ae-sample-mcp-srvstdio

This is a sample application that implements the Model Context Protocol (MCP) server for identity management. Transport is done using standard input and output (stdio). It is designed to be used with the Model Context Protocol (MCP) Inspector for debugging and inspection purposes.

### MCP Inspector

It is designed to work with the Model Context Protocol (MCP) Inspector, which lets you inspect and debug the communication between your application and the MCP server.

To start the inspector, navigate to the application folder and run the following command using Node.js:
```bash
npx @modelcontextprotocol/inspector dotnet run
```
## ae-sample-mcp-srvsse

This is a sample application that implements the Model Context Protocol (MCP) server for identity management. Transport is done using Server-Sent Events (SSE). It is designed to be used with the Model Context Protocol (MCP) Inspector for debugging and inspection purposes.


New SSE connection request. NOTE: The sse transport is deprecated and has been replaced by StreamableHttp
Query parameters: {"url":"http://localhost:3001/sse","transportType":"sse"}
SSE transport: url=http://localhost:3001/sse, headers={"Accept":"text/event-stream"}
Connection refused. Is the MCP server running?

New StreamableHttp connection request
Query parameters: {"url":"http://localhost:3001/sse","transportType":"streamable-http"}
Created StreamableHttp server transport
Created StreamableHttp client transport
Client <-> Proxy  sessionId: b3846af1-936c-47b0-b8e5-4f85d90b2a06
Connection refused. Is the MCP server running?
