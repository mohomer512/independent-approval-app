using System.Text.Json;

namespace IndependentApproval.Api.Contracts.Administration.RequestTypes;

public sealed record CreateRequestTypeRequest(
    string? Code,
    string? NameEnglish,
    string? NameArabic,
    string? DescriptionEnglish,
    string? DescriptionArabic,
    string? RequestPrefix,
    string? NavigationSlug,
    int NavigationOrder);

public sealed record UpdateRequestTypeVersionRequest(
    string? NameEnglish,
    string? NameArabic,
    string? DescriptionEnglish,
    string? DescriptionArabic,
    string? RequestPrefix,
    string? NavigationSlug,
    int NavigationOrder,
    string? RowVersion);

public sealed record CreateRequestFieldDefinitionRequest(
    string? Key,
    string? LabelEnglish,
    string? LabelArabic,
    string? HelpTextEnglish,
    string? HelpTextArabic,
    string? FieldType,
    bool IsRequired,
    JsonElement? DefaultValue,
    JsonElement? ValidationConfig,
    JsonElement? ChoiceConfig,
    int SortOrder,
    bool IsActive,
    string? DocumentMode,
    string? VersionRowVersion);

public sealed record UpdateRequestFieldDefinitionRequest(
    string? LabelEnglish,
    string? LabelArabic,
    string? HelpTextEnglish,
    string? HelpTextArabic,
    string? FieldType,
    bool IsRequired,
    JsonElement? DefaultValue,
    JsonElement? ValidationConfig,
    JsonElement? ChoiceConfig,
    int SortOrder,
    bool IsActive,
    string? DocumentMode,
    string? VersionRowVersion,
    string? FieldRowVersion);

public sealed record RequestTypeConcurrencyRequest(string? RowVersion);

public sealed record DeleteRequestFieldDefinitionRequest(
    string? VersionRowVersion,
    string? FieldRowVersion);
