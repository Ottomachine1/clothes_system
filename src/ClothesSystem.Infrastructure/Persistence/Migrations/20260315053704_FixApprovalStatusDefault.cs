using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClothesSystem.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixApprovalStatusDefault : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "ApprovalStatus",
                table: "ClothingItems",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.Sql("""
                UPDATE "ClothingItems"
                SET "ApprovalStatus" = 1
                WHERE "ApprovalStatus" = 0;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "ApprovalStatus",
                table: "ClothingItems",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldDefaultValue: 1);
        }
    }
}
