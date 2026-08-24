using IndependentApproval.Api.Contracts.Administration.RequestTypes;
using IndependentApproval.Api.Contracts.Common;

namespace IndependentApproval.Api.Application.Administration;

public interface IRequestTypeAdministrationService
{
    Task<PagedResponse<RequestTypeListItemResponse>> ListAsync(
        string? search,
        bool includeArchived,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<RequestTypeDetailResponse> GetAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<RequestTypeVersionResponse> GetVersionAsync(
        Guid requestTypeId,
        Guid versionId,
        CancellationToken cancellationToken);

    IReadOnlyList<SystemRequestFieldResponse> ListSystemFields();

    Task<RequestTypeDetailResponse> CreateAsync(
        CreateRequestTypeRequest request,
        CancellationToken cancellationToken);

    Task<RequestTypeVersionResponse> UpdateVersionAsync(
        Guid requestTypeId,
        Guid versionId,
        UpdateRequestTypeVersionRequest request,
        CancellationToken cancellationToken);

    Task<RequestTypeVersionResponse> AddFieldAsync(
        Guid requestTypeId,
        Guid versionId,
        CreateRequestFieldDefinitionRequest request,
        CancellationToken cancellationToken);

    Task<RequestTypeVersionResponse> UpdateFieldAsync(
        Guid requestTypeId,
        Guid versionId,
        Guid fieldId,
        UpdateRequestFieldDefinitionRequest request,
        CancellationToken cancellationToken);

    Task<RequestTypeVersionResponse> DeleteFieldAsync(
        Guid requestTypeId,
        Guid versionId,
        Guid fieldId,
        DeleteRequestFieldDefinitionRequest request,
        CancellationToken cancellationToken);

    Task<RequestTypeVersionResponse> PublishAsync(
        Guid requestTypeId,
        Guid versionId,
        RequestTypeConcurrencyRequest request,
        CancellationToken cancellationToken);

    Task<RequestTypeVersionResponse> CloneAsync(
        Guid requestTypeId,
        Guid versionId,
        RequestTypeConcurrencyRequest request,
        CancellationToken cancellationToken);

    Task<RequestTypeDetailResponse> ArchiveAsync(
        Guid requestTypeId,
        RequestTypeConcurrencyRequest request,
        CancellationToken cancellationToken);

    Task<RequestTypeVersionResponse> ArchiveVersionAsync(
        Guid requestTypeId,
        Guid versionId,
        RequestTypeConcurrencyRequest request,
        CancellationToken cancellationToken);
}
