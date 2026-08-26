using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable IDE0161, CA1861

namespace Sofistike.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoryMenuGroups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Categories_IsActive_DisplayOrder",
                schema: "catalog",
                table: "Categories");

            migrationBuilder.AddColumn<string>(
                name: "MenuGroup",
                schema: "catalog",
                table: "Categories",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Category");

            var seededAt = new System.DateTimeOffset(
                2026,
                8,
                26,
                0,
                0,
                0,
                System.TimeSpan.Zero
            );
            migrationBuilder.InsertData(
                schema: "catalog",
                table: "Categories",
                columns: new[]
                {
                    "Id",
                    "Name",
                    "Slug",
                    "Description",
                    "ParentCategoryId",
                    "MenuGroup",
                    "DisplayOrder",
                    "IsActive",
                    "CreatedAtUtc",
                    "UpdatedAtUtc",
                },
                values: new object[,]
                {
                    { new System.Guid("c1000000-0000-4000-8000-000000000001"), "Sleep Better", "sleep-better", null, null, "Solution", 1, true, seededAt, seededAt },
                    { new System.Guid("c1000000-0000-4000-8000-000000000002"), "Allergy Care", "allergy-care", null, null, "Solution", 2, true, seededAt, seededAt },
                    { new System.Guid("c1000000-0000-4000-8000-000000000003"), "Home Reset", "home-reset", null, null, "Solution", 3, true, seededAt, seededAt },
                    { new System.Guid("c1000000-0000-4000-8000-000000000004"), "Laundry Care", "laundry-care", null, null, "Solution", 4, true, seededAt, seededAt },
                    { new System.Guid("c1000000-0000-4000-8000-000000000005"), "Bathroom Care", "bathroom-care", null, null, "Solution", 5, true, seededAt, seededAt },
                    { new System.Guid("c1000000-0000-4000-8000-000000000006"), "Kitchen Care", "kitchen-care", null, null, "Solution", 6, true, seededAt, seededAt },
                    { new System.Guid("c1000000-0000-4000-8000-000000000007"), "Pet Friendly", "pet-friendly", null, null, "Solution", 7, true, seededAt, seededAt },
                    { new System.Guid("c1000000-0000-4000-8000-000000000008"), "Healthy Living", "healthy-living", null, null, "Solution", 8, true, seededAt, seededAt },
                    { new System.Guid("c1000000-0000-4000-8000-000000000009"), "Organization", "organization", null, null, "Solution", 9, true, seededAt, seededAt },
                    { new System.Guid("c1000000-0000-4000-8000-000000000010"), "Innovation Lab", "innovation-lab", null, null, "Solution", 10, true, seededAt, seededAt },
                    { new System.Guid("c2000000-0000-4000-8000-000000000001"), "Bedroom", "bedroom", null, null, "Room", 1, true, seededAt, seededAt },
                    { new System.Guid("c2000000-0000-4000-8000-000000000002"), "Bathroom", "bathroom", null, null, "Room", 2, true, seededAt, seededAt },
                    { new System.Guid("c2000000-0000-4000-8000-000000000003"), "Living Room", "living-room", null, null, "Room", 3, true, seededAt, seededAt },
                    { new System.Guid("c2000000-0000-4000-8000-000000000004"), "Kitchen", "kitchen-room", null, null, "Room", 4, true, seededAt, seededAt },
                    { new System.Guid("c2000000-0000-4000-8000-000000000005"), "Laundry Room", "laundry-room", null, null, "Room", 5, true, seededAt, seededAt },
                    { new System.Guid("c2000000-0000-4000-8000-000000000006"), "Kids Room", "kids-room", null, null, "Room", 6, true, seededAt, seededAt },
                    { new System.Guid("c2000000-0000-4000-8000-000000000007"), "Guest Room", "guest-room", null, null, "Room", 7, true, seededAt, seededAt },
                    { new System.Guid("c2000000-0000-4000-8000-000000000008"), "Pet Area", "pet-area", null, null, "Room", 8, true, seededAt, seededAt },
                    { new System.Guid("c2000000-0000-4000-8000-000000000009"), "Travel", "travel", null, null, "Room", 9, true, seededAt, seededAt },
                });

            migrationBuilder.CreateIndex(
                name: "IX_Categories_IsActive_MenuGroup_DisplayOrder",
                schema: "catalog",
                table: "Categories",
                columns: new[] { "IsActive", "MenuGroup", "DisplayOrder" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Categories_MenuGroup",
                schema: "catalog",
                table: "Categories",
                sql: "[MenuGroup] IN ('Solution', 'Room', 'Category')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "catalog",
                table: "Categories",
                keyColumns: new[] { "Id" },
                keyValues: new object[,]
                {
                    { new System.Guid("c1000000-0000-4000-8000-000000000001") },
                    { new System.Guid("c1000000-0000-4000-8000-000000000002") },
                    { new System.Guid("c1000000-0000-4000-8000-000000000003") },
                    { new System.Guid("c1000000-0000-4000-8000-000000000004") },
                    { new System.Guid("c1000000-0000-4000-8000-000000000005") },
                    { new System.Guid("c1000000-0000-4000-8000-000000000006") },
                    { new System.Guid("c1000000-0000-4000-8000-000000000007") },
                    { new System.Guid("c1000000-0000-4000-8000-000000000008") },
                    { new System.Guid("c1000000-0000-4000-8000-000000000009") },
                    { new System.Guid("c1000000-0000-4000-8000-000000000010") },
                    { new System.Guid("c2000000-0000-4000-8000-000000000001") },
                    { new System.Guid("c2000000-0000-4000-8000-000000000002") },
                    { new System.Guid("c2000000-0000-4000-8000-000000000003") },
                    { new System.Guid("c2000000-0000-4000-8000-000000000004") },
                    { new System.Guid("c2000000-0000-4000-8000-000000000005") },
                    { new System.Guid("c2000000-0000-4000-8000-000000000006") },
                    { new System.Guid("c2000000-0000-4000-8000-000000000007") },
                    { new System.Guid("c2000000-0000-4000-8000-000000000008") },
                    { new System.Guid("c2000000-0000-4000-8000-000000000009") },
                });

            migrationBuilder.DropIndex(
                name: "IX_Categories_IsActive_MenuGroup_DisplayOrder",
                schema: "catalog",
                table: "Categories");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Categories_MenuGroup",
                schema: "catalog",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "MenuGroup",
                schema: "catalog",
                table: "Categories");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_IsActive_DisplayOrder",
                schema: "catalog",
                table: "Categories",
                columns: new[] { "IsActive", "DisplayOrder" });
        }
    }
}
