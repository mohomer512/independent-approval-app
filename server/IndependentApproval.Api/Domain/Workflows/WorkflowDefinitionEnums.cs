namespace IndependentApproval.Api.Domain.Workflows;

public enum WorkflowVersionLifecycle
{
    Draft = 1,
    Published = 2,
    Archived = 3
}

public enum WorkflowCommentPolicy
{
    None = 1,
    Optional = 2,
    Required = 3
}

public enum WorkflowActionType
{
    Submit = 1,
    Approve = 2,
    Reject = 3,
    PutOnHold = 4,
    Resume = 5,
    RequestMoreInformation = 6,
    Return = 7,
    Forward = 8,
    Complete = 9
}

public enum WorkflowTerminalOutcome
{
    Approved = 1,
    Rejected = 2,
    Completed = 3
}

public enum WorkflowFieldAccess
{
    Hidden = 1,
    ReadOnly = 2,
    Editable = 3,
    EditableRequired = 4
}

public enum WorkflowDocumentAccess
{
    Hidden = 1,
    View = 2,
    Edit = 3
}
