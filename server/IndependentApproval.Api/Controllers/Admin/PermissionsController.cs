using IndependentApproval.Api.Application.Administration;
using IndependentApproval.Api.Application.Authorization;
using IndependentApproval.Api.Contracts.Administration.Roles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IndependentApproval.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/permissions")]
[Authorize(Policy = AuthorizationPolicyNames.SystemAdministrator)]
public sealed class PermissionsController(
    IRoleAdministrationService roleService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<PermissionResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public ActionResult<IReadOnlyList<PermissionResponse>> List() =>
        Ok(roleService.ListPermissions());
}
