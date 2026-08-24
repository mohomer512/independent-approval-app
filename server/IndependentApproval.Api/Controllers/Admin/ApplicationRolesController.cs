using IndependentApproval.Api.Application.Administration;
using IndependentApproval.Api.Application.Authorization;
using IndependentApproval.Api.Contracts.Administration.Roles;
using IndependentApproval.Api.Contracts.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IndependentApproval.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/roles")]
[Authorize(Policy = AuthorizationPolicyNames.SystemAdministrator)]
public sealed class ApplicationRolesController(
    IRoleAdministrationService roleService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<PagedResponse<ApplicationRoleResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResponse<ApplicationRoleResponse>>> List(
        [FromQuery] string? search = null,
        [FromQuery] bool includeArchived = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken cancellationToken = default) =>
        Ok(await roleService.ListAsync(
            search,
            includeArchived,
            page,
            pageSize,
            cancellationToken));

    [HttpGet("{id:guid}")]
    [ProducesResponseType<ApplicationRoleResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApplicationRoleResponse>> Get(
        Guid id,
        CancellationToken cancellationToken) =>
        Ok(await roleService.GetAsync(id, cancellationToken));

    [HttpPost]
    [ProducesResponseType<ApplicationRoleResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApplicationRoleResponse>> Create(
        [FromBody] CreateApplicationRoleRequest request,
        CancellationToken cancellationToken)
    {
        var response = await roleService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = response.Id }, response);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType<ApplicationRoleResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApplicationRoleResponse>> Update(
        Guid id,
        [FromBody] UpdateApplicationRoleRequest request,
        CancellationToken cancellationToken) =>
        Ok(await roleService.UpdateAsync(id, request, cancellationToken));

    [HttpPost("{id:guid}/archive")]
    [ProducesResponseType<ApplicationRoleResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApplicationRoleResponse>> Archive(
        Guid id,
        [FromBody] ApplicationRoleConcurrencyRequest request,
        CancellationToken cancellationToken) =>
        Ok(await roleService.ArchiveAsync(id, request, cancellationToken));
}
