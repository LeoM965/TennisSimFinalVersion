using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TennisSim.Migrations
{
    /// <inheritdoc />
    public partial class repar : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Schedules_Tournaments_TournamentId1",
                table: "Schedules");

            migrationBuilder.DropIndex(
                name: "IX_Schedules_TournamentId1",
                table: "Schedules");

            migrationBuilder.DropColumn(
                name: "TournamentId1",
                table: "Schedules");

            migrationBuilder.AddColumn<int>(
                name: "TournamentId",
                table: "Schedules",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Schedules_TournamentId",
                table: "Schedules",
                column: "TournamentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Schedules_Tournaments_TournamentId",
                table: "Schedules",
                column: "TournamentId",
                principalTable: "Tournaments",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Schedules_Tournaments_TournamentId",
                table: "Schedules");

            migrationBuilder.DropIndex(
                name: "IX_Schedules_TournamentId",
                table: "Schedules");

            migrationBuilder.DropColumn(
                name: "TournamentId",
                table: "Schedules");

            migrationBuilder.AddColumn<int>(
                name: "TournamentId1",
                table: "Schedules",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Schedules_TournamentId1",
                table: "Schedules",
                column: "TournamentId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Schedules_Tournaments_TournamentId1",
                table: "Schedules",
                column: "TournamentId1",
                principalTable: "Tournaments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
