namespace IndependentApproval.Api.Contracts.Administration;

public sealed record AdministrationSummaryResponse(
    int ActiveUserCount,
    int ActiveRoleCount,
    int PublishedWorkflowCount,
    int ActiveRequestTypeCount);
