using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AltLigMenajer.Migrations
{
    /// <inheritdoc />
    public partial class AddFormationToTeam : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Formation",
                table: "Teams",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Formation",
                table: "Teams");
        }
    }
}
