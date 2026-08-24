using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IndependentApproval.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EnforceRequestTypeNavigationOwnership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "RequestPrefix",
                schema: "app",
                table: "RequestTypeVersions",
                type: "varchar(30)",
                unicode: false,
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(30)",
                oldMaxLength: 30);

            migrationBuilder.AlterColumn<string>(
                name: "NavigationSlug",
                schema: "app",
                table: "RequestTypeVersions",
                type: "varchar(200)",
                unicode: false,
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.CreateTable(
                name: "RequestTypePrefixReservations",
                schema: "app",
                columns: table => new
                {
                    NormalizedPrefix = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    RequestTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReservedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    ReservedByAccount = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ReservedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequestTypePrefixReservations", x => x.NormalizedPrefix);
                    table.UniqueConstraint("AK_RequestTypePrefixReservations_RequestTypeId_NormalizedPrefix", x => new { x.RequestTypeId, x.NormalizedPrefix });
                    table.CheckConstraint("CK_RequestTypePrefixReservations_NormalizedPrefix_Valid", "LEN([NormalizedPrefix]) > 0 AND [NormalizedPrefix] COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^A-Z0-9]%' COLLATE Latin1_General_100_BIN2");
                    table.ForeignKey(
                        name: "FK_RequestTypePrefixReservations_ApplicationUsers_ReservedByUserId",
                        column: x => x.ReservedByUserId,
                        principalSchema: "app",
                        principalTable: "ApplicationUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RequestTypePrefixReservations_RequestTypes_RequestTypeId",
                        column: x => x.RequestTypeId,
                        principalSchema: "app",
                        principalTable: "RequestTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RequestTypeSlugReservations",
                schema: "app",
                columns: table => new
                {
                    NormalizedSlug = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: false),
                    RequestTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReservedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    ReservedByAccount = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ReservedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequestTypeSlugReservations", x => x.NormalizedSlug);
                    table.UniqueConstraint("AK_RequestTypeSlugReservations_RequestTypeId_NormalizedSlug", x => new { x.RequestTypeId, x.NormalizedSlug });
                    table.CheckConstraint("CK_RequestTypeSlugReservations_NormalizedSlug_Valid", "LEN([NormalizedSlug]) > 0 AND [NormalizedSlug] COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^a-z0-9-]%' COLLATE Latin1_General_100_BIN2");
                    table.ForeignKey(
                        name: "FK_RequestTypeSlugReservations_ApplicationUsers_ReservedByUserId",
                        column: x => x.ReservedByUserId,
                        principalSchema: "app",
                        principalTable: "ApplicationUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RequestTypeSlugReservations_RequestTypes_RequestTypeId",
                        column: x => x.RequestTypeId,
                        principalSchema: "app",
                        principalTable: "RequestTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.Sql(
                """
                IF EXISTS
                (
                    SELECT [RequestPrefix]
                    FROM [app].[RequestTypeVersions]
                    GROUP BY [RequestPrefix]
                    HAVING COUNT(DISTINCT [RequestTypeId]) > 1
                )
                BEGIN
                    THROW 51001, 'Existing request-type prefixes have conflicting owners. Resolve the duplicate ownership before applying this migration.', 1;
                END;

                IF EXISTS
                (
                    SELECT [NavigationSlug]
                    FROM [app].[RequestTypeVersions]
                    GROUP BY [NavigationSlug]
                    HAVING COUNT(DISTINCT [RequestTypeId]) > 1
                )
                BEGIN
                    THROW 51002, 'Existing request-type navigation slugs have conflicting owners. Resolve the duplicate ownership before applying this migration.', 1;
                END;

                INSERT INTO [app].[RequestTypePrefixReservations]
                    ([NormalizedPrefix], [RequestTypeId], [ReservedAtUtc], [ReservedByAccount], [ReservedByUserId])
                SELECT
                    [version].[RequestPrefix],
                    [version].[RequestTypeId],
                    MIN([version].[CreatedAtUtc]),
                    [requestType].[CreatedByAccount],
                    NULL
                FROM [app].[RequestTypeVersions] AS [version]
                INNER JOIN [app].[RequestTypes] AS [requestType]
                    ON [requestType].[Id] = [version].[RequestTypeId]
                GROUP BY
                    [version].[RequestPrefix],
                    [version].[RequestTypeId],
                    [requestType].[CreatedByAccount];

                INSERT INTO [app].[RequestTypeSlugReservations]
                    ([NormalizedSlug], [RequestTypeId], [ReservedAtUtc], [ReservedByAccount], [ReservedByUserId])
                SELECT
                    [version].[NavigationSlug],
                    [version].[RequestTypeId],
                    MIN([version].[CreatedAtUtc]),
                    [requestType].[CreatedByAccount],
                    NULL
                FROM [app].[RequestTypeVersions] AS [version]
                INNER JOIN [app].[RequestTypes] AS [requestType]
                    ON [requestType].[Id] = [version].[RequestTypeId]
                GROUP BY
                    [version].[NavigationSlug],
                    [version].[RequestTypeId],
                    [requestType].[CreatedByAccount];
                """);

            migrationBuilder.CreateIndex(
                name: "IX_RequestTypeVersions_RequestTypeId_NavigationSlug",
                schema: "app",
                table: "RequestTypeVersions",
                columns: new[] { "RequestTypeId", "NavigationSlug" });

            migrationBuilder.CreateIndex(
                name: "IX_RequestTypeVersions_RequestTypeId_RequestPrefix",
                schema: "app",
                table: "RequestTypeVersions",
                columns: new[] { "RequestTypeId", "RequestPrefix" });

            migrationBuilder.CreateIndex(
                name: "IX_RequestTypePrefixReservations_ReservedByUserId",
                schema: "app",
                table: "RequestTypePrefixReservations",
                column: "ReservedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RequestTypeSlugReservations_ReservedByUserId",
                schema: "app",
                table: "RequestTypeSlugReservations",
                column: "ReservedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_RequestTypeVersions_RequestTypePrefixReservations_RequestTypeId_RequestPrefix",
                schema: "app",
                table: "RequestTypeVersions",
                columns: new[] { "RequestTypeId", "RequestPrefix" },
                principalSchema: "app",
                principalTable: "RequestTypePrefixReservations",
                principalColumns: new[] { "RequestTypeId", "NormalizedPrefix" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RequestTypeVersions_RequestTypeSlugReservations_RequestTypeId_NavigationSlug",
                schema: "app",
                table: "RequestTypeVersions",
                columns: new[] { "RequestTypeId", "NavigationSlug" },
                principalSchema: "app",
                principalTable: "RequestTypeSlugReservations",
                principalColumns: new[] { "RequestTypeId", "NormalizedSlug" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RequestTypeVersions_RequestTypePrefixReservations_RequestTypeId_RequestPrefix",
                schema: "app",
                table: "RequestTypeVersions");

            migrationBuilder.DropForeignKey(
                name: "FK_RequestTypeVersions_RequestTypeSlugReservations_RequestTypeId_NavigationSlug",
                schema: "app",
                table: "RequestTypeVersions");

            migrationBuilder.DropTable(
                name: "RequestTypePrefixReservations",
                schema: "app");

            migrationBuilder.DropTable(
                name: "RequestTypeSlugReservations",
                schema: "app");

            migrationBuilder.DropIndex(
                name: "IX_RequestTypeVersions_RequestTypeId_NavigationSlug",
                schema: "app",
                table: "RequestTypeVersions");

            migrationBuilder.DropIndex(
                name: "IX_RequestTypeVersions_RequestTypeId_RequestPrefix",
                schema: "app",
                table: "RequestTypeVersions");

            migrationBuilder.AlterColumn<string>(
                name: "RequestPrefix",
                schema: "app",
                table: "RequestTypeVersions",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(30)",
                oldUnicode: false,
                oldMaxLength: 30);

            migrationBuilder.AlterColumn<string>(
                name: "NavigationSlug",
                schema: "app",
                table: "RequestTypeVersions",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(200)",
                oldUnicode: false,
                oldMaxLength: 200);
        }
    }
}
