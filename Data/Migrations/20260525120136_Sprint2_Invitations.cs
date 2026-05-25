using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiaspoChantier.Data.Migrations
{
    /// <inheritdoc />
    public partial class Sprint2_Invitations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Invitations",
                columns: table => new
                {
                    IdInvitation = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdProjet = table.Column<int>(type: "int", nullable: false),
                    IdEntrepreneur = table.Column<int>(type: "int", nullable: false),
                    Statut = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DateCreation = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateReponse = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Invitations", x => x.IdInvitation);
                    table.ForeignKey(
                        name: "FK_Invitations_Entrepreneurs_IdEntrepreneur",
                        column: x => x.IdEntrepreneur,
                        principalTable: "Entrepreneurs",
                        principalColumn: "IdEntrepreneur",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Invitations_Projets_IdProjet",
                        column: x => x.IdProjet,
                        principalTable: "Projets",
                        principalColumn: "IdProjet",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Invitations_IdEntrepreneur",
                table: "Invitations",
                column: "IdEntrepreneur");

            migrationBuilder.CreateIndex(
                name: "IX_Invitations_IdProjet_IdEntrepreneur",
                table: "Invitations",
                columns: new[] { "IdProjet", "IdEntrepreneur" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Invitations");
        }
    }
}
