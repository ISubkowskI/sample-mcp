namespace Ae.Sample.Mcp.Settings
{
    public sealed class ServerAuthenticationOptions
    {
        public const string Authentication = "Authentication"; // Configuration section name

        public string Scheme { get; set; } = "Bearer";

        public string ExpectedToken { get; set; } = "*";
    }
}