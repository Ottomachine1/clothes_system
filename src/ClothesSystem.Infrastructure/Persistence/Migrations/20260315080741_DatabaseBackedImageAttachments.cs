using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClothesSystem.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DatabaseBackedImageAttachments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "FilePath",
                table: "ClothingImageAttachments",
                type: "TEXT",
                maxLength: 300,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 300);

            migrationBuilder.AddColumn<byte[]>(
                name: "BinaryContent",
                table: "ClothingImageAttachments",
                type: "BLOB",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContentType",
                table: "ClothingImageAttachments",
                type: "TEXT",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BinaryContent",
                table: "ClothingImageAttachments");

            migrationBuilder.DropColumn(
                name: "ContentType",
                table: "ClothingImageAttachments");

            migrationBuilder.AlterColumn<string>(
                name: "FilePath",
                table: "ClothingImageAttachments",
                type: "TEXT",
                maxLength: 300,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 300,
                oldNullable: true);
        }
    }
}
