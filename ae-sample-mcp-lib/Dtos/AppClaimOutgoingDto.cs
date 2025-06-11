﻿namespace Ae.Sample.Mcp.Dtos
{
    public sealed record AppClaimOutgoingDto
    {
        public Guid Id { get; init; } = Guid.Empty;
        public string Type { get; init; } = string.Empty;
        public string Value { get; init; } = string.Empty;
        public string ValueType { get; init; } = string.Empty;
        public string DisplayText { get; init; } = string.Empty;
        public IDictionary<string, string>? Properties { get; init; }
        public string? Description { get; init; }
    }
}
