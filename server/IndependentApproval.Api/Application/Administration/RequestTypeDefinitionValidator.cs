using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using IndependentApproval.Api.Contracts.Administration.RequestTypes;
using IndependentApproval.Api.Domain.Requests;

namespace IndependentApproval.Api.Application.Administration;

internal static partial class RequestTypeDefinitionValidator
{
    private const int MaximumCodeLength = 64;
    private const int MaximumNameLength = 200;
    private const int MaximumDescriptionLength = 2000;
    private const int MaximumHelpTextLength = 1000;
    private const int MaximumPrefixLength = 12;
    private const int MaximumSlugLength = 100;
    private const int MaximumFieldKeyLength = 64;
    private const int MaximumPatternLength = 512;
    private const int MaximumDefaultJsonBytes = 16 * 1024;
    private const int MaximumValidationJsonBytes = 16 * 1024;
    private const int MaximumChoiceJsonBytes = 64 * 1024;
    private const int MaximumChoiceCount = 200;

    private static readonly IReadOnlySet<string> ReservedKeys =
        new HashSet<string>(RequestSystemFieldKeys.All, StringComparer.OrdinalIgnoreCase)
        {
            "id",
            "rowVersion",
            "requestTypeId",
            "requestTypeVersionId",
            "workflowDefinitionId",
            "workflowVersionId",
            "currentWorkflowStepId"
        };

    public static RequestTypeCreateValues ValidateCreate(
        CreateRequestTypeRequest request)
    {
        var errors = NewErrors();
        var code = NormalizeRequired(request.Code, "code", MaximumCodeLength, errors);

        if (code is not null && !CodePattern().IsMatch(code))
        {
            AddError(errors, "code", "Code must start with an ASCII letter or number and contain only letters, numbers, dots, hyphens or underscores.");
        }

        var version = ValidateVersionValues(
            request.NameEnglish,
            request.NameArabic,
            request.DescriptionEnglish,
            request.DescriptionArabic,
            request.RequestPrefix,
            request.NavigationSlug,
            request.NavigationOrder,
            errors);
        ThrowIfInvalid(errors);

        return new RequestTypeCreateValues(
            code!.ToUpperInvariant(),
            version);
    }

    public static RequestTypeVersionValues ValidateVersion(
        UpdateRequestTypeVersionRequest request)
    {
        var errors = NewErrors();
        var values = ValidateVersionValues(
            request.NameEnglish,
            request.NameArabic,
            request.DescriptionEnglish,
            request.DescriptionArabic,
            request.RequestPrefix,
            request.NavigationSlug,
            request.NavigationOrder,
            errors);
        ThrowIfInvalid(errors);
        return values;
    }

    public static RequestFieldValues ValidateField(
        CreateRequestFieldDefinitionRequest request)
    {
        var errors = NewErrors();
        var key = ValidateKey(request.Key, errors);
        var values = ValidateFieldValues(
            key,
            request.LabelEnglish,
            request.LabelArabic,
            request.HelpTextEnglish,
            request.HelpTextArabic,
            request.FieldType,
            request.IsRequired,
            request.DefaultValue,
            request.ValidationConfig,
            request.ChoiceConfig,
            request.SortOrder,
            request.IsActive,
            request.DocumentMode,
            errors);
        ThrowIfInvalid(errors);
        return values;
    }

    public static RequestFieldValues ValidateField(
        string existingKey,
        UpdateRequestFieldDefinitionRequest request)
    {
        var errors = NewErrors();
        var values = ValidateFieldValues(
            existingKey,
            request.LabelEnglish,
            request.LabelArabic,
            request.HelpTextEnglish,
            request.HelpTextArabic,
            request.FieldType,
            request.IsRequired,
            request.DefaultValue,
            request.ValidationConfig,
            request.ChoiceConfig,
            request.SortOrder,
            request.IsActive,
            request.DocumentMode,
            errors);
        ThrowIfInvalid(errors);
        return values;
    }

    public static string FieldTypeName(RequestFieldType fieldType) => fieldType switch
    {
        RequestFieldType.ShortText => "shortText",
        RequestFieldType.LongText => "longText",
        RequestFieldType.Integer => "integer",
        RequestFieldType.Decimal => "decimal",
        RequestFieldType.Date => "date",
        RequestFieldType.DateTime => "dateTime",
        RequestFieldType.YesNo => "yesNo",
        RequestFieldType.SingleChoice => "singleChoice",
        RequestFieldType.MultipleChoice => "multipleChoice",
        RequestFieldType.ActiveDirectoryUser => "activeDirectoryUser",
        RequestFieldType.ApplicationRole => "applicationRole",
        RequestFieldType.FileDocument => "fileDocument",
        RequestFieldType.RichDocument => "richDocument",
        _ => throw new ArgumentOutOfRangeException(nameof(fieldType))
    };

    public static string? DocumentModeName(DocumentFieldMode? mode) => mode switch
    {
        null => null,
        DocumentFieldMode.UploadOnly => "uploadOnly",
        DocumentFieldMode.CreateInEditorOnly => "createInEditorOnly",
        DocumentFieldMode.UploadOrCreate => "uploadOrCreate",
        _ => throw new ArgumentOutOfRangeException(nameof(mode))
    };

    public static string LifecycleName(RequestTypeVersionLifecycle lifecycle) => lifecycle switch
    {
        RequestTypeVersionLifecycle.Draft => "draft",
        RequestTypeVersionLifecycle.Published => "published",
        RequestTypeVersionLifecycle.Archived => "archived",
        _ => throw new ArgumentOutOfRangeException(nameof(lifecycle))
    };

    private static RequestTypeVersionValues ValidateVersionValues(
        string? nameEnglish,
        string? nameArabic,
        string? descriptionEnglish,
        string? descriptionArabic,
        string? requestPrefix,
        string? navigationSlug,
        int navigationOrder,
        IDictionary<string, List<string>> errors)
    {
        var normalizedNameEnglish = NormalizeRequired(
            nameEnglish,
            "nameEnglish",
            MaximumNameLength,
            errors);
        var normalizedNameArabic = NormalizeRequired(
            nameArabic,
            "nameArabic",
            MaximumNameLength,
            errors);
        var normalizedDescriptionEnglish = NormalizeRequired(
            descriptionEnglish,
            "descriptionEnglish",
            MaximumDescriptionLength,
            errors);
        var normalizedDescriptionArabic = NormalizeRequired(
            descriptionArabic,
            "descriptionArabic",
            MaximumDescriptionLength,
            errors);
        var normalizedPrefix = NormalizeRequired(
            requestPrefix,
            "requestPrefix",
            MaximumPrefixLength,
            errors)?.ToUpperInvariant();
        var normalizedSlug = NormalizeRequired(
            navigationSlug,
            "navigationSlug",
            MaximumSlugLength,
            errors)?.ToLowerInvariant();

        if (normalizedPrefix is not null && !PrefixPattern().IsMatch(normalizedPrefix))
        {
            AddError(errors, "requestPrefix", "Request prefix must start with an ASCII letter and contain only letters or numbers.");
        }

        if (normalizedSlug is not null && !SlugPattern().IsMatch(normalizedSlug))
        {
            AddError(errors, "navigationSlug", "Navigation slug must contain lowercase letters or numbers separated by single hyphens.");
        }

        if (navigationOrder is < 0 or > 100000)
        {
            AddError(errors, "navigationOrder", "Navigation order must be between 0 and 100000.");
        }

        return new RequestTypeVersionValues(
            normalizedNameEnglish!,
            normalizedNameArabic!,
            normalizedDescriptionEnglish!,
            normalizedDescriptionArabic!,
            normalizedPrefix!,
            normalizedSlug!,
            navigationOrder);
    }

    private static string ValidateKey(
        string? key,
        IDictionary<string, List<string>> errors)
    {
        var normalizedKey = NormalizeRequired(
            key,
            "key",
            MaximumFieldKeyLength,
            errors)?.ToLowerInvariant();

        if (normalizedKey is not null && !FieldKeyPattern().IsMatch(normalizedKey))
        {
            AddError(errors, "key", "Field key must start with a lowercase letter and contain only lowercase letters, numbers or underscores.");
        }

        if (normalizedKey is not null && ReservedKeys.Contains(normalizedKey))
        {
            AddError(errors, "key", "This key is reserved for a protected system field.");
        }

        return normalizedKey ?? string.Empty;
    }

    private static RequestFieldValues ValidateFieldValues(
        string key,
        string? labelEnglish,
        string? labelArabic,
        string? helpTextEnglish,
        string? helpTextArabic,
        string? fieldType,
        bool isRequired,
        JsonElement? defaultValue,
        JsonElement? validationConfig,
        JsonElement? choiceConfig,
        int sortOrder,
        bool isActive,
        string? documentMode,
        IDictionary<string, List<string>> errors)
    {
        var normalizedLabelEnglish = NormalizeRequired(
            labelEnglish,
            "labelEnglish",
            MaximumNameLength,
            errors);
        var normalizedLabelArabic = NormalizeRequired(
            labelArabic,
            "labelArabic",
            MaximumNameLength,
            errors);
        var normalizedHelpEnglish = NormalizeOptional(
            helpTextEnglish,
            "helpTextEnglish",
            MaximumHelpTextLength,
            errors);
        var normalizedHelpArabic = NormalizeOptional(
            helpTextArabic,
            "helpTextArabic",
            MaximumHelpTextLength,
            errors);
        var parsedFieldType = ParseFieldType(fieldType, errors);
        var parsedDocumentMode = ParseDocumentMode(
            documentMode,
            parsedFieldType,
            errors);

        if (sortOrder is < 0 or > 100000)
        {
            AddError(errors, "sortOrder", "Sort order must be between 0 and 100000.");
        }

        var validationRules = ValidateValidationConfig(
            parsedFieldType,
            validationConfig,
            errors);
        var choiceValues = ValidateChoiceConfig(
            parsedFieldType,
            choiceConfig,
            errors);
        ValidateDefaultValue(
            parsedFieldType,
            defaultValue,
            validationRules,
            choiceValues,
            errors);

        var defaultValueJson = SerializeBounded(
            defaultValue,
            "defaultValue",
            MaximumDefaultJsonBytes,
            errors);
        var validationJson = SerializeBounded(
            validationConfig,
            "validationConfig",
            MaximumValidationJsonBytes,
            errors);
        var choiceJson = SerializeBounded(
            choiceConfig,
            "choiceConfig",
            MaximumChoiceJsonBytes,
            errors);

        return new RequestFieldValues(
            key,
            normalizedLabelEnglish!,
            normalizedLabelArabic!,
            normalizedHelpEnglish,
            normalizedHelpArabic,
            parsedFieldType,
            isRequired,
            defaultValueJson,
            validationJson,
            choiceJson,
            sortOrder,
            isActive,
            parsedDocumentMode);
    }

    private static RequestFieldType ParseFieldType(
        string? value,
        IDictionary<string, List<string>> errors)
    {
        var normalized = value?.Trim();
        var parsed = normalized?.ToLowerInvariant() switch
        {
            "shorttext" => RequestFieldType.ShortText,
            "longtext" => RequestFieldType.LongText,
            "integer" => RequestFieldType.Integer,
            "decimal" => RequestFieldType.Decimal,
            "date" => RequestFieldType.Date,
            "datetime" => RequestFieldType.DateTime,
            "yesno" => RequestFieldType.YesNo,
            "singlechoice" => RequestFieldType.SingleChoice,
            "multiplechoice" => RequestFieldType.MultipleChoice,
            "activedirectoryuser" => RequestFieldType.ActiveDirectoryUser,
            "applicationrole" => RequestFieldType.ApplicationRole,
            "filedocument" => RequestFieldType.FileDocument,
            "richdocument" => RequestFieldType.RichDocument,
            _ => (RequestFieldType?)null
        };

        if (parsed is null)
        {
            AddError(errors, "fieldType", "Field type is not supported.");
            return RequestFieldType.ShortText;
        }

        return parsed.Value;
    }

    private static DocumentFieldMode? ParseDocumentMode(
        string? value,
        RequestFieldType fieldType,
        IDictionary<string, List<string>> errors)
    {
        var isDocument = fieldType is RequestFieldType.FileDocument
            or RequestFieldType.RichDocument;

        if (!isDocument)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                AddError(errors, "documentMode", "Document mode is only valid for document fields.");
            }

            return null;
        }

        var parsed = value?.Trim().ToLowerInvariant() switch
        {
            "uploadonly" => DocumentFieldMode.UploadOnly,
            "createineditoronly" => DocumentFieldMode.CreateInEditorOnly,
            "uploadorcreate" => DocumentFieldMode.UploadOrCreate,
            _ => (DocumentFieldMode?)null
        };

        if (parsed is null)
        {
            AddError(errors, "documentMode", "A supported document mode is required for document fields.");
        }

        return parsed;
    }

    private static ValidationRules ValidateValidationConfig(
        RequestFieldType fieldType,
        JsonElement? config,
        IDictionary<string, List<string>> errors)
    {
        if (IsNull(config))
        {
            return new ValidationRules();
        }

        if (config!.Value.ValueKind != JsonValueKind.Object)
        {
            AddError(errors, "validationConfig", "Validation configuration must be a JSON object or null.");
            return new ValidationRules();
        }

        var rules = new ValidationRules();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var allowed = AllowedValidationKeys(fieldType);

        foreach (var property in config.Value.EnumerateObject())
        {
            if (!seen.Add(property.Name))
            {
                AddError(errors, "validationConfig", $"Validation key '{property.Name}' is duplicated.");
                continue;
            }

            if (!allowed.Contains(property.Name))
            {
                AddError(errors, "validationConfig", $"Validation key '{property.Name}' is not valid for {FieldTypeName(fieldType)}.");
                continue;
            }

            switch (property.Name)
            {
                case "minLength":
                    rules.MinLength = ReadBoundedInteger(property.Value, 0, 10000, property.Name, errors);
                    break;
                case "maxLength":
                    var textMaximum = fieldType == RequestFieldType.ShortText ? 1000 : 10000;
                    rules.MaxLength = ReadBoundedInteger(property.Value, 1, textMaximum, property.Name, errors);
                    break;
                case "pattern":
                    rules.Pattern = ReadPattern(property.Value, errors);
                    break;
                case "minimum":
                    ReadMinimum(fieldType, property.Value, rules, errors);
                    break;
                case "maximum":
                    ReadMaximum(fieldType, property.Value, rules, errors);
                    break;
                case "decimalPlaces":
                    rules.DecimalPlaces = ReadBoundedInteger(property.Value, 0, 8, property.Name, errors);
                    break;
                case "minimumSelections":
                    rules.MinimumSelections = ReadBoundedInteger(property.Value, 0, MaximumChoiceCount, property.Name, errors);
                    break;
                case "maximumSelections":
                    rules.MaximumSelections = ReadBoundedInteger(property.Value, 1, MaximumChoiceCount, property.Name, errors);
                    break;
                case "maximumDocuments":
                    rules.MaximumDocuments = ReadBoundedInteger(property.Value, 1, 100, property.Name, errors);
                    break;
                case "allowedExtensions":
                    ValidateAllowedExtensions(property.Value, errors);
                    break;
            }
        }

        if (rules.MinLength > rules.MaxLength)
        {
            AddError(errors, "validationConfig", "minLength cannot exceed maxLength.");
        }

        if (rules.NumericMinimum > rules.NumericMaximum)
        {
            AddError(errors, "validationConfig", "minimum cannot exceed maximum.");
        }

        if (rules.DateMinimum > rules.DateMaximum
            || rules.DateTimeMinimum > rules.DateTimeMaximum)
        {
            AddError(errors, "validationConfig", "minimum cannot be later than maximum.");
        }

        if (rules.MinimumSelections > rules.MaximumSelections)
        {
            AddError(errors, "validationConfig", "minimumSelections cannot exceed maximumSelections.");
        }

        return rules;
    }

    private static IReadOnlySet<string> AllowedValidationKeys(RequestFieldType fieldType) =>
        fieldType switch
        {
            RequestFieldType.ShortText or RequestFieldType.LongText =>
                new HashSet<string>(StringComparer.Ordinal) { "minLength", "maxLength", "pattern" },
            RequestFieldType.Integer =>
                new HashSet<string>(StringComparer.Ordinal) { "minimum", "maximum" },
            RequestFieldType.Decimal =>
                new HashSet<string>(StringComparer.Ordinal) { "minimum", "maximum", "decimalPlaces" },
            RequestFieldType.Date or RequestFieldType.DateTime =>
                new HashSet<string>(StringComparer.Ordinal) { "minimum", "maximum" },
            RequestFieldType.MultipleChoice =>
                new HashSet<string>(StringComparer.Ordinal) { "minimumSelections", "maximumSelections" },
            RequestFieldType.FileDocument or RequestFieldType.RichDocument =>
                new HashSet<string>(StringComparer.Ordinal) { "maximumDocuments", "allowedExtensions" },
            _ => new HashSet<string>(StringComparer.Ordinal)
        };

    private static IReadOnlySet<string> ValidateChoiceConfig(
        RequestFieldType fieldType,
        JsonElement? config,
        IDictionary<string, List<string>> errors)
    {
        var isChoice = fieldType is RequestFieldType.SingleChoice
            or RequestFieldType.MultipleChoice;

        if (!isChoice)
        {
            if (!IsNull(config))
            {
                AddError(errors, "choiceConfig", "Choice configuration is only valid for choice fields.");
            }

            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        if (IsNull(config) || config!.Value.ValueKind != JsonValueKind.Object)
        {
            AddError(errors, "choiceConfig", "Choice fields require a choice configuration object.");
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        JsonElement? options = null;

        foreach (var property in config.Value.EnumerateObject())
        {
            if (property.Name != "options" || options is not null)
            {
                AddError(errors, "choiceConfig", $"Choice configuration contains an unsupported or duplicate key '{property.Name}'.");
                continue;
            }

            options = property.Value;
        }

        if (options is null || options.Value.ValueKind != JsonValueKind.Array)
        {
            AddError(errors, "choiceConfig", "choiceConfig.options must be an array.");
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        var optionArray = options.Value.EnumerateArray().ToArray();

        if (optionArray.Length is < 1 or > MaximumChoiceCount)
        {
            AddError(errors, "choiceConfig", $"Choice configuration must contain between 1 and {MaximumChoiceCount} options.");
        }

        var values = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var activeValues = new HashSet<string>(StringComparer.Ordinal);
        var orders = new HashSet<int>();

        foreach (var option in optionArray)
        {
            ValidateChoiceOption(option, values, activeValues, orders, errors);
        }

        if (activeValues.Count == 0)
        {
            AddError(errors, "choiceConfig", "Choice configuration must contain at least one active option.");
        }

        return activeValues;
    }

    private static void ValidateChoiceOption(
        JsonElement option,
        ISet<string> values,
        ISet<string> activeValues,
        ISet<int> orders,
        IDictionary<string, List<string>> errors)
    {
        if (option.ValueKind != JsonValueKind.Object)
        {
            AddError(errors, "choiceConfig", "Every choice option must be an object.");
            return;
        }

        string? value = null;
        string? labelEnglish = null;
        string? labelArabic = null;
        int? sortOrder = null;
        bool? isActive = null;
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var property in option.EnumerateObject())
        {
            if (!seen.Add(property.Name))
            {
                AddError(errors, "choiceConfig", $"Choice option key '{property.Name}' is duplicated.");
                continue;
            }

            switch (property.Name)
            {
                case "value" when property.Value.ValueKind == JsonValueKind.String:
                    value = ReadCanonicalChoiceText(
                        property.Value,
                        property.Name,
                        errors);
                    break;
                case "labelEnglish" when property.Value.ValueKind == JsonValueKind.String:
                    labelEnglish = ReadCanonicalChoiceText(
                        property.Value,
                        property.Name,
                        errors);
                    break;
                case "labelArabic" when property.Value.ValueKind == JsonValueKind.String:
                    labelArabic = ReadCanonicalChoiceText(
                        property.Value,
                        property.Name,
                        errors);
                    break;
                case "sortOrder" when property.Value.TryGetInt32(out var order):
                    sortOrder = order;
                    break;
                case "isActive" when property.Value.ValueKind is JsonValueKind.True or JsonValueKind.False:
                    isActive = property.Value.GetBoolean();
                    break;
                default:
                    AddError(errors, "choiceConfig", $"Choice option key '{property.Name}' is unsupported or has an invalid value.");
                    break;
            }
        }

        if (string.IsNullOrWhiteSpace(value) || value.Length > 128)
        {
            AddError(errors, "choiceConfig", "Each choice value is required and cannot exceed 128 characters.");
        }
        else if (!values.Add(value))
        {
            AddError(errors, "choiceConfig", $"Choice value '{value}' is duplicated.");
        }

        if (string.IsNullOrWhiteSpace(labelEnglish) || labelEnglish.Length > MaximumNameLength
            || string.IsNullOrWhiteSpace(labelArabic) || labelArabic.Length > MaximumNameLength)
        {
            AddError(errors, "choiceConfig", "Each choice requires bounded English and Arabic labels.");
        }

        if (sortOrder is null)
        {
            AddError(errors, "choiceConfig", "Each choice requires a sortOrder integer.");
        }
        else if (sortOrder is < 0 or > 100000)
        {
            AddError(errors, "choiceConfig", "Each choice sortOrder must be between 0 and 100000.");
        }
        else if (sortOrder is int order && !orders.Add(order))
        {
            AddError(errors, "choiceConfig", $"Choice sortOrder '{order}' is duplicated.");
        }

        if (isActive is null)
        {
            AddError(errors, "choiceConfig", "Each choice requires an isActive boolean.");
        }
        else if (isActive.Value && !string.IsNullOrWhiteSpace(value))
        {
            activeValues.Add(value);
        }
    }

    private static void ValidateDefaultValue(
        RequestFieldType fieldType,
        JsonElement? defaultValue,
        ValidationRules rules,
        IReadOnlySet<string> activeChoices,
        IDictionary<string, List<string>> errors)
    {
        if (IsNull(defaultValue))
        {
            return;
        }

        var value = defaultValue!.Value;

        switch (fieldType)
        {
            case RequestFieldType.ShortText:
            case RequestFieldType.LongText:
                if (value.ValueKind != JsonValueKind.String)
                {
                    AddError(errors, "defaultValue", "Text field default must be a JSON string.");
                    return;
                }

                var text = value.GetString() ?? string.Empty;
                var maximum = fieldType == RequestFieldType.ShortText ? 1000 : 10000;

                if (text.Length > maximum
                    || rules.MinLength is int minimumLength && text.Length < minimumLength
                    || rules.MaxLength is int maximumLength && text.Length > maximumLength)
                {
                    AddError(errors, "defaultValue", "Text default does not satisfy the configured length bounds.");
                }

                try
                {
                    if (rules.Pattern is not null
                        && !Regex.IsMatch(
                            text,
                            rules.Pattern,
                            RegexOptions.CultureInvariant,
                            TimeSpan.FromMilliseconds(100)))
                    {
                        AddError(errors, "defaultValue", "Text default does not satisfy the configured pattern.");
                    }
                }
                catch (RegexMatchTimeoutException)
                {
                    AddError(errors, "defaultValue", "The configured pattern is too expensive to evaluate safely.");
                }

                break;
            case RequestFieldType.Integer:
                if (!value.TryGetInt64(out var integerValue))
                {
                    AddError(errors, "defaultValue", "Integer field default must be a JSON integer.");
                }
                else
                {
                    ValidateNumericDefault(integerValue, rules, errors);
                }

                break;
            case RequestFieldType.Decimal:
                if (!value.TryGetDecimal(out var decimalValue))
                {
                    AddError(errors, "defaultValue", "Decimal field default must be a JSON number.");
                }
                else
                {
                    ValidateNumericDefault(decimalValue, rules, errors);
                }

                break;
            case RequestFieldType.Date:
                if (value.ValueKind != JsonValueKind.String
                    || !DateOnly.TryParseExact(
                        value.GetString(),
                        "yyyy-MM-dd",
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out var dateValue))
                {
                    AddError(errors, "defaultValue", "Date field default must use the yyyy-MM-dd format.");
                }
                else if (dateValue < rules.DateMinimum || dateValue > rules.DateMaximum)
                {
                    AddError(errors, "defaultValue", "Date default is outside the configured bounds.");
                }

                break;
            case RequestFieldType.DateTime:
                if (value.ValueKind != JsonValueKind.String
                    || !TryParseExplicitOffsetDateTime(value.GetString(), out var dateTimeValue))
                {
                    AddError(errors, "defaultValue", "Date-time field default must be an ISO-8601 timestamp with an offset.");
                }
                else if (dateTimeValue < rules.DateTimeMinimum || dateTimeValue > rules.DateTimeMaximum)
                {
                    AddError(errors, "defaultValue", "Date-time default is outside the configured bounds.");
                }

                break;
            case RequestFieldType.YesNo:
                if (value.ValueKind is not JsonValueKind.True and not JsonValueKind.False)
                {
                    AddError(errors, "defaultValue", "Yes/no field default must be a JSON boolean.");
                }

                break;
            case RequestFieldType.SingleChoice:
                if (value.ValueKind != JsonValueKind.String
                    || !activeChoices.Contains(value.GetString() ?? string.Empty))
                {
                    AddError(errors, "defaultValue", "Single-choice default must match an active choice value.");
                }

                break;
            case RequestFieldType.MultipleChoice:
                ValidateMultipleChoiceDefault(value, activeChoices, rules, errors);
                break;
            default:
                AddError(errors, "defaultValue", "This field type does not support a default value.");
                break;
        }
    }

    private static string? ReadCanonicalChoiceText(
        JsonElement value,
        string propertyName,
        IDictionary<string, List<string>> errors)
    {
        var original = value.GetString();
        var normalized = original?.Trim();

        if (!string.Equals(original, normalized, StringComparison.Ordinal))
        {
            AddError(
                errors,
                "choiceConfig",
                $"Choice option {propertyName} cannot contain surrounding whitespace.");
        }

        return normalized;
    }

    private static void ValidateMultipleChoiceDefault(
        JsonElement value,
        IReadOnlySet<string> activeChoices,
        ValidationRules rules,
        IDictionary<string, List<string>> errors)
    {
        if (value.ValueKind != JsonValueKind.Array)
        {
            AddError(errors, "defaultValue", "Multiple-choice default must be a JSON string array.");
            return;
        }

        var values = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in value.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.String
                || string.IsNullOrWhiteSpace(item.GetString())
                || !activeChoices.Contains(item.GetString()!))
            {
                AddError(errors, "defaultValue", "Every multiple-choice default must match an active choice value.");
                continue;
            }

            if (!values.Add(item.GetString()!))
            {
                AddError(errors, "defaultValue", "Multiple-choice defaults must be unique.");
            }
        }

        if (rules.MinimumSelections is int minimum && values.Count < minimum
            || rules.MaximumSelections is int maximum && values.Count > maximum)
        {
            AddError(errors, "defaultValue", "Multiple-choice default does not satisfy the configured selection bounds.");
        }
    }

    private static void ValidateNumericDefault(
        decimal value,
        ValidationRules rules,
        IDictionary<string, List<string>> errors)
    {
        if (value < rules.NumericMinimum || value > rules.NumericMaximum)
        {
            AddError(errors, "defaultValue", "Numeric default is outside the configured bounds.");
        }

        if (rules.DecimalPlaces is int decimalPlaces
            && decimal.Round(value, decimalPlaces) != value)
        {
            AddError(errors, "defaultValue", "Decimal default has more fractional digits than allowed.");
        }
    }

    private static void ReadMinimum(
        RequestFieldType fieldType,
        JsonElement value,
        ValidationRules rules,
        IDictionary<string, List<string>> errors)
    {
        if (fieldType is RequestFieldType.Integer or RequestFieldType.Decimal)
        {
            if (!value.TryGetDecimal(out var minimum)
                || fieldType == RequestFieldType.Integer && decimal.Truncate(minimum) != minimum)
            {
                AddError(errors, "validationConfig", "minimum must be a number of the field's type.");
            }
            else
            {
                rules.NumericMinimum = minimum;
            }

            return;
        }

        if (fieldType == RequestFieldType.Date)
        {
            if (value.ValueKind != JsonValueKind.String
                || !DateOnly.TryParseExact(value.GetString(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var minimum))
            {
                AddError(errors, "validationConfig", "minimum must be a yyyy-MM-dd date.");
            }
            else
            {
                rules.DateMinimum = minimum;
            }

            return;
        }

        if (value.ValueKind != JsonValueKind.String
            || !TryParseExplicitOffsetDateTime(value.GetString(), out var dateTimeMinimum))
        {
            AddError(errors, "validationConfig", "minimum must be an ISO-8601 timestamp with an offset.");
        }
        else
        {
            rules.DateTimeMinimum = dateTimeMinimum;
        }
    }

    private static void ReadMaximum(
        RequestFieldType fieldType,
        JsonElement value,
        ValidationRules rules,
        IDictionary<string, List<string>> errors)
    {
        if (fieldType is RequestFieldType.Integer or RequestFieldType.Decimal)
        {
            if (!value.TryGetDecimal(out var maximum)
                || fieldType == RequestFieldType.Integer && decimal.Truncate(maximum) != maximum)
            {
                AddError(errors, "validationConfig", "maximum must be a number of the field's type.");
            }
            else
            {
                rules.NumericMaximum = maximum;
            }

            return;
        }

        if (fieldType == RequestFieldType.Date)
        {
            if (value.ValueKind != JsonValueKind.String
                || !DateOnly.TryParseExact(value.GetString(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var maximum))
            {
                AddError(errors, "validationConfig", "maximum must be a yyyy-MM-dd date.");
            }
            else
            {
                rules.DateMaximum = maximum;
            }

            return;
        }

        if (value.ValueKind != JsonValueKind.String
            || !TryParseExplicitOffsetDateTime(value.GetString(), out var dateTimeMaximum))
        {
            AddError(errors, "validationConfig", "maximum must be an ISO-8601 timestamp with an offset.");
        }
        else
        {
            rules.DateTimeMaximum = dateTimeMaximum;
        }
    }

    private static int? ReadBoundedInteger(
        JsonElement value,
        int minimum,
        int maximum,
        string key,
        IDictionary<string, List<string>> errors)
    {
        if (!value.TryGetInt32(out var parsed) || parsed < minimum || parsed > maximum)
        {
            AddError(errors, "validationConfig", $"{key} must be an integer between {minimum} and {maximum}.");
            return null;
        }

        return parsed;
    }

    private static string? ReadPattern(
        JsonElement value,
        IDictionary<string, List<string>> errors)
    {
        if (value.ValueKind != JsonValueKind.String)
        {
            AddError(errors, "validationConfig", "pattern must be a string.");
            return null;
        }

        var pattern = value.GetString();

        if (string.IsNullOrWhiteSpace(pattern) || pattern.Length > MaximumPatternLength)
        {
            AddError(errors, "validationConfig", $"pattern is required and cannot exceed {MaximumPatternLength} characters.");
            return null;
        }

        try
        {
            _ = new Regex(
                pattern,
                RegexOptions.CultureInvariant,
                TimeSpan.FromMilliseconds(100));
        }
        catch (ArgumentException)
        {
            AddError(errors, "validationConfig", "pattern is not a valid regular expression.");
            return null;
        }

        return pattern;
    }

    private static void ValidateAllowedExtensions(
        JsonElement value,
        IDictionary<string, List<string>> errors)
    {
        if (value.ValueKind != JsonValueKind.Array)
        {
            AddError(errors, "validationConfig", "allowedExtensions must be an array of extensions.");
            return;
        }

        var extensions = value.EnumerateArray().ToArray();

        if (extensions.Length is < 1 or > 20)
        {
            AddError(errors, "validationConfig", "allowedExtensions must contain between 1 and 20 entries.");
        }

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var extension in extensions)
        {
            var text = extension.ValueKind == JsonValueKind.String
                ? extension.GetString()?.Trim()
                : null;
            var original = extension.ValueKind == JsonValueKind.String
                ? extension.GetString()
                : null;

            if (string.IsNullOrWhiteSpace(text)
                || text.Length > 16
                || !ExtensionPattern().IsMatch(text)
                || !string.Equals(original, text, StringComparison.Ordinal))
            {
                AddError(errors, "validationConfig", "Every allowed extension must use a lowercase value such as '.pdf' without surrounding whitespace.");
            }
            else if (!seen.Add(text))
            {
                AddError(errors, "validationConfig", $"Allowed extension '{text}' is duplicated.");
            }
        }
    }

    private static bool TryParseExplicitOffsetDateTime(
        string? value,
        out DateTimeOffset parsed)
    {
        parsed = default;
        return value is { Length: <= 40 }
               && ExplicitOffsetDateTimePattern().IsMatch(value)
               && DateTimeOffset.TryParse(
                   value,
                   CultureInfo.InvariantCulture,
                   DateTimeStyles.RoundtripKind,
                   out parsed);
    }

    private static string? SerializeBounded(
        JsonElement? value,
        string field,
        int maximumBytes,
        IDictionary<string, List<string>> errors)
    {
        if (IsNull(value))
        {
            return null;
        }

        var json = JsonSerializer.Serialize(value!.Value);

        if (Encoding.UTF8.GetByteCount(json) > maximumBytes)
        {
            AddError(errors, field, $"JSON content cannot exceed {maximumBytes} UTF-8 bytes.");
            return null;
        }

        return json;
    }

    private static bool IsNull(JsonElement? value) =>
        value is null || value.Value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined;

    private static string? NormalizeRequired(
        string? value,
        string field,
        int maximumLength,
        IDictionary<string, List<string>> errors)
    {
        var normalized = value?.Trim();

        if (string.IsNullOrWhiteSpace(normalized))
        {
            AddError(errors, field, "A value is required.");
            return null;
        }

        if (normalized.Length > maximumLength)
        {
            AddError(errors, field, $"Value cannot exceed {maximumLength} characters.");
        }

        return normalized;
    }

    private static string? NormalizeOptional(
        string? value,
        string field,
        int maximumLength,
        IDictionary<string, List<string>> errors)
    {
        var normalized = value?.Trim();

        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }

        if (normalized.Length > maximumLength)
        {
            AddError(errors, field, $"Value cannot exceed {maximumLength} characters.");
        }

        return normalized;
    }

    private static Dictionary<string, List<string>> NewErrors() =>
        new(StringComparer.OrdinalIgnoreCase);

    private static void AddError(
        IDictionary<string, List<string>> errors,
        string field,
        string message)
    {
        if (!errors.TryGetValue(field, out var messages))
        {
            messages = [];
            errors[field] = messages;
        }

        messages.Add(message);
    }

    private static void ThrowIfInvalid(
        IReadOnlyDictionary<string, List<string>> errors)
    {
        if (errors.Count == 0)
        {
            return;
        }

        throw new AdministrationValidationException(
            errors.ToDictionary(
                pair => pair.Key,
                pair => pair.Value.Distinct(StringComparer.Ordinal).ToArray(),
                StringComparer.OrdinalIgnoreCase));
    }

    [GeneratedRegex("^[A-Za-z0-9][A-Za-z0-9._-]{0,63}$", RegexOptions.CultureInvariant)]
    private static partial Regex CodePattern();

    [GeneratedRegex("^[A-Z][A-Z0-9]{0,11}$", RegexOptions.CultureInvariant)]
    private static partial Regex PrefixPattern();

    [GeneratedRegex("^[a-z0-9]+(?:-[a-z0-9]+)*$", RegexOptions.CultureInvariant)]
    private static partial Regex SlugPattern();

    [GeneratedRegex("^[a-z][a-z0-9_]{0,63}$", RegexOptions.CultureInvariant)]
    private static partial Regex FieldKeyPattern();

    [GeneratedRegex("^\\.[a-z0-9]{1,15}$", RegexOptions.CultureInvariant)]
    private static partial Regex ExtensionPattern();

    [GeneratedRegex(
        "^[0-9]{4}-[0-9]{2}-[0-9]{2}T[0-9]{2}:[0-9]{2}:[0-9]{2}(?:\\.[0-9]{1,7})?(?:Z|[+-][0-9]{2}:[0-9]{2})$",
        RegexOptions.CultureInvariant)]
    private static partial Regex ExplicitOffsetDateTimePattern();

    private sealed class ValidationRules
    {
        public int? MinLength { get; set; }

        public int? MaxLength { get; set; }

        public string? Pattern { get; set; }

        public decimal? NumericMinimum { get; set; }

        public decimal? NumericMaximum { get; set; }

        public int? DecimalPlaces { get; set; }

        public DateOnly? DateMinimum { get; set; }

        public DateOnly? DateMaximum { get; set; }

        public DateTimeOffset? DateTimeMinimum { get; set; }

        public DateTimeOffset? DateTimeMaximum { get; set; }

        public int? MinimumSelections { get; set; }

        public int? MaximumSelections { get; set; }

        public int? MaximumDocuments { get; set; }
    }
}

internal sealed record RequestTypeCreateValues(
    string Code,
    RequestTypeVersionValues Version);

internal sealed record RequestTypeVersionValues(
    string NameEnglish,
    string NameArabic,
    string DescriptionEnglish,
    string DescriptionArabic,
    string RequestPrefix,
    string NavigationSlug,
    int NavigationOrder);

internal sealed record RequestFieldValues(
    string Key,
    string LabelEnglish,
    string LabelArabic,
    string? HelpTextEnglish,
    string? HelpTextArabic,
    RequestFieldType FieldType,
    bool IsRequired,
    string? DefaultValueJson,
    string? ValidationConfigurationJson,
    string? ChoiceConfigurationJson,
    int SortOrder,
    bool IsActive,
    DocumentFieldMode? DocumentMode);
