﻿namespace Ae.Sample.Mcp.Dtos;

public sealed record ErrorOutgoingDto
{
    public IEnumerable<string?>? Errors { get; init; } = [];
    public string? Status { get; init; } = string.Empty;
}
