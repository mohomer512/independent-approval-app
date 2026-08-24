namespace IndependentApproval.Api.Domain.Administration;

public static class PermissionCatalog
{
    public static readonly Guid AccessApplicationId =
        Guid.Parse("a8fcb6c1-8b0a-4ee5-a0cf-726df70a1001");

    public static readonly Guid StartRequestsId =
        Guid.Parse("a8fcb6c1-8b0a-4ee5-a0cf-726df70a1002");

    public static readonly Guid ProcessAssignedTasksId =
        Guid.Parse("a8fcb6c1-8b0a-4ee5-a0cf-726df70a1003");

    public static readonly Guid ViewAllowedRequestsId =
        Guid.Parse("a8fcb6c1-8b0a-4ee5-a0cf-726df70a1004");

    public static readonly Guid ManageDocumentsId =
        Guid.Parse("a8fcb6c1-8b0a-4ee5-a0cf-726df70a1005");

    public static readonly Guid ViewAdministrationId =
        Guid.Parse("a8fcb6c1-8b0a-4ee5-a0cf-726df70a1006");

    public static IReadOnlyList<PermissionDefinition> All { get; } =
    [
        new(
            AccessApplicationId,
            PermissionCodes.AccessApplication,
            "Access application",
            "الوصول إلى التطبيق",
            "Access the application after Windows authentication.",
            "الوصول إلى التطبيق بعد مصادقة Windows."),
        new(
            StartRequestsId,
            PermissionCodes.StartRequests,
            "Start requests",
            "بدء الطلبات",
            "Create and submit requests for an allowed workflow.",
            "إنشاء الطلبات وإرسالها ضمن سير عمل مسموح."),
        new(
            ProcessAssignedTasksId,
            PermissionCodes.ProcessAssignedTasks,
            "Process assigned tasks",
            "معالجة المهام المسندة",
            "Process workflow tasks assigned to the user's roles.",
            "معالجة مهام سير العمل المسندة إلى أدوار المستخدم."),
        new(
            ViewAllowedRequestsId,
            PermissionCodes.ViewAllowedRequests,
            "View allowed requests",
            "عرض الطلبات المسموح بها",
            "View requests allowed by workflow and role rules.",
            "عرض الطلبات التي تسمح بها قواعد سير العمل والأدوار."),
        new(
            ManageDocumentsId,
            PermissionCodes.ManageDocuments,
            "Manage documents",
            "إدارة المستندات",
            "Upload, open and manage documents when workflow rules allow it.",
            "رفع المستندات وفتحها وإدارتها عندما تسمح قواعد سير العمل بذلك."),
        new(
            ViewAdministrationId,
            PermissionCodes.ViewAdministration,
            "View administration",
            "عرض الإدارة",
            "View administration information without granting system administrator access.",
            "عرض معلومات الإدارة دون منح صلاحية مسؤول النظام.")
    ];
}

public sealed record PermissionDefinition(
    Guid Id,
    string Code,
    string NameEnglish,
    string NameArabic,
    string DescriptionEnglish,
    string DescriptionArabic);
