using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TennisSim.Migrations
{
    /// <inheritdoc />
    public partial class Stors : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "HasBeenViewed",
                table: "Schedules",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HasBeenViewed",
                table: "Schedules");
        }
    }
}
