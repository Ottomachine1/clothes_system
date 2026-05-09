using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClothesSystem.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MultiAttachmentWorkflowAndFabricTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ApprovalStatus",
                table: "ClothingItems",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "ApprovalRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ClothingItemId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Action = table.Column<int>(type: "INTEGER", nullable: false),
                    Comment = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    CreatedByUserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: false),
                    CreatedByDisplayName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovalRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApprovalRecords_ClothingItems_ClothingItemId",
                        column: x => x.ClothingItemId,
                        principalTable: "ClothingItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClothingImageAttachments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ClothingItemId = table.Column<Guid>(type: "TEXT", nullable: false),
                    FilePath = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    OriginalFileName = table.Column<string>(type: "TEXT", maxLength: 260, nullable: false),
                    UploadedByUserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: false),
                    UploadedByDisplayName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    UploadedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClothingImageAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClothingImageAttachments_ClothingItems_ClothingItemId",
                        column: x => x.ClothingItemId,
                        principalTable: "ClothingItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FabricEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ClothingItemId = table.Column<Guid>(type: "TEXT", nullable: false),
                    MaterialName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Specification = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Remark = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FabricEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FabricEntries_ClothingItems_ClothingItemId",
                        column: x => x.ClothingItemId,
                        principalTable: "ClothingItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalRecords_ClothingItemId",
                table: "ApprovalRecords",
                column: "ClothingItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ClothingImageAttachments_ClothingItemId",
                table: "ClothingImageAttachments",
                column: "ClothingItemId");

            migrationBuilder.CreateIndex(
                name: "IX_FabricEntries_ClothingItemId",
                table: "FabricEntries",
                column: "ClothingItemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApprovalRecords");

            migrationBuilder.DropTable(
                name: "ClothingImageAttachments");

            migrationBuilder.DropTable(
                name: "FabricEntries");

            migrationBuilder.DropColumn(
                name: "ApprovalStatus",
                table: "ClothingItems");
        }
    }
}
