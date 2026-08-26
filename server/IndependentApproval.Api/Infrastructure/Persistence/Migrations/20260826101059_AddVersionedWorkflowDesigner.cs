using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IndependentApproval.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddVersionedWorkflowDesigner : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WorkflowDefinitions",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NormalizedCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsArchived = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedByAccount = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ModifiedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ModifiedByAccount = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ArchivedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ArchivedByAccount = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ArchivedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowDefinitions", x => x.Id);
                    table.CheckConstraint("CK_WorkflowDefinitions_ArchivedAudit", "[IsArchived] = 0 OR [ArchivedAtUtc] IS NOT NULL");
                    table.CheckConstraint("CK_WorkflowDefinitions_Code_NotBlank", "LEN(LTRIM(RTRIM([Code]))) > 0");
                    table.CheckConstraint("CK_WorkflowDefinitions_NormalizedCode_NotBlank", "LEN(LTRIM(RTRIM([NormalizedCode]))) > 0");
                    table.ForeignKey(
                        name: "FK_WorkflowDefinitions_ApplicationUsers_ArchivedByUserId",
                        column: x => x.ArchivedByUserId,
                        principalSchema: "app",
                        principalTable: "ApplicationUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkflowDefinitions_ApplicationUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalSchema: "app",
                        principalTable: "ApplicationUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkflowDefinitions_ApplicationUsers_ModifiedByUserId",
                        column: x => x.ModifiedByUserId,
                        principalSchema: "app",
                        principalTable: "ApplicationUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowSlugReservations",
                schema: "app",
                columns: table => new
                {
                    NormalizedSlug = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: false),
                    WorkflowDefinitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReservedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    ReservedByAccount = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ReservedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowSlugReservations", x => x.NormalizedSlug);
                    table.UniqueConstraint("AK_WorkflowSlugReservations_WorkflowDefinitionId_NormalizedSlug", x => new { x.WorkflowDefinitionId, x.NormalizedSlug });
                    table.CheckConstraint("CK_WorkflowSlugReservations_NormalizedSlug_Valid", "LEN([NormalizedSlug]) > 0 AND [NormalizedSlug] COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^a-z0-9-]%' COLLATE Latin1_General_100_BIN2");
                    table.ForeignKey(
                        name: "FK_WorkflowSlugReservations_ApplicationUsers_ReservedByUserId",
                        column: x => x.ReservedByUserId,
                        principalSchema: "app",
                        principalTable: "ApplicationUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkflowSlugReservations_WorkflowDefinitions_WorkflowDefinitionId",
                        column: x => x.WorkflowDefinitionId,
                        principalSchema: "app",
                        principalTable: "WorkflowDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowVersions",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkflowDefinitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestTypeVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VersionNumber = table.Column<int>(type: "int", nullable: false),
                    NameEnglish = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameArabic = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DescriptionEnglish = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    DescriptionArabic = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    NavigationLabelEnglish = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NavigationLabelArabic = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NavigationSlug = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: false),
                    NavigationOrder = table.Column<int>(type: "int", nullable: false),
                    Lifecycle = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedByAccount = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ModifiedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ModifiedByAccount = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PublishedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    PublishedByAccount = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    PublishedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ArchivedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ArchivedByAccount = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ArchivedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowVersions", x => x.Id);
                    table.UniqueConstraint("AK_WorkflowVersions_Id_RequestTypeVersionId", x => new { x.Id, x.RequestTypeVersionId });
                    table.UniqueConstraint("AK_WorkflowVersions_Id_WorkflowDefinitionId_RequestTypeVersionId", x => new { x.Id, x.WorkflowDefinitionId, x.RequestTypeVersionId });
                    table.CheckConstraint("CK_WorkflowVersions_ArchivedAudit", "[Lifecycle] <> 'Archived' OR [ArchivedAtUtc] IS NOT NULL");
                    table.CheckConstraint("CK_WorkflowVersions_Lifecycle_Valid", "[Lifecycle] IN ('Draft', 'Published', 'Archived')");
                    table.CheckConstraint("CK_WorkflowVersions_NavigationOrder_NonNegative", "[NavigationOrder] >= 0");
                    table.CheckConstraint("CK_WorkflowVersions_PublishedAudit", "[Lifecycle] <> 'Published' OR [PublishedAtUtc] IS NOT NULL");
                    table.CheckConstraint("CK_WorkflowVersions_VersionNumber_Positive", "[VersionNumber] > 0");
                    table.ForeignKey(
                        name: "FK_WorkflowVersions_ApplicationUsers_ArchivedByUserId",
                        column: x => x.ArchivedByUserId,
                        principalSchema: "app",
                        principalTable: "ApplicationUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkflowVersions_ApplicationUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalSchema: "app",
                        principalTable: "ApplicationUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkflowVersions_ApplicationUsers_ModifiedByUserId",
                        column: x => x.ModifiedByUserId,
                        principalSchema: "app",
                        principalTable: "ApplicationUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkflowVersions_ApplicationUsers_PublishedByUserId",
                        column: x => x.PublishedByUserId,
                        principalSchema: "app",
                        principalTable: "ApplicationUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkflowVersions_RequestTypeVersions_RequestTypeVersionId",
                        column: x => x.RequestTypeVersionId,
                        principalSchema: "app",
                        principalTable: "RequestTypeVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkflowVersions_WorkflowDefinitions_WorkflowDefinitionId",
                        column: x => x.WorkflowDefinitionId,
                        principalSchema: "app",
                        principalTable: "WorkflowDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkflowVersions_WorkflowSlugReservations_WorkflowDefinitionId_NavigationSlug",
                        columns: x => new { x.WorkflowDefinitionId, x.NavigationSlug },
                        principalSchema: "app",
                        principalTable: "WorkflowSlugReservations",
                        principalColumns: new[] { "WorkflowDefinitionId", "NormalizedSlug" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowSteps",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkflowVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Key = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NormalizedKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NameEnglish = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameArabic = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    DiagramX = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    DiagramY = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    IsStartStep = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CanEditRequestData = table.Column<bool>(type: "bit", nullable: false),
                    CanOpenDocuments = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CanEditDocuments = table.Column<bool>(type: "bit", nullable: false),
                    CanForwardDocuments = table.Column<bool>(type: "bit", nullable: false),
                    CommentPolicy = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedByAccount = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ModifiedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ModifiedByAccount = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowSteps", x => x.Id);
                    table.UniqueConstraint("AK_WorkflowSteps_Id_WorkflowVersionId", x => new { x.Id, x.WorkflowVersionId });
                    table.CheckConstraint("CK_WorkflowSteps_CommentPolicy_Valid", "[CommentPolicy] IN ('None', 'Optional', 'Required')");
                    table.CheckConstraint("CK_WorkflowSteps_SortOrder_NonNegative", "[SortOrder] >= 0");
                    table.ForeignKey(
                        name: "FK_WorkflowSteps_ApplicationUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalSchema: "app",
                        principalTable: "ApplicationUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkflowSteps_ApplicationUsers_ModifiedByUserId",
                        column: x => x.ModifiedByUserId,
                        principalSchema: "app",
                        principalTable: "ApplicationUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkflowSteps_WorkflowVersions_WorkflowVersionId",
                        column: x => x.WorkflowVersionId,
                        principalSchema: "app",
                        principalTable: "WorkflowVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowVersionStarterRoles",
                schema: "app",
                columns: table => new
                {
                    WorkflowVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApplicationRoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowVersionStarterRoles", x => new { x.WorkflowVersionId, x.ApplicationRoleId });
                    table.ForeignKey(
                        name: "FK_WorkflowVersionStarterRoles_ApplicationRoles_ApplicationRoleId",
                        column: x => x.ApplicationRoleId,
                        principalSchema: "app",
                        principalTable: "ApplicationRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkflowVersionStarterRoles_WorkflowVersions_WorkflowVersionId",
                        column: x => x.WorkflowVersionId,
                        principalSchema: "app",
                        principalTable: "WorkflowVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowStepFieldPermissions",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkflowVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestTypeVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkflowStepId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestFieldDefinitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Access = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    DocumentAccess = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    CanSelectForForwarding = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedByAccount = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ModifiedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ModifiedByAccount = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowStepFieldPermissions", x => x.Id);
                    table.CheckConstraint("CK_WorkflowStepFieldPermissions_Access_Valid", "[Access] IN ('Hidden', 'ReadOnly', 'Editable', 'EditableRequired')");
                    table.CheckConstraint("CK_WorkflowStepFieldPermissions_DocumentAccess_Valid", "[DocumentAccess] IS NULL OR [DocumentAccess] IN ('Hidden', 'View', 'Edit')");
                    table.CheckConstraint("CK_WorkflowStepFieldPermissions_ForwardingRequiresDocumentAccess", "[CanSelectForForwarding] = 0 OR [DocumentAccess] IN ('View', 'Edit')");
                    table.ForeignKey(
                        name: "FK_WorkflowStepFieldPermissions_ApplicationUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalSchema: "app",
                        principalTable: "ApplicationUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkflowStepFieldPermissions_ApplicationUsers_ModifiedByUserId",
                        column: x => x.ModifiedByUserId,
                        principalSchema: "app",
                        principalTable: "ApplicationUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkflowStepFieldPermissions_RequestFieldDefinitions_RequestFieldDefinitionId_RequestTypeVersionId",
                        columns: x => new { x.RequestFieldDefinitionId, x.RequestTypeVersionId },
                        principalSchema: "app",
                        principalTable: "RequestFieldDefinitions",
                        principalColumns: new[] { "Id", "RequestTypeVersionId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkflowStepFieldPermissions_WorkflowSteps_WorkflowStepId_WorkflowVersionId",
                        columns: x => new { x.WorkflowStepId, x.WorkflowVersionId },
                        principalSchema: "app",
                        principalTable: "WorkflowSteps",
                        principalColumns: new[] { "Id", "WorkflowVersionId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkflowStepFieldPermissions_WorkflowVersions_WorkflowVersionId_RequestTypeVersionId",
                        columns: x => new { x.WorkflowVersionId, x.RequestTypeVersionId },
                        principalSchema: "app",
                        principalTable: "WorkflowVersions",
                        principalColumns: new[] { "Id", "RequestTypeVersionId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowStepRoles",
                schema: "app",
                columns: table => new
                {
                    WorkflowStepId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApplicationRoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkflowVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowStepRoles", x => new { x.WorkflowStepId, x.ApplicationRoleId });
                    table.ForeignKey(
                        name: "FK_WorkflowStepRoles_ApplicationRoles_ApplicationRoleId",
                        column: x => x.ApplicationRoleId,
                        principalSchema: "app",
                        principalTable: "ApplicationRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkflowStepRoles_WorkflowSteps_WorkflowStepId_WorkflowVersionId",
                        columns: x => new { x.WorkflowStepId, x.WorkflowVersionId },
                        principalSchema: "app",
                        principalTable: "WorkflowSteps",
                        principalColumns: new[] { "Id", "WorkflowVersionId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkflowStepRoles_WorkflowVersions_WorkflowVersionId",
                        column: x => x.WorkflowVersionId,
                        principalSchema: "app",
                        principalTable: "WorkflowVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowTransitions",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkflowVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Key = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NormalizedKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SourceStepId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TargetStepId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ActionLabelEnglish = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ActionLabelArabic = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ActionType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ResultingStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RequiresComment = table.Column<bool>(type: "bit", nullable: false),
                    TerminalOutcome = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedByAccount = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ModifiedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ModifiedByAccount = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowTransitions", x => x.Id);
                    table.UniqueConstraint("AK_WorkflowTransitions_Id_WorkflowVersionId", x => new { x.Id, x.WorkflowVersionId });
                    table.CheckConstraint("CK_WorkflowTransitions_ActionType_Valid", "[ActionType] IN ('Submit', 'Approve', 'Reject', 'PutOnHold', 'Resume', 'RequestMoreInformation', 'Return', 'Forward', 'Complete')");
                    table.CheckConstraint("CK_WorkflowTransitions_ResultingStatus_Valid", "[ResultingStatus] IN ('Draft', 'Submitted', 'InProgress', 'OnHold', 'MoreInformationRequired', 'Approved', 'Rejected', 'Completed', 'Cancelled')");
                    table.CheckConstraint("CK_WorkflowTransitions_SortOrder_NonNegative", "[SortOrder] >= 0");
                    table.CheckConstraint("CK_WorkflowTransitions_TargetOrTerminal", "(([TerminalOutcome] IS NULL AND [TargetStepId] IS NOT NULL) OR ([TerminalOutcome] IS NOT NULL AND [TargetStepId] IS NULL))");
                    table.CheckConstraint("CK_WorkflowTransitions_TerminalOutcome_Valid", "[TerminalOutcome] IS NULL OR [TerminalOutcome] IN ('Approved', 'Rejected', 'Completed')");
                    table.CheckConstraint("CK_WorkflowTransitions_TerminalStatus_MatchesOutcome", "[TerminalOutcome] IS NULL OR ([TerminalOutcome] = 'Approved' AND [ResultingStatus] = 'Approved') OR ([TerminalOutcome] = 'Rejected' AND [ResultingStatus] = 'Rejected') OR ([TerminalOutcome] = 'Completed' AND [ResultingStatus] = 'Completed')");
                    table.ForeignKey(
                        name: "FK_WorkflowTransitions_ApplicationUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalSchema: "app",
                        principalTable: "ApplicationUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkflowTransitions_ApplicationUsers_ModifiedByUserId",
                        column: x => x.ModifiedByUserId,
                        principalSchema: "app",
                        principalTable: "ApplicationUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkflowTransitions_WorkflowSteps_SourceStepId_WorkflowVersionId",
                        columns: x => new { x.SourceStepId, x.WorkflowVersionId },
                        principalSchema: "app",
                        principalTable: "WorkflowSteps",
                        principalColumns: new[] { "Id", "WorkflowVersionId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkflowTransitions_WorkflowSteps_TargetStepId_WorkflowVersionId",
                        columns: x => new { x.TargetStepId, x.WorkflowVersionId },
                        principalSchema: "app",
                        principalTable: "WorkflowSteps",
                        principalColumns: new[] { "Id", "WorkflowVersionId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkflowTransitions_WorkflowVersions_WorkflowVersionId",
                        column: x => x.WorkflowVersionId,
                        principalSchema: "app",
                        principalTable: "WorkflowVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalRequests_CurrentWorkflowStepId_WorkflowVersionId",
                schema: "app",
                table: "ApprovalRequests",
                columns: new[] { "CurrentWorkflowStepId", "WorkflowVersionId" });

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalRequests_WorkflowDefinitionId",
                schema: "app",
                table: "ApprovalRequests",
                column: "WorkflowDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalRequests_WorkflowVersionId_WorkflowDefinitionId_RequestTypeVersionId",
                schema: "app",
                table: "ApprovalRequests",
                columns: new[] { "WorkflowVersionId", "WorkflowDefinitionId", "RequestTypeVersionId" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowDefinitions_ArchivedByUserId",
                schema: "app",
                table: "WorkflowDefinitions",
                column: "ArchivedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowDefinitions_CreatedByUserId",
                schema: "app",
                table: "WorkflowDefinitions",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowDefinitions_IsArchived",
                schema: "app",
                table: "WorkflowDefinitions",
                column: "IsArchived");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowDefinitions_ModifiedByUserId",
                schema: "app",
                table: "WorkflowDefinitions",
                column: "ModifiedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowDefinitions_NormalizedCode",
                schema: "app",
                table: "WorkflowDefinitions",
                column: "NormalizedCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowSlugReservations_ReservedByUserId",
                schema: "app",
                table: "WorkflowSlugReservations",
                column: "ReservedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowStepFieldPermissions_CreatedByUserId",
                schema: "app",
                table: "WorkflowStepFieldPermissions",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowStepFieldPermissions_ModifiedByUserId",
                schema: "app",
                table: "WorkflowStepFieldPermissions",
                column: "ModifiedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowStepFieldPermissions_RequestFieldDefinitionId_RequestTypeVersionId",
                schema: "app",
                table: "WorkflowStepFieldPermissions",
                columns: new[] { "RequestFieldDefinitionId", "RequestTypeVersionId" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowStepFieldPermissions_WorkflowStepId_RequestFieldDefinitionId",
                schema: "app",
                table: "WorkflowStepFieldPermissions",
                columns: new[] { "WorkflowStepId", "RequestFieldDefinitionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowStepFieldPermissions_WorkflowStepId_WorkflowVersionId",
                schema: "app",
                table: "WorkflowStepFieldPermissions",
                columns: new[] { "WorkflowStepId", "WorkflowVersionId" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowStepFieldPermissions_WorkflowVersionId_RequestTypeVersionId",
                schema: "app",
                table: "WorkflowStepFieldPermissions",
                columns: new[] { "WorkflowVersionId", "RequestTypeVersionId" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowStepRoles_ApplicationRoleId",
                schema: "app",
                table: "WorkflowStepRoles",
                column: "ApplicationRoleId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowStepRoles_WorkflowStepId_WorkflowVersionId",
                schema: "app",
                table: "WorkflowStepRoles",
                columns: new[] { "WorkflowStepId", "WorkflowVersionId" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowStepRoles_WorkflowVersionId_ApplicationRoleId",
                schema: "app",
                table: "WorkflowStepRoles",
                columns: new[] { "WorkflowVersionId", "ApplicationRoleId" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowSteps_CreatedByUserId",
                schema: "app",
                table: "WorkflowSteps",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowSteps_ModifiedByUserId",
                schema: "app",
                table: "WorkflowSteps",
                column: "ModifiedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowSteps_WorkflowVersionId_IsActive",
                schema: "app",
                table: "WorkflowSteps",
                columns: new[] { "WorkflowVersionId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowSteps_WorkflowVersionId_NormalizedKey",
                schema: "app",
                table: "WorkflowSteps",
                columns: new[] { "WorkflowVersionId", "NormalizedKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowSteps_WorkflowVersionId_SortOrder",
                schema: "app",
                table: "WorkflowSteps",
                columns: new[] { "WorkflowVersionId", "SortOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_WorkflowSteps_WorkflowVersionId_StartStep",
                schema: "app",
                table: "WorkflowSteps",
                column: "WorkflowVersionId",
                unique: true,
                filter: "[IsStartStep] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowTransitions_CreatedByUserId",
                schema: "app",
                table: "WorkflowTransitions",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowTransitions_ModifiedByUserId",
                schema: "app",
                table: "WorkflowTransitions",
                column: "ModifiedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowTransitions_SourceStepId_WorkflowVersionId",
                schema: "app",
                table: "WorkflowTransitions",
                columns: new[] { "SourceStepId", "WorkflowVersionId" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowTransitions_TargetStepId_WorkflowVersionId",
                schema: "app",
                table: "WorkflowTransitions",
                columns: new[] { "TargetStepId", "WorkflowVersionId" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowTransitions_WorkflowVersionId_NormalizedKey",
                schema: "app",
                table: "WorkflowTransitions",
                columns: new[] { "WorkflowVersionId", "NormalizedKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowTransitions_WorkflowVersionId_SourceStepId_SortOrder",
                schema: "app",
                table: "WorkflowTransitions",
                columns: new[] { "WorkflowVersionId", "SourceStepId", "SortOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowTransitions_WorkflowVersionId_TargetStepId",
                schema: "app",
                table: "WorkflowTransitions",
                columns: new[] { "WorkflowVersionId", "TargetStepId" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowVersions_ArchivedByUserId",
                schema: "app",
                table: "WorkflowVersions",
                column: "ArchivedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowVersions_CreatedByUserId",
                schema: "app",
                table: "WorkflowVersions",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowVersions_Lifecycle_NavigationOrder",
                schema: "app",
                table: "WorkflowVersions",
                columns: new[] { "Lifecycle", "NavigationOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowVersions_Lifecycle_NavigationSlug",
                schema: "app",
                table: "WorkflowVersions",
                columns: new[] { "Lifecycle", "NavigationSlug" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowVersions_ModifiedByUserId",
                schema: "app",
                table: "WorkflowVersions",
                column: "ModifiedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowVersions_PublishedByUserId",
                schema: "app",
                table: "WorkflowVersions",
                column: "PublishedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowVersions_RequestTypeVersionId",
                schema: "app",
                table: "WorkflowVersions",
                column: "RequestTypeVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowVersions_WorkflowDefinitionId_NavigationSlug",
                schema: "app",
                table: "WorkflowVersions",
                columns: new[] { "WorkflowDefinitionId", "NavigationSlug" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowVersions_WorkflowDefinitionId_VersionNumber",
                schema: "app",
                table: "WorkflowVersions",
                columns: new[] { "WorkflowDefinitionId", "VersionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_WorkflowVersions_WorkflowDefinitionId_Draft",
                schema: "app",
                table: "WorkflowVersions",
                column: "WorkflowDefinitionId",
                unique: true,
                filter: "[Lifecycle] = 'Draft'");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowVersionStarterRoles_ApplicationRoleId",
                schema: "app",
                table: "WorkflowVersionStarterRoles",
                column: "ApplicationRoleId");

            migrationBuilder.AddForeignKey(
                name: "FK_ApprovalRequests_WorkflowDefinitions_WorkflowDefinitionId",
                schema: "app",
                table: "ApprovalRequests",
                column: "WorkflowDefinitionId",
                principalSchema: "app",
                principalTable: "WorkflowDefinitions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ApprovalRequests_WorkflowSteps_CurrentWorkflowStepId_WorkflowVersionId",
                schema: "app",
                table: "ApprovalRequests",
                columns: new[] { "CurrentWorkflowStepId", "WorkflowVersionId" },
                principalSchema: "app",
                principalTable: "WorkflowSteps",
                principalColumns: new[] { "Id", "WorkflowVersionId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ApprovalRequests_WorkflowVersions_WorkflowVersionId_WorkflowDefinitionId_RequestTypeVersionId",
                schema: "app",
                table: "ApprovalRequests",
                columns: new[] { "WorkflowVersionId", "WorkflowDefinitionId", "RequestTypeVersionId" },
                principalSchema: "app",
                principalTable: "WorkflowVersions",
                principalColumns: new[] { "Id", "WorkflowDefinitionId", "RequestTypeVersionId" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ApprovalRequests_WorkflowDefinitions_WorkflowDefinitionId",
                schema: "app",
                table: "ApprovalRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_ApprovalRequests_WorkflowSteps_CurrentWorkflowStepId_WorkflowVersionId",
                schema: "app",
                table: "ApprovalRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_ApprovalRequests_WorkflowVersions_WorkflowVersionId_WorkflowDefinitionId_RequestTypeVersionId",
                schema: "app",
                table: "ApprovalRequests");

            migrationBuilder.DropTable(
                name: "WorkflowStepFieldPermissions",
                schema: "app");

            migrationBuilder.DropTable(
                name: "WorkflowStepRoles",
                schema: "app");

            migrationBuilder.DropTable(
                name: "WorkflowTransitions",
                schema: "app");

            migrationBuilder.DropTable(
                name: "WorkflowVersionStarterRoles",
                schema: "app");

            migrationBuilder.DropTable(
                name: "WorkflowSteps",
                schema: "app");

            migrationBuilder.DropTable(
                name: "WorkflowVersions",
                schema: "app");

            migrationBuilder.DropTable(
                name: "WorkflowSlugReservations",
                schema: "app");

            migrationBuilder.DropTable(
                name: "WorkflowDefinitions",
                schema: "app");

            migrationBuilder.DropIndex(
                name: "IX_ApprovalRequests_CurrentWorkflowStepId_WorkflowVersionId",
                schema: "app",
                table: "ApprovalRequests");

            migrationBuilder.DropIndex(
                name: "IX_ApprovalRequests_WorkflowDefinitionId",
                schema: "app",
                table: "ApprovalRequests");

            migrationBuilder.DropIndex(
                name: "IX_ApprovalRequests_WorkflowVersionId_WorkflowDefinitionId_RequestTypeVersionId",
                schema: "app",
                table: "ApprovalRequests");
        }
    }
}
