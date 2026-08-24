namespace IndependentApproval.Api.Domain.Requests;

public enum RequestTypeVersionLifecycle
{
    Draft = 1,
    Published = 2,
    Archived = 3
}

public enum RequestFieldType
{
    ShortText = 1,
    LongText = 2,
    Integer = 3,
    Decimal = 4,
    Date = 5,
    DateTime = 6,
    YesNo = 7,
    SingleChoice = 8,
    MultipleChoice = 9,
    ActiveDirectoryUser = 10,
    ApplicationRole = 11,
    FileDocument = 12,
    RichDocument = 13
}

public enum DocumentFieldMode
{
    UploadOnly = 1,
    CreateInEditorOnly = 2,
    UploadOrCreate = 3
}

public enum ApprovalRequestStatus
{
    Draft = 1,
    Submitted = 2,
    InProgress = 3,
    OnHold = 4,
    MoreInformationRequired = 5,
    Approved = 6,
    Rejected = 7,
    Completed = 8,
    Cancelled = 9
}
