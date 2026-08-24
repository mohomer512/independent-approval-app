using Microsoft.AspNetCore.Authorization;

namespace IndependentApproval.Api.Application.Authorization;

public sealed class ApplicationUserAuthorizationHandler(
    IApplicationAccessService applicationAccessService,
    IHttpContextAccessor httpContextAccessor) :
    AuthorizationHandler<ApplicationUserRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ApplicationUserRequirement requirement)
    {
        var cancellationToken = httpContextAccessor.HttpContext?.RequestAborted
            ?? CancellationToken.None;
        var access = await applicationAccessService.ResolveAsync(
            context.User,
            cancellationToken);

        if (access.HasApplicationAccess)
        {
            context.Succeed(requirement);
        }
    }
}

public sealed class SystemAdministratorAuthorizationHandler(
    IApplicationAccessService applicationAccessService) :
    AuthorizationHandler<SystemAdministratorRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        SystemAdministratorRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated == true
            && applicationAccessService.IsSystemAdministrator(context.User.Identity.Name))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
