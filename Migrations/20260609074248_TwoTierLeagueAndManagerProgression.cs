using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AltLigMenajer.Migrations
{
    /// <inheritdoc />
    public partial class TwoTierLeagueAndManagerProgression : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LeagueTier",
                table: "Teams",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ExperiencePoints",
                table: "Managers",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "LicenseLevel",
                table: "Managers",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "LeagueTier",
                table: "Fixtures",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LeagueTier",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "ExperiencePoints",
                table: "Managers");

            migrationBuilder.DropColumn(
                name: "LicenseLevel",
                table: "Managers");

            migrationBuilder.DropColumn(
                name: "LeagueTier",
                table: "Fixtures");
        }
    }
}
