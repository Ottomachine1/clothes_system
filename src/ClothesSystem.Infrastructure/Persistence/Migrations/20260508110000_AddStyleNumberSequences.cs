using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClothesSystem.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStyleNumberSequences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StyleNumberSequences",
                columns: table => new
                {
                    Period = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    LastSequence = table.Column<int>(type: "INTEGER", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StyleNumberSequences", x => x.Period);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StyleNumberSequences");
        }
    }
}
