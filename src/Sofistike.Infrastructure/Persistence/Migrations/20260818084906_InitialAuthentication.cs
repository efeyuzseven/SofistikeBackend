using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sofistike.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class InitialAuthentication : Migration
{
    private static readonly string[] DevelopmentUserColumns =
    [
        "Id",
        "CreatedAtUtc",
        "Email",
        "FirstName",
        "IsActive",
        "LastName",
        "NormalizedEmail",
        "PasswordHash",
        "PasswordIterations",
        "PasswordSalt",
        "PhoneNumber",
        "Role",
    ];

    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(
            name: "auth");

        migrationBuilder.CreateTable(
            name: "Users",
            schema: "auth",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Email = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                NormalizedEmail = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                PhoneNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                Role = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                PasswordSalt = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                PasswordHash = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                PasswordIterations = table.Column<int>(type: "int", nullable: false),
                IsActive = table.Column<bool>(type: "bit", nullable: false),
                CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Users", x => x.Id);
            });

        migrationBuilder.InsertData(
            schema: "auth",
            table: "Users",
            columns: DevelopmentUserColumns,
            values: new object[] { new Guid("d8fbd714-b22f-4a7f-b576-c6a2183f6e80"), new DateTimeOffset(new DateTime(2026, 8, 18, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "umay@sofistike.com", "Umay", true, null, "UMAY@SOFISTIKE.COM", "StGqoqkeBqdPX6jwzsH95lJoqA8/Ej8s3ruOXAaE754=", 120000, "A28ElxOKGPX0xqNXBZE9Ug==", null, "Customer" });

        migrationBuilder.CreateIndex(
            name: "IX_Users_NormalizedEmail",
            schema: "auth",
            table: "Users",
            column: "NormalizedEmail",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "Users",
            schema: "auth");
    }
}
