namespace IndependentApproval.Api.Contracts.System;

public sealed record SystemInfoResponse(
    string ApplicationName,
    string Version,
    string Environment,
    DateTimeOffset CurrentUtcTimestamp);
