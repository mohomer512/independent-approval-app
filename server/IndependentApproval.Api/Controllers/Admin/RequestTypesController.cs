using IndependentApproval.Api.Application.Administration;
using IndependentApproval.Api.Application.Authorization;
using IndependentApproval.Api.Contracts.Administration.RequestTypes;
using IndependentApproval.Api.Contracts.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IndependentApproval.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/request-types")]
[Authorize(Policy = AuthorizationPolicyNames.SystemAdministrator)]
public sealed class RequestTypesController(
    IRequestTypeAdministrationService requestTypeService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<PagedResponse<RequestTypeListItemResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResponse<RequestTypeListItemResponse>>> List(
        [FromQuery] string? search = null,
        [FromQuery] bool includeArchived = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken cancellationToken = default) =>
        Ok(await requestTypeService.ListAsync(
            search,
            includeArchived,
            page,
            pageSize,
            cancellationToken));

    [HttpGet("system-fields")]
    [ProducesResponseType<IReadOnlyList<SystemRequestFieldResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public ActionResult<IReadOnlyList<SystemRequestFieldResponse>> ListSystemFields() =>
        Ok(requestTypeService.ListSystemFields());

    [HttpGet("{id:guid}")]
    [ProducesResponseType<RequestTypeDetailResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RequestTypeDetailResponse>> Get(
        Guid id,
        CancellationToken cancellationToken) =>
        Ok(await requestTypeService.GetAsync(id, cancellationToken));

    [HttpGet("{requestTypeId:guid}/versions/{versionId:guid}")]
    [ProducesResponseType<RequestTypeVersionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RequestTypeVersionResponse>> GetVersion(
        Guid requestTypeId,
        Guid versionId,
        CancellationToken cancellationToken) =>
        Ok(await requestTypeService.GetVersionAsync(
            requestTypeId,
            versionId,
            cancellationToken));

    [HttpPost]
    [ProducesResponseType<RequestTypeDetailResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RequestTypeDetailResponse>> Create(
        [FromBody] CreateRequestTypeRequest request,
        CancellationToken cancellationToken)
    {
        var response = await requestTypeService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = response.Id }, response);
    }

    [HttpPut("{requestTypeId:guid}/versions/{versionId:guid}")]
    [ProducesResponseType<RequestTypeVersionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RequestTypeVersionResponse>> UpdateVersion(
        Guid requestTypeId,
        Guid versionId,
        [FromBody] UpdateRequestTypeVersionRequest request,
        CancellationToken cancellationToken) =>
        Ok(await requestTypeService.UpdateVersionAsync(
            requestTypeId,
            versionId,
            request,
            cancellationToken));

    [HttpPost("{requestTypeId:guid}/versions/{versionId:guid}/fields")]
    [ProducesResponseType<RequestTypeVersionResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RequestTypeVersionResponse>> AddField(
        Guid requestTypeId,
        Guid versionId,
        [FromBody] CreateRequestFieldDefinitionRequest request,
        CancellationToken cancellationToken)
    {
        var response = await requestTypeService.AddFieldAsync(
            requestTypeId,
            versionId,
            request,
            cancellationToken);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpPut("{requestTypeId:guid}/versions/{versionId:guid}/fields/{fieldId:guid}")]
    [ProducesResponseType<RequestTypeVersionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RequestTypeVersionResponse>> UpdateField(
        Guid requestTypeId,
        Guid versionId,
        Guid fieldId,
        [FromBody] UpdateRequestFieldDefinitionRequest request,
        CancellationToken cancellationToken) =>
        Ok(await requestTypeService.UpdateFieldAsync(
            requestTypeId,
            versionId,
            fieldId,
            request,
            cancellationToken));

    [HttpDelete("{requestTypeId:guid}/versions/{versionId:guid}/fields/{fieldId:guid}")]
    [ProducesResponseType<RequestTypeVersionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RequestTypeVersionResponse>> DeleteField(
        Guid requestTypeId,
        Guid versionId,
        Guid fieldId,
        [FromBody] DeleteRequestFieldDefinitionRequest request,
        CancellationToken cancellationToken) =>
        Ok(await requestTypeService.DeleteFieldAsync(
            requestTypeId,
            versionId,
            fieldId,
            request,
            cancellationToken));

    [HttpPost("{requestTypeId:guid}/versions/{versionId:guid}/publish")]
    [ProducesResponseType<RequestTypeVersionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RequestTypeVersionResponse>> Publish(
        Guid requestTypeId,
        Guid versionId,
        [FromBody] RequestTypeConcurrencyRequest request,
        CancellationToken cancellationToken) =>
        Ok(await requestTypeService.PublishAsync(
            requestTypeId,
            versionId,
            request,
            cancellationToken));

    [HttpPost("{requestTypeId:guid}/versions/{versionId:guid}/clone")]
    [ProducesResponseType<RequestTypeVersionResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RequestTypeVersionResponse>> Clone(
        Guid requestTypeId,
        Guid versionId,
        [FromBody] RequestTypeConcurrencyRequest request,
        CancellationToken cancellationToken)
    {
        var response = await requestTypeService.CloneAsync(
            requestTypeId,
            versionId,
            request,
            cancellationToken);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpPost("{requestTypeId:guid}/archive")]
    [ProducesResponseType<RequestTypeDetailResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RequestTypeDetailResponse>> Archive(
        Guid requestTypeId,
        [FromBody] RequestTypeConcurrencyRequest request,
        CancellationToken cancellationToken) =>
        Ok(await requestTypeService.ArchiveAsync(
            requestTypeId,
            request,
            cancellationToken));

    [HttpPost("{requestTypeId:guid}/versions/{versionId:guid}/archive")]
    [ProducesResponseType<RequestTypeVersionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RequestTypeVersionResponse>> ArchiveVersion(
        Guid requestTypeId,
        Guid versionId,
        [FromBody] RequestTypeConcurrencyRequest request,
        CancellationToken cancellationToken) =>
        Ok(await requestTypeService.ArchiveVersionAsync(
            requestTypeId,
            versionId,
            request,
            cancellationToken));
}
