using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AltLigMenajer.Migrations
{
    /// <inheritdoc />
    public partial class AddTransferListProperties : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AskingPrice",
                table: "Players",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "IsLoanListed",
                table: "Players",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsTransferListed",
                table: "Players",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AskingPrice",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "IsLoanListed",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "IsTransferListed",
                table: "Players");
        }
    }
}
