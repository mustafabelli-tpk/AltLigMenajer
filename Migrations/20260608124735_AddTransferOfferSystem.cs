using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AltLigMenajer.Migrations
{
    /// <inheritdoc />
    public partial class AddTransferOfferSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TransferOffers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PlayerId = table.Column<int>(type: "INTEGER", nullable: false),
                    OfferingTeamId = table.Column<int>(type: "INTEGER", nullable: false),
                    OfferedFee = table.Column<decimal>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    DateOffered = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ManagerId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransferOffers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TransferOffers_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TransferOffers_Teams_OfferingTeamId",
                        column: x => x.OfferingTeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TransferOffers_OfferingTeamId",
                table: "TransferOffers",
                column: "OfferingTeamId");

            migrationBuilder.CreateIndex(
                name: "IX_TransferOffers_PlayerId",
                table: "TransferOffers",
                column: "PlayerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TransferOffers");
        }
    }
}
