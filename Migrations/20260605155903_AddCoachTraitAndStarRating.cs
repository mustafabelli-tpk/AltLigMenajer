using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AltLigMenajer.Migrations
{
    /// <inheritdoc />
    public partial class AddCoachTraitAndStarRating : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Ovr",
                table: "Teams",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "RequiredStarRating",
                table: "Teams",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "CoachTrait",
                table: "Managers",
                type: "TEXT",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "StarRating",
                table: "Managers",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Ovr",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "RequiredStarRating",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "CoachTrait",
                table: "Managers");

            migrationBuilder.DropColumn(
                name: "StarRating",
                table: "Managers");
        }
    }
}
