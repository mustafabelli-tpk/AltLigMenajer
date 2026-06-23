using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AltLigMenajer.Migrations
{
    /// <inheritdoc />
    public partial class AddGoalsAssistsMatchweek : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Assists",
                table: "Players",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Goals",
                table: "Players",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Matchweek",
                table: "Fixtures",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Assists",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "Goals",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "Matchweek",
                table: "Fixtures");
        }
    }
}
