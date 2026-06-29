using System;

namespace PostageCalculator;

public sealed record PostageRequest
{
    public string City { get; init; } = string.Empty;
    public string Country { get; init; } = string.Empty;
    public float Weight { get; init; }
}

public sealed record PostageResult
{
    public float Cost { get; init; }
}
