using IndependentApproval.Api.Contracts.Administration.Workflows;
using IndependentApproval.Api.Contracts.Common;

namespace IndependentApproval.Api.Application.Administration;

public interface IWorkflowAdministrationService
{
    Task<PagedResponse<WorkflowListItemResponse>> ListAsync(
        string? search,
        bool includeArchived,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<WorkflowDetailResponse> GetAsync(
        Guid workflowId,
        CancellationToken cancellationToken);

    Task<WorkflowVersionResponse> GetVersionAsync(
        Guid workflowId,
        Guid versionId,
        CancellationToken cancellationToken);

    Task<WorkflowOptionsResponse> GetOptionsAsync(CancellationToken cancellationToken);

    Task<WorkflowDetailResponse> CreateAsync(
        CreateWorkflowRequest request,
        CancellationToken cancellationToken);

    Task<WorkflowVersionResponse> UpdateVersionAsync(
        Guid workflowId,
        Guid versionId,
        UpdateWorkflowVersionRequest request,
        CancellationToken cancellationToken);

    Task<WorkflowVersionResponse> ReplaceStarterRolesAsync(
        Guid workflowId,
        Guid versionId,
        ReplaceWorkflowStarterRolesRequest request,
        CancellationToken cancellationToken);

    Task<WorkflowVersionResponse> AddStepAsync(
        Guid workflowId,
        Guid versionId,
        CreateWorkflowStepRequest request,
        CancellationToken cancellationToken);

    Task<WorkflowVersionResponse> UpdateStepAsync(
        Guid workflowId,
        Guid versionId,
        Guid stepId,
        UpdateWorkflowStepRequest request,
        CancellationToken cancellationToken);

    Task<WorkflowVersionResponse> DeleteStepAsync(
        Guid workflowId,
        Guid versionId,
        Guid stepId,
        DeleteWorkflowStepRequest request,
        CancellationToken cancellationToken);

    Task<WorkflowVersionResponse> ReplaceStepRolesAsync(
        Guid workflowId,
        Guid versionId,
        Guid stepId,
        ReplaceWorkflowStepRolesRequest request,
        CancellationToken cancellationToken);

    Task<WorkflowVersionResponse> ReplaceStepFieldPermissionsAsync(
        Guid workflowId,
        Guid versionId,
        Guid stepId,
        ReplaceWorkflowStepFieldPermissionsRequest request,
        CancellationToken cancellationToken);

    Task<WorkflowVersionResponse> AddTransitionAsync(
        Guid workflowId,
        Guid versionId,
        CreateWorkflowTransitionRequest request,
        CancellationToken cancellationToken);

    Task<WorkflowVersionResponse> UpdateTransitionAsync(
        Guid workflowId,
        Guid versionId,
        Guid transitionId,
        UpdateWorkflowTransitionRequest request,
        CancellationToken cancellationToken);

    Task<WorkflowVersionResponse> DeleteTransitionAsync(
        Guid workflowId,
        Guid versionId,
        Guid transitionId,
        DeleteWorkflowTransitionRequest request,
        CancellationToken cancellationToken);

    Task<WorkflowValidationResponse> ValidateAsync(
        Guid workflowId,
        Guid versionId,
        ValidateWorkflowRequest request,
        CancellationToken cancellationToken);

    Task<WorkflowVersionResponse> PublishAsync(
        Guid workflowId,
        Guid versionId,
        WorkflowConcurrencyRequest request,
        CancellationToken cancellationToken);

    Task<WorkflowVersionResponse> CloneAsync(
        Guid workflowId,
        Guid versionId,
        WorkflowConcurrencyRequest request,
        CancellationToken cancellationToken);

    Task<WorkflowVersionResponse> ArchiveVersionAsync(
        Guid workflowId,
        Guid versionId,
        WorkflowConcurrencyRequest request,
        CancellationToken cancellationToken);

    Task<WorkflowDetailResponse> ArchiveAsync(
        Guid workflowId,
        WorkflowConcurrencyRequest request,
        CancellationToken cancellationToken);
}
