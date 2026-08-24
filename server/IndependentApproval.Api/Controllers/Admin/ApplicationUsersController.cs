using IndependentApproval.Api.Application.Administration;
using IndependentApproval.Api.Application.Authorization;
using IndependentApproval.Api.Contracts.Administration.Users;
using IndependentApproval.Api.Contracts.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IndependentApproval.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/users")]
[Authorize(Policy = AuthorizationPolicyNames.SystemAdministrator)]
public sealed class ApplicationUsersController(
    IApplicationUserAdministrationService userService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<ApplicationUserResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<ApplicationUserResponse>> Add(
        [FromBody] AddApplicationUserRequest request,
        CancellationToken cancellationToken)
    {
        var response = await userService.AddAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = response.Id }, response);
    }

    [HttpGet]
    [ProducesResponseType<PagedResponse<ApplicationUserResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResponse<ApplicationUserResponse>>> List(
        [FromQuery] string? search = null,
        [FromQuery] bool includeRemoved = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken cancellationToken = default) =>
        Ok(await userService.ListAsync(
            search,
            includeRemoved,
            page,
            pageSize,
            cancellationToken));

    [HttpGet("{id:guid}")]
    [ProducesResponseType<ApplicationUserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApplicationUserResponse>> Get(
        Guid id,
        CancellationToken cancellationToken) =>
        Ok(await userService.GetAsync(id, cancellationToken));

    [HttpPut("{id:guid}/roles")]
    [ProducesResponseType<ApplicationUserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApplicationUserResponse>> UpdateRoles(
        Guid id,
        [FromBody] UpdateApplicationUserRolesRequest request,
        CancellationToken cancellationToken) =>
        Ok(await userService.UpdateRolesAsync(id, request, cancellationToken));

    [HttpPost("{id:guid}/lock")]
    [ProducesResponseType<ApplicationUserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApplicationUserResponse>> Lock(
        Guid id,
        [FromBody] LockApplicationUserRequest request,
        CancellationToken cancellationToken) =>
        Ok(await userService.LockAsync(id, request, cancellationToken));

    [HttpPost("{id:guid}/unlock")]
    [ProducesResponseType<ApplicationUserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApplicationUserResponse>> Unlock(
        Guid id,
        [FromBody] ApplicationUserConcurrencyRequest request,
        CancellationToken cancellationToken) =>
        Ok(await userService.UnlockAsync(id, request, cancellationToken));

    [HttpPost("{id:guid}/remove")]
    [ProducesResponseType<ApplicationUserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApplicationUserResponse>> Remove(
        Guid id,
        [FromBody] ApplicationUserConcurrencyRequest request,
        CancellationToken cancellationToken) =>
        Ok(await userService.RemoveAsync(id, request, cancellationToken));

    [HttpPost("{id:guid}/restore")]
    [ProducesResponseType<ApplicationUserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApplicationUserResponse>> Restore(
        Guid id,
        [FromBody] ApplicationUserConcurrencyRequest request,
        CancellationToken cancellationToken) =>
        Ok(await userService.RestoreAsync(id, request, cancellationToken));

    [HttpPost("{id:guid}/refresh-directory-profile")]
    [ProducesResponseType<ApplicationUserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<ApplicationUserResponse>> RefreshDirectoryProfile(
        Guid id,
        [FromBody] ApplicationUserConcurrencyRequest request,
        CancellationToken cancellationToken) =>
        Ok(await userService.RefreshDirectoryProfileAsync(
            id,
            request,
            cancellationToken));
}
