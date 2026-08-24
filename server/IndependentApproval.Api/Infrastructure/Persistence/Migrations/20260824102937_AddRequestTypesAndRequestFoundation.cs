using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IndependentApproval.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRequestTypesAndRequestFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RequestNumberSequences",
                schema: "app",
                columns: table => new
                {
                    NormalizedPrefix = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    NextValue = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequestNumberSequences", x => new { x.NormalizedPrefix, x.Year });
                    table.CheckConstraint("CK_RequestNumberSequences_NextValue_Positive", "[NextValue] > 0");
                    table.CheckConstraint("CK_RequestNumberSequences_NormalizedPrefix_Valid", "LEN([NormalizedPrefix]) > 0 AND [NormalizedPrefix] COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^A-Z0-9-]%' COLLATE Latin1_General_100_BIN2");
                    table.CheckConstraint("CK_RequestNumberSequences_Year_Valid", "[Year] BETWEEN 1 AND 9999");
                });

            migrationBuilder.CreateTable(
                name: "RequestTypes",
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
                    table.PrimaryKey("PK_RequestTypes", x => x.Id);
                    table.CheckConstraint("CK_RequestTypes_Code_NotBlank", "LEN(LTRIM(RTRIM([Code]))) > 0");
                    table.CheckConstraint("CK_RequestTypes_NormalizedCode_NotBlank", "LEN(LTRIM(RTRIM([NormalizedCode]))) > 0");
                    table.ForeignKey(
                        name: "FK_RequestTypes_ApplicationUsers_ArchivedByUserId",
                        column: x => x.ArchivedByUserId,
                        principalSchema: "app",
                        principalTable: "ApplicationUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RequestTypes_ApplicationUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalSchema: "app",
                        principalTable: "ApplicationUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RequestTypes_ApplicationUsers_ModifiedByUserId",
                        column: x => x.ModifiedByUserId,
                        principalSchema: "app",
                        principalTable: "ApplicationUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RequestTypeVersions",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VersionNumber = table.Column<int>(type: "int", nullable: false),
                    NameEnglish = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameArabic = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DescriptionEnglish = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    DescriptionArabic = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    RequestPrefix = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    NavigationSlug = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
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
                    table.PrimaryKey("PK_RequestTypeVersions", x => x.Id);
                    table.UniqueConstraint("AK_RequestTypeVersions_Id_RequestTypeId", x => new { x.Id, x.RequestTypeId });
                    table.CheckConstraint("CK_RequestTypeVersions_ArchivedAudit", "[Lifecycle] <> 'Archived' OR [ArchivedAtUtc] IS NOT NULL");
                    table.CheckConstraint("CK_RequestTypeVersions_Lifecycle_Valid", "[Lifecycle] IN ('Draft', 'Published', 'Archived')");
                    table.CheckConstraint("CK_RequestTypeVersions_NavigationOrder_NonNegative", "[NavigationOrder] >= 0");
                    table.CheckConstraint("CK_RequestTypeVersions_PublishedAudit", "[Lifecycle] <> 'Published' OR [PublishedAtUtc] IS NOT NULL");
                    table.CheckConstraint("CK_RequestTypeVersions_VersionNumber_Positive", "[VersionNumber] > 0");
                    table.ForeignKey(
                        name: "FK_RequestTypeVersions_ApplicationUsers_ArchivedByUserId",
                        column: x => x.ArchivedByUserId,
                        principalSchema: "app",
                        principalTable: "ApplicationUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RequestTypeVersions_ApplicationUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalSchema: "app",
                        principalTable: "ApplicationUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RequestTypeVersions_ApplicationUsers_ModifiedByUserId",
                        column: x => x.ModifiedByUserId,
                        principalSchema: "app",
                        principalTable: "ApplicationUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RequestTypeVersions_ApplicationUsers_PublishedByUserId",
                        column: x => x.PublishedByUserId,
                        principalSchema: "app",
                        principalTable: "ApplicationUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RequestTypeVersions_RequestTypes_RequestTypeId",
                        column: x => x.RequestTypeId,
                        principalSchema: "app",
                        principalTable: "RequestTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ApprovalRequests",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestNumber = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    RequestTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestTypeVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkflowDefinitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkflowVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CurrentWorkflowStepId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RequestedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    ModifiedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    SubmittedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovalRequests", x => x.Id);
                    table.UniqueConstraint("AK_ApprovalRequests_Id_RequestTypeVersionId", x => new { x.Id, x.RequestTypeVersionId });
                    table.CheckConstraint("CK_ApprovalRequests_CompletedAtUtc_Chronology", "[CompletedAtUtc] IS NULL OR [CompletedAtUtc] >= [CreatedAtUtc]");
                    table.CheckConstraint("CK_ApprovalRequests_CurrentWorkflowStepId_NotEmpty", "[CurrentWorkflowStepId] IS NULL OR [CurrentWorkflowStepId] <> '00000000-0000-0000-0000-000000000000'");
                    table.CheckConstraint("CK_ApprovalRequests_ModifiedAtUtc_Chronology", "[ModifiedAtUtc] IS NULL OR [ModifiedAtUtc] >= [CreatedAtUtc]");
                    table.CheckConstraint("CK_ApprovalRequests_Status_Valid", "[Status] IN ('Draft', 'Submitted', 'InProgress', 'OnHold', 'MoreInformationRequired', 'Approved', 'Rejected', 'Completed', 'Cancelled')");
                    table.CheckConstraint("CK_ApprovalRequests_SubmittedAtUtc_Chronology", "[SubmittedAtUtc] IS NULL OR [SubmittedAtUtc] >= [CreatedAtUtc]");
                    table.CheckConstraint("CK_ApprovalRequests_WorkflowDefinitionId_NotEmpty", "[WorkflowDefinitionId] <> '00000000-0000-0000-0000-000000000000'");
                    table.CheckConstraint("CK_ApprovalRequests_WorkflowVersionId_NotEmpty", "[WorkflowVersionId] <> '00000000-0000-0000-0000-000000000000'");
                    table.ForeignKey(
                        name: "FK_ApprovalRequests_ApplicationUsers_RequestedByUserId",
                        column: x => x.RequestedByUserId,
                        principalSchema: "app",
                        principalTable: "ApplicationUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ApprovalRequests_RequestTypeVersions_RequestTypeVersionId_RequestTypeId",
                        columns: x => new { x.RequestTypeVersionId, x.RequestTypeId },
                        principalSchema: "app",
                        principalTable: "RequestTypeVersions",
                        principalColumns: new[] { "Id", "RequestTypeId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ApprovalRequests_RequestTypes_RequestTypeId",
                        column: x => x.RequestTypeId,
                        principalSchema: "app",
                        principalTable: "RequestTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RequestFieldDefinitions",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestTypeVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Key = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NormalizedKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LabelEnglish = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    LabelArabic = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    HelpTextEnglish = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    HelpTextArabic = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    FieldType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    DefaultValueJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ValidationConfigurationJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ChoiceConfigurationJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    DocumentMode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
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
                    table.PrimaryKey("PK_RequestFieldDefinitions", x => x.Id);
                    table.UniqueConstraint("AK_RequestFieldDefinitions_Id_RequestTypeVersionId", x => new { x.Id, x.RequestTypeVersionId });
                    table.CheckConstraint("CK_RequestFieldDefinitions_ChoiceConfigurationJson_Valid", "[ChoiceConfigurationJson] IS NULL OR ISJSON([ChoiceConfigurationJson]) = 1");
                    table.CheckConstraint("CK_RequestFieldDefinitions_DefaultValueJson_Valid", "[DefaultValueJson] IS NULL OR ISJSON(CONCAT('[', [DefaultValueJson], ']')) = 1");
                    table.CheckConstraint("CK_RequestFieldDefinitions_DocumentMode_FieldType", "(([FieldType] IN ('FileDocument', 'RichDocument') AND [DocumentMode] IS NOT NULL) OR ([FieldType] NOT IN ('FileDocument', 'RichDocument') AND [DocumentMode] IS NULL))");
                    table.CheckConstraint("CK_RequestFieldDefinitions_DocumentMode_Valid", "[DocumentMode] IS NULL OR [DocumentMode] IN ('UploadOnly', 'CreateInEditorOnly', 'UploadOrCreate')");
                    table.CheckConstraint("CK_RequestFieldDefinitions_FieldType_Valid", "[FieldType] IN ('ShortText', 'LongText', 'Integer', 'Decimal', 'Date', 'DateTime', 'YesNo', 'SingleChoice', 'MultipleChoice', 'ActiveDirectoryUser', 'ApplicationRole', 'FileDocument', 'RichDocument')");
                    table.CheckConstraint("CK_RequestFieldDefinitions_SortOrder_NonNegative", "[SortOrder] >= 0");
                    table.CheckConstraint("CK_RequestFieldDefinitions_ValidationConfigurationJson_Valid", "[ValidationConfigurationJson] IS NULL OR ISJSON([ValidationConfigurationJson]) = 1");
                    table.ForeignKey(
                        name: "FK_RequestFieldDefinitions_ApplicationUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalSchema: "app",
                        principalTable: "ApplicationUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RequestFieldDefinitions_ApplicationUsers_ModifiedByUserId",
                        column: x => x.ModifiedByUserId,
                        principalSchema: "app",
                        principalTable: "ApplicationUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RequestFieldDefinitions_RequestTypeVersions_RequestTypeVersionId",
                        column: x => x.RequestTypeVersionId,
                        principalSchema: "app",
                        principalTable: "RequestTypeVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RequestContentValues",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApprovalRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestTypeVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestFieldDefinitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ValueJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedByApplicationUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModifiedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ModifiedByApplicationUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequestContentValues", x => x.Id);
                    table.CheckConstraint("CK_RequestContentValues_ValueJson_Valid", "ISJSON(CONCAT('[', [ValueJson], ']')) = 1");
                    table.ForeignKey(
                        name: "FK_RequestContentValues_ApplicationUsers_CreatedByApplicationUserId",
                        column: x => x.CreatedByApplicationUserId,
                        principalSchema: "app",
                        principalTable: "ApplicationUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RequestContentValues_ApplicationUsers_ModifiedByApplicationUserId",
                        column: x => x.ModifiedByApplicationUserId,
                        principalSchema: "app",
                        principalTable: "ApplicationUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RequestContentValues_ApprovalRequests_ApprovalRequestId_RequestTypeVersionId",
                        columns: x => new { x.ApprovalRequestId, x.RequestTypeVersionId },
                        principalSchema: "app",
                        principalTable: "ApprovalRequests",
                        principalColumns: new[] { "Id", "RequestTypeVersionId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RequestContentValues_RequestFieldDefinitions_RequestFieldDefinitionId_RequestTypeVersionId",
                        columns: x => new { x.RequestFieldDefinitionId, x.RequestTypeVersionId },
                        principalSchema: "app",
                        principalTable: "RequestFieldDefinitions",
                        principalColumns: new[] { "Id", "RequestTypeVersionId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RequestDocuments",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApprovalRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestTypeVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestFieldDefinitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    AttachedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    AttachedByApplicationUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequestDocuments", x => x.Id);
                    table.CheckConstraint("CK_RequestDocuments_SortOrder_NonNegative", "[SortOrder] >= 0");
                    table.ForeignKey(
                        name: "FK_RequestDocuments_ApplicationUsers_AttachedByApplicationUserId",
                        column: x => x.AttachedByApplicationUserId,
                        principalSchema: "app",
                        principalTable: "ApplicationUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RequestDocuments_ApprovalRequests_ApprovalRequestId_RequestTypeVersionId",
                        columns: x => new { x.ApprovalRequestId, x.RequestTypeVersionId },
                        principalSchema: "app",
                        principalTable: "ApprovalRequests",
                        principalColumns: new[] { "Id", "RequestTypeVersionId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RequestDocuments_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalSchema: "app",
                        principalTable: "Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RequestDocuments_RequestFieldDefinitions_RequestFieldDefinitionId_RequestTypeVersionId",
                        columns: x => new { x.RequestFieldDefinitionId, x.RequestTypeVersionId },
                        principalSchema: "app",
                        principalTable: "RequestFieldDefinitions",
                        principalColumns: new[] { "Id", "RequestTypeVersionId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RequestRichDocumentRevisions",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApprovalRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestTypeVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestFieldDefinitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RevisionNumber = table.Column<int>(type: "int", nullable: false),
                    ContentHtml = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContentSha256 = table.Column<string>(type: "char(64)", nullable: false),
                    ContentLength = table.Column<int>(type: "int", nullable: false),
                    SanitizerVersion = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedByApplicationUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequestRichDocumentRevisions", x => x.Id);
                    table.CheckConstraint("CK_RequestRichDocumentRevisions_ContentLength_NonNegative", "[ContentLength] >= 0");
                    table.CheckConstraint("CK_RequestRichDocumentRevisions_ContentSha256_Valid", "LEN([ContentSha256]) = 64 AND [ContentSha256] NOT LIKE '%[^0-9A-Fa-f]%'");
                    table.CheckConstraint("CK_RequestRichDocumentRevisions_RevisionNumber_Positive", "[RevisionNumber] > 0");
                    table.ForeignKey(
                        name: "FK_RequestRichDocumentRevisions_ApplicationUsers_CreatedByApplicationUserId",
                        column: x => x.CreatedByApplicationUserId,
                        principalSchema: "app",
                        principalTable: "ApplicationUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RequestRichDocumentRevisions_ApprovalRequests_ApprovalRequestId_RequestTypeVersionId",
                        columns: x => new { x.ApprovalRequestId, x.RequestTypeVersionId },
                        principalSchema: "app",
                        principalTable: "ApprovalRequests",
                        principalColumns: new[] { "Id", "RequestTypeVersionId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RequestRichDocumentRevisions_RequestFieldDefinitions_RequestFieldDefinitionId_RequestTypeVersionId",
                        columns: x => new { x.RequestFieldDefinitionId, x.RequestTypeVersionId },
                        principalSchema: "app",
                        principalTable: "RequestFieldDefinitions",
                        principalColumns: new[] { "Id", "RequestTypeVersionId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalRequests_CurrentWorkflowStepId",
                schema: "app",
                table: "ApprovalRequests",
                column: "CurrentWorkflowStepId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalRequests_RequestedByUserId_Status_ModifiedAtUtc",
                schema: "app",
                table: "ApprovalRequests",
                columns: new[] { "RequestedByUserId", "Status", "ModifiedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalRequests_RequestNumber",
                schema: "app",
                table: "ApprovalRequests",
                column: "RequestNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalRequests_RequestTypeId",
                schema: "app",
                table: "ApprovalRequests",
                column: "RequestTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalRequests_RequestTypeVersionId_RequestTypeId",
                schema: "app",
                table: "ApprovalRequests",
                columns: new[] { "RequestTypeVersionId", "RequestTypeId" });

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalRequests_RequestTypeVersionId_Status",
                schema: "app",
                table: "ApprovalRequests",
                columns: new[] { "RequestTypeVersionId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalRequests_WorkflowVersionId_Status",
                schema: "app",
                table: "ApprovalRequests",
                columns: new[] { "WorkflowVersionId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_RequestContentValues_ApprovalRequestId_RequestFieldDefinitionId",
                schema: "app",
                table: "RequestContentValues",
                columns: new[] { "ApprovalRequestId", "RequestFieldDefinitionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RequestContentValues_ApprovalRequestId_RequestTypeVersionId",
                schema: "app",
                table: "RequestContentValues",
                columns: new[] { "ApprovalRequestId", "RequestTypeVersionId" });

            migrationBuilder.CreateIndex(
                name: "IX_RequestContentValues_CreatedByApplicationUserId",
                schema: "app",
                table: "RequestContentValues",
                column: "CreatedByApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RequestContentValues_ModifiedByApplicationUserId",
                schema: "app",
                table: "RequestContentValues",
                column: "ModifiedByApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RequestContentValues_RequestFieldDefinitionId",
                schema: "app",
                table: "RequestContentValues",
                column: "RequestFieldDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_RequestContentValues_RequestFieldDefinitionId_RequestTypeVersionId",
                schema: "app",
                table: "RequestContentValues",
                columns: new[] { "RequestFieldDefinitionId", "RequestTypeVersionId" });

            migrationBuilder.CreateIndex(
                name: "IX_RequestDocuments_ApprovalRequestId_RequestFieldDefinitionId_DocumentId",
                schema: "app",
                table: "RequestDocuments",
                columns: new[] { "ApprovalRequestId", "RequestFieldDefinitionId", "DocumentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RequestDocuments_ApprovalRequestId_RequestFieldDefinitionId_SortOrder",
                schema: "app",
                table: "RequestDocuments",
                columns: new[] { "ApprovalRequestId", "RequestFieldDefinitionId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_RequestDocuments_ApprovalRequestId_RequestTypeVersionId",
                schema: "app",
                table: "RequestDocuments",
                columns: new[] { "ApprovalRequestId", "RequestTypeVersionId" });

            migrationBuilder.CreateIndex(
                name: "IX_RequestDocuments_AttachedByApplicationUserId",
                schema: "app",
                table: "RequestDocuments",
                column: "AttachedByApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RequestDocuments_DocumentId",
                schema: "app",
                table: "RequestDocuments",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_RequestDocuments_RequestFieldDefinitionId_RequestTypeVersionId",
                schema: "app",
                table: "RequestDocuments",
                columns: new[] { "RequestFieldDefinitionId", "RequestTypeVersionId" });

            migrationBuilder.CreateIndex(
                name: "IX_RequestFieldDefinitions_CreatedByUserId",
                schema: "app",
                table: "RequestFieldDefinitions",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RequestFieldDefinitions_ModifiedByUserId",
                schema: "app",
                table: "RequestFieldDefinitions",
                column: "ModifiedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RequestFieldDefinitions_RequestTypeVersionId_IsActive",
                schema: "app",
                table: "RequestFieldDefinitions",
                columns: new[] { "RequestTypeVersionId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_RequestFieldDefinitions_RequestTypeVersionId_NormalizedKey",
                schema: "app",
                table: "RequestFieldDefinitions",
                columns: new[] { "RequestTypeVersionId", "NormalizedKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RequestFieldDefinitions_RequestTypeVersionId_SortOrder",
                schema: "app",
                table: "RequestFieldDefinitions",
                columns: new[] { "RequestTypeVersionId", "SortOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RequestRichDocumentRevisions_ApprovalRequestId_RequestFieldDefinitionId_RevisionNumber",
                schema: "app",
                table: "RequestRichDocumentRevisions",
                columns: new[] { "ApprovalRequestId", "RequestFieldDefinitionId", "RevisionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RequestRichDocumentRevisions_ApprovalRequestId_RequestTypeVersionId",
                schema: "app",
                table: "RequestRichDocumentRevisions",
                columns: new[] { "ApprovalRequestId", "RequestTypeVersionId" });

            migrationBuilder.CreateIndex(
                name: "IX_RequestRichDocumentRevisions_CreatedByApplicationUserId",
                schema: "app",
                table: "RequestRichDocumentRevisions",
                column: "CreatedByApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RequestRichDocumentRevisions_RequestFieldDefinitionId",
                schema: "app",
                table: "RequestRichDocumentRevisions",
                column: "RequestFieldDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_RequestRichDocumentRevisions_RequestFieldDefinitionId_RequestTypeVersionId",
                schema: "app",
                table: "RequestRichDocumentRevisions",
                columns: new[] { "RequestFieldDefinitionId", "RequestTypeVersionId" });

            migrationBuilder.CreateIndex(
                name: "IX_RequestTypes_ArchivedByUserId",
                schema: "app",
                table: "RequestTypes",
                column: "ArchivedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RequestTypes_CreatedByUserId",
                schema: "app",
                table: "RequestTypes",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RequestTypes_IsArchived",
                schema: "app",
                table: "RequestTypes",
                column: "IsArchived");

            migrationBuilder.CreateIndex(
                name: "IX_RequestTypes_ModifiedByUserId",
                schema: "app",
                table: "RequestTypes",
                column: "ModifiedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RequestTypes_NormalizedCode",
                schema: "app",
                table: "RequestTypes",
                column: "NormalizedCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RequestTypeVersions_ArchivedByUserId",
                schema: "app",
                table: "RequestTypeVersions",
                column: "ArchivedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RequestTypeVersions_CreatedByUserId",
                schema: "app",
                table: "RequestTypeVersions",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RequestTypeVersions_Lifecycle_NavigationOrder",
                schema: "app",
                table: "RequestTypeVersions",
                columns: new[] { "Lifecycle", "NavigationOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_RequestTypeVersions_Lifecycle_NavigationSlug",
                schema: "app",
                table: "RequestTypeVersions",
                columns: new[] { "Lifecycle", "NavigationSlug" });

            migrationBuilder.CreateIndex(
                name: "IX_RequestTypeVersions_ModifiedByUserId",
                schema: "app",
                table: "RequestTypeVersions",
                column: "ModifiedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RequestTypeVersions_PublishedByUserId",
                schema: "app",
                table: "RequestTypeVersions",
                column: "PublishedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RequestTypeVersions_RequestTypeId_VersionNumber",
                schema: "app",
                table: "RequestTypeVersions",
                columns: new[] { "RequestTypeId", "VersionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_RequestTypeVersions_RequestTypeId_Draft",
                schema: "app",
                table: "RequestTypeVersions",
                column: "RequestTypeId",
                unique: true,
                filter: "[Lifecycle] = 'Draft'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RequestContentValues",
                schema: "app");

            migrationBuilder.DropTable(
                name: "RequestDocuments",
                schema: "app");

            migrationBuilder.DropTable(
                name: "RequestNumberSequences",
                schema: "app");

            migrationBuilder.DropTable(
                name: "RequestRichDocumentRevisions",
                schema: "app");

            migrationBuilder.DropTable(
                name: "ApprovalRequests",
                schema: "app");

            migrationBuilder.DropTable(
                name: "RequestFieldDefinitions",
                schema: "app");

            migrationBuilder.DropTable(
                name: "RequestTypeVersions",
                schema: "app");

            migrationBuilder.DropTable(
                name: "RequestTypes",
                schema: "app");
        }
    }
}
