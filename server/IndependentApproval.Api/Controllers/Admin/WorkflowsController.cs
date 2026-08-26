using IndependentApproval.Api.Application.Administration;
using IndependentApproval.Api.Application.Authorization;
using IndependentApproval.Api.Contracts.Administration.Workflows;
using IndependentApproval.Api.Contracts.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IndependentApproval.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/workflows")]
[Authorize(Policy = AuthorizationPolicyNames.SystemAdministrator)]
public sealed class WorkflowsController(
    IWorkflowAdministrationService workflowService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<PagedResponse<WorkflowListItemResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResponse<WorkflowListItemResponse>>> List(
        [FromQuery] string? search = null,
        [FromQuery] bool includeArchived = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken cancellationToken = default) =>
        Ok(await workflowService.ListAsync(
            search,
            includeArchived,
            page,
            pageSize,
            cancellationToken));

    [HttpGet("options")]
    [ProducesResponseType<WorkflowOptionsResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<WorkflowOptionsResponse>> GetOptions(
        CancellationToken cancellationToken) =>
        Ok(await workflowService.GetOptionsAsync(cancellationToken));

    [HttpGet("{workflowId:guid}")]
    [ProducesResponseType<WorkflowDetailResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WorkflowDetailResponse>> Get(
        Guid workflowId,
        CancellationToken cancellationToken) =>
        Ok(await workflowService.GetAsync(workflowId, cancellationToken));

    [HttpGet("{workflowId:guid}/versions/{versionId:guid}")]
    [ProducesResponseType<WorkflowVersionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WorkflowVersionResponse>> GetVersion(
        Guid workflowId,
        Guid versionId,
        CancellationToken cancellationToken) =>
        Ok(await workflowService.GetVersionAsync(
            workflowId,
            versionId,
            cancellationToken));

    [HttpPost]
    [ProducesResponseType<WorkflowDetailResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<WorkflowDetailResponse>> Create(
        [FromBody] CreateWorkflowRequest request,
        CancellationToken cancellationToken)
    {
        var response = await workflowService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { workflowId = response.Id }, response);
    }

    [HttpPut("{workflowId:guid}/versions/{versionId:guid}")]
    [ProducesResponseType<WorkflowVersionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<WorkflowVersionResponse>> UpdateVersion(
        Guid workflowId,
        Guid versionId,
        [FromBody] UpdateWorkflowVersionRequest request,
        CancellationToken cancellationToken) =>
        Ok(await workflowService.UpdateVersionAsync(
            workflowId,
            versionId,
            request,
            cancellationToken));

    [HttpPut("{workflowId:guid}/versions/{versionId:guid}/starter-roles")]
    [ProducesResponseType<WorkflowVersionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<WorkflowVersionResponse>> ReplaceStarterRoles(
        Guid workflowId,
        Guid versionId,
        [FromBody] ReplaceWorkflowStarterRolesRequest request,
        CancellationToken cancellationToken) =>
        Ok(await workflowService.ReplaceStarterRolesAsync(
            workflowId,
            versionId,
            request,
            cancellationToken));

    [HttpPost("{workflowId:guid}/versions/{versionId:guid}/steps")]
    [ProducesResponseType<WorkflowVersionResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<WorkflowVersionResponse>> AddStep(
        Guid workflowId,
        Guid versionId,
        [FromBody] CreateWorkflowStepRequest request,
        CancellationToken cancellationToken)
    {
        var response = await workflowService.AddStepAsync(
            workflowId,
            versionId,
            request,
            cancellationToken);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpPut("{workflowId:guid}/versions/{versionId:guid}/steps/{stepId:guid}")]
    [ProducesResponseType<WorkflowVersionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<WorkflowVersionResponse>> UpdateStep(
        Guid workflowId,
        Guid versionId,
        Guid stepId,
        [FromBody] UpdateWorkflowStepRequest request,
        CancellationToken cancellationToken) =>
        Ok(await workflowService.UpdateStepAsync(
            workflowId,
            versionId,
            stepId,
            request,
            cancellationToken));

    [HttpDelete("{workflowId:guid}/versions/{versionId:guid}/steps/{stepId:guid}")]
    [ProducesResponseType<WorkflowVersionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<WorkflowVersionResponse>> DeleteStep(
        Guid workflowId,
        Guid versionId,
        Guid stepId,
        [FromBody] DeleteWorkflowStepRequest request,
        CancellationToken cancellationToken) =>
        Ok(await workflowService.DeleteStepAsync(
            workflowId,
            versionId,
            stepId,
            request,
            cancellationToken));

    [HttpPut("{workflowId:guid}/versions/{versionId:guid}/steps/{stepId:guid}/roles")]
    [ProducesResponseType<WorkflowVersionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<WorkflowVersionResponse>> ReplaceStepRoles(
        Guid workflowId,
        Guid versionId,
        Guid stepId,
        [FromBody] ReplaceWorkflowStepRolesRequest request,
        CancellationToken cancellationToken) =>
        Ok(await workflowService.ReplaceStepRolesAsync(
            workflowId,
            versionId,
            stepId,
            request,
            cancellationToken));

    [HttpPut("{workflowId:guid}/versions/{versionId:guid}/steps/{stepId:guid}/field-permissions")]
    [ProducesResponseType<WorkflowVersionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<WorkflowVersionResponse>> ReplaceStepFieldPermissions(
        Guid workflowId,
        Guid versionId,
        Guid stepId,
        [FromBody] ReplaceWorkflowStepFieldPermissionsRequest request,
        CancellationToken cancellationToken) =>
        Ok(await workflowService.ReplaceStepFieldPermissionsAsync(
            workflowId,
            versionId,
            stepId,
            request,
            cancellationToken));

    [HttpPost("{workflowId:guid}/versions/{versionId:guid}/transitions")]
    [ProducesResponseType<WorkflowVersionResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<WorkflowVersionResponse>> AddTransition(
        Guid workflowId,
        Guid versionId,
        [FromBody] CreateWorkflowTransitionRequest request,
        CancellationToken cancellationToken)
    {
        var response = await workflowService.AddTransitionAsync(
            workflowId,
            versionId,
            request,
            cancellationToken);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpPut("{workflowId:guid}/versions/{versionId:guid}/transitions/{transitionId:guid}")]
    [ProducesResponseType<WorkflowVersionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<WorkflowVersionResponse>> UpdateTransition(
        Guid workflowId,
        Guid versionId,
        Guid transitionId,
        [FromBody] UpdateWorkflowTransitionRequest request,
        CancellationToken cancellationToken) =>
        Ok(await workflowService.UpdateTransitionAsync(
            workflowId,
            versionId,
            transitionId,
            request,
            cancellationToken));

    [HttpDelete("{workflowId:guid}/versions/{versionId:guid}/transitions/{transitionId:guid}")]
    [ProducesResponseType<WorkflowVersionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<WorkflowVersionResponse>> DeleteTransition(
        Guid workflowId,
        Guid versionId,
        Guid transitionId,
        [FromBody] DeleteWorkflowTransitionRequest request,
        CancellationToken cancellationToken) =>
        Ok(await workflowService.DeleteTransitionAsync(
            workflowId,
            versionId,
            transitionId,
            request,
            cancellationToken));

    [HttpPost("{workflowId:guid}/versions/{versionId:guid}/validate")]
    [ProducesResponseType<WorkflowValidationResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<WorkflowValidationResponse>> Validate(
        Guid workflowId,
        Guid versionId,
        [FromBody] ValidateWorkflowRequest request,
        CancellationToken cancellationToken) =>
        Ok(await workflowService.ValidateAsync(
            workflowId,
            versionId,
            request,
            cancellationToken));

    [HttpPost("{workflowId:guid}/versions/{versionId:guid}/publish")]
    [ProducesResponseType<WorkflowVersionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<WorkflowVersionResponse>> Publish(
        Guid workflowId,
        Guid versionId,
        [FromBody] WorkflowConcurrencyRequest request,
        CancellationToken cancellationToken) =>
        Ok(await workflowService.PublishAsync(
            workflowId,
            versionId,
            request,
            cancellationToken));

    [HttpPost("{workflowId:guid}/versions/{versionId:guid}/clone")]
    [ProducesResponseType<WorkflowVersionResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<WorkflowVersionResponse>> Clone(
        Guid workflowId,
        Guid versionId,
        [FromBody] WorkflowConcurrencyRequest request,
        CancellationToken cancellationToken)
    {
        var response = await workflowService.CloneAsync(
            workflowId,
            versionId,
            request,
            cancellationToken);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpPost("{workflowId:guid}/versions/{versionId:guid}/archive")]
    [ProducesResponseType<WorkflowVersionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<WorkflowVersionResponse>> ArchiveVersion(
        Guid workflowId,
        Guid versionId,
        [FromBody] WorkflowConcurrencyRequest request,
        CancellationToken cancellationToken) =>
        Ok(await workflowService.ArchiveVersionAsync(
            workflowId,
            versionId,
            request,
            cancellationToken));

    [HttpPost("{workflowId:guid}/archive")]
    [ProducesResponseType<WorkflowDetailResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<WorkflowDetailResponse>> Archive(
        Guid workflowId,
        [FromBody] WorkflowConcurrencyRequest request,
        CancellationToken cancellationToken) =>
        Ok(await workflowService.ArchiveAsync(
            workflowId,
            request,
            cancellationToken));
}
