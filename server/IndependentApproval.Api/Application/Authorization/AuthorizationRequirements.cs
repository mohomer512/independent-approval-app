using Microsoft.AspNetCore.Authorization;

namespace IndependentApproval.Api.Application.Authorization;

public sealed class ApplicationUserRequirement : IAuthorizationRequirement;

public sealed class SystemAdministratorRequirement : IAuthorizationRequirement;
