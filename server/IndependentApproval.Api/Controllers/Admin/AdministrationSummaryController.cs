using IndependentApproval.Api.Application.Administration;
using IndependentApproval.Api.Application.Authorization;
using IndependentApproval.Api.Contracts.Administration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IndependentApproval.Api.Controllers.Admin;

[ApiController]
[Route("api/admin")]
[Authorize(Policy = AuthorizationPolicyNames.SystemAdministrator)]
public sealed class AdministrationSummaryController(
    IAdministrationSummaryService summaryService) : ControllerBase
{
    [HttpGet("summary")]
    [ProducesResponseType<AdministrationSummaryResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<AdministrationSummaryResponse>> GetSummary(
        CancellationToken cancellationToken) =>
        Ok(await summaryService.GetAsync(cancellationToken));
}
