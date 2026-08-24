using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace IndependentApproval.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddApplicationUsersAndRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ApplicationUsers",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AdSid = table.Column<byte[]>(type: "varbinary(68)", nullable: false),
                    AdObjectGuid = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AccountName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Domain = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    SamAccountName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    UserPrincipalName = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: true),
                    NormalizedAccountName = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsLocked = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    LockReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsRemoved = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedByAccount = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ModifiedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ModifiedByAccount = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LockedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockedByAccount = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    LockedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RemovedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RemovedByAccount = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    RemovedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastSuccessfulAccessAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationUsers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApplicationUsers_ApplicationUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalSchema: "app",
                        principalTable: "ApplicationUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ApplicationUsers_ApplicationUsers_LockedByUserId",
                        column: x => x.LockedByUserId,
                        principalSchema: "app",
                        principalTable: "ApplicationUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ApplicationUsers_ApplicationUsers_ModifiedByUserId",
                        column: x => x.ModifiedByUserId,
                        principalSchema: "app",
                        principalTable: "ApplicationUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ApplicationUsers_ApplicationUsers_RemovedByUserId",
                        column: x => x.RemovedByUserId,
                        principalSchema: "app",
                        principalTable: "ApplicationUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Permissions",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NameEnglish = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameArabic = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DescriptionEnglish = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    DescriptionArabic = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ApplicationRoles",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NormalizedCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NameEnglish = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameArabic = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DescriptionEnglish = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    DescriptionArabic = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
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
                    table.PrimaryKey("PK_ApplicationRoles", x => x.Id);
                    table.CheckConstraint("CK_ApplicationRoles_ArchivedInactive", "[IsArchived] = 0 OR [IsActive] = 0");
                    table.ForeignKey(
                        name: "FK_ApplicationRoles_ApplicationUsers_ArchivedByUserId",
                        column: x => x.ArchivedByUserId,
                        principalSchema: "app",
                        principalTable: "ApplicationUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ApplicationRoles_ApplicationUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalSchema: "app",
                        principalTable: "ApplicationUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ApplicationRoles_ApplicationUsers_ModifiedByUserId",
                        column: x => x.ModifiedByUserId,
                        principalSchema: "app",
                        principalTable: "ApplicationUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ApplicationUserRoles",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApplicationUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApplicationRoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssignedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    AssignedByAccount = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    AssignedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RemovedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RemovedByAccount = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    RemovedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationUserRoles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApplicationUserRoles_ApplicationRoles_ApplicationRoleId",
                        column: x => x.ApplicationRoleId,
                        principalSchema: "app",
                        principalTable: "ApplicationRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ApplicationUserRoles_ApplicationUsers_ApplicationUserId",
                        column: x => x.ApplicationUserId,
                        principalSchema: "app",
                        principalTable: "ApplicationUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ApplicationUserRoles_ApplicationUsers_AssignedByUserId",
                        column: x => x.AssignedByUserId,
                        principalSchema: "app",
                        principalTable: "ApplicationUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ApplicationUserRoles_ApplicationUsers_RemovedByUserId",
                        column: x => x.RemovedByUserId,
                        principalSchema: "app",
                        principalTable: "ApplicationUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RolePermissions",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApplicationRoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PermissionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GrantedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    GrantedByAccount = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    GrantedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RemovedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RemovedByAccount = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    RemovedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolePermissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RolePermissions_ApplicationRoles_ApplicationRoleId",
                        column: x => x.ApplicationRoleId,
                        principalSchema: "app",
                        principalTable: "ApplicationRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RolePermissions_ApplicationUsers_GrantedByUserId",
                        column: x => x.GrantedByUserId,
                        principalSchema: "app",
                        principalTable: "ApplicationUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RolePermissions_ApplicationUsers_RemovedByUserId",
                        column: x => x.RemovedByUserId,
                        principalSchema: "app",
                        principalTable: "ApplicationUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RolePermissions_Permissions_PermissionId",
                        column: x => x.PermissionId,
                        principalSchema: "app",
                        principalTable: "Permissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "app",
                table: "Permissions",
                columns: new[] { "Id", "Code", "DescriptionArabic", "DescriptionEnglish", "NameArabic", "NameEnglish" },
                values: new object[,]
                {
                    { new Guid("a8fcb6c1-8b0a-4ee5-a0cf-726df70a1001"), "APPLICATION.ACCESS", "الوصول إلى التطبيق بعد مصادقة Windows.", "Access the application after Windows authentication.", "الوصول إلى التطبيق", "Access application" },
                    { new Guid("a8fcb6c1-8b0a-4ee5-a0cf-726df70a1002"), "REQUESTS.START", "إنشاء الطلبات وإرسالها ضمن سير عمل مسموح.", "Create and submit requests for an allowed workflow.", "بدء الطلبات", "Start requests" },
                    { new Guid("a8fcb6c1-8b0a-4ee5-a0cf-726df70a1003"), "TASKS.PROCESS_ASSIGNED", "معالجة مهام سير العمل المسندة إلى أدوار المستخدم.", "Process workflow tasks assigned to the user's roles.", "معالجة المهام المسندة", "Process assigned tasks" },
                    { new Guid("a8fcb6c1-8b0a-4ee5-a0cf-726df70a1004"), "REQUESTS.VIEW_ALLOWED", "عرض الطلبات التي تسمح بها قواعد سير العمل والأدوار.", "View requests allowed by workflow and role rules.", "عرض الطلبات المسموح بها", "View allowed requests" },
                    { new Guid("a8fcb6c1-8b0a-4ee5-a0cf-726df70a1005"), "DOCUMENTS.MANAGE", "رفع المستندات وفتحها وإدارتها عندما تسمح قواعد سير العمل بذلك.", "Upload, open and manage documents when workflow rules allow it.", "إدارة المستندات", "Manage documents" },
                    { new Guid("a8fcb6c1-8b0a-4ee5-a0cf-726df70a1006"), "ADMINISTRATION.VIEW", "عرض معلومات الإدارة دون منح صلاحية مسؤول النظام.", "View administration information without granting system administrator access.", "عرض الإدارة", "View administration" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationRoles_ArchivedByUserId",
                schema: "app",
                table: "ApplicationRoles",
                column: "ArchivedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationRoles_CreatedByUserId",
                schema: "app",
                table: "ApplicationRoles",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationRoles_IsArchived_IsActive",
                schema: "app",
                table: "ApplicationRoles",
                columns: new[] { "IsArchived", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationRoles_ModifiedByUserId",
                schema: "app",
                table: "ApplicationRoles",
                column: "ModifiedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationRoles_NormalizedCode",
                schema: "app",
                table: "ApplicationRoles",
                column: "NormalizedCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationUserRoles_ApplicationRoleId_RemovedAtUtc",
                schema: "app",
                table: "ApplicationUserRoles",
                columns: new[] { "ApplicationRoleId", "RemovedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationUserRoles_ApplicationUserId_ApplicationRoleId",
                schema: "app",
                table: "ApplicationUserRoles",
                columns: new[] { "ApplicationUserId", "ApplicationRoleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationUserRoles_AssignedByUserId",
                schema: "app",
                table: "ApplicationUserRoles",
                column: "AssignedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationUserRoles_RemovedByUserId",
                schema: "app",
                table: "ApplicationUserRoles",
                column: "RemovedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationUsers_AdObjectGuid",
                schema: "app",
                table: "ApplicationUsers",
                column: "AdObjectGuid",
                unique: true,
                filter: "[AdObjectGuid] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationUsers_AdSid",
                schema: "app",
                table: "ApplicationUsers",
                column: "AdSid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationUsers_CreatedByUserId",
                schema: "app",
                table: "ApplicationUsers",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationUsers_IsRemoved_IsActive_IsLocked",
                schema: "app",
                table: "ApplicationUsers",
                columns: new[] { "IsRemoved", "IsActive", "IsLocked" });

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationUsers_LockedByUserId",
                schema: "app",
                table: "ApplicationUsers",
                column: "LockedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationUsers_ModifiedByUserId",
                schema: "app",
                table: "ApplicationUsers",
                column: "ModifiedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationUsers_NormalizedAccountName",
                schema: "app",
                table: "ApplicationUsers",
                column: "NormalizedAccountName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationUsers_RemovedByUserId",
                schema: "app",
                table: "ApplicationUsers",
                column: "RemovedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_Code",
                schema: "app",
                table: "Permissions",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_ApplicationRoleId_PermissionId",
                schema: "app",
                table: "RolePermissions",
                columns: new[] { "ApplicationRoleId", "PermissionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_GrantedByUserId",
                schema: "app",
                table: "RolePermissions",
                column: "GrantedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_PermissionId_RemovedAtUtc",
                schema: "app",
                table: "RolePermissions",
                columns: new[] { "PermissionId", "RemovedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_RemovedByUserId",
                schema: "app",
                table: "RolePermissions",
                column: "RemovedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApplicationUserRoles",
                schema: "app");

            migrationBuilder.DropTable(
                name: "RolePermissions",
                schema: "app");

            migrationBuilder.DropTable(
                name: "ApplicationRoles",
                schema: "app");

            migrationBuilder.DropTable(
                name: "Permissions",
                schema: "app");

            migrationBuilder.DropTable(
                name: "ApplicationUsers",
                schema: "app");
        }
    }
}
