namespace Ae.Sample.Mcp.Settings
{
    public sealed class AppOptions
    {
        public const string App = "App"; // Configuration section name

        /// <summary>
        /// The name of the application. (e.g. "MCP Server")
        /// </summary>
        public string Name { get; set; } = "???";

        /// <summary>
        /// The version of the application. (e.g. "1.0.1")
        /// </summary>
        public string Version { get; set; } = "?.?";
    }
}
