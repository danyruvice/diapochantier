using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiaspoChantier.Data.Migrations
{
    /// <inheritdoc />
    public partial class Sprint2_Projet_Jalon : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Projets",
                columns: table => new
                {
                    IdProjet = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Titre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    TypeProjet = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Localisation = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Latitude = table.Column<double>(type: "float", nullable: false),
                    Longitude = table.Column<double>(type: "float", nullable: false),
                    BudgetTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Statut = table.Column<int>(type: "int", nullable: false),
                    DateCreation = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateMiseAJour = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IdInvestisseur = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    IdEntrepreneur = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projets", x => x.IdProjet);
                    table.ForeignKey(
                        name: "FK_Projets_Entrepreneurs_IdEntrepreneur",
                        column: x => x.IdEntrepreneur,
                        principalTable: "Entrepreneurs",
                        principalColumn: "IdEntrepreneur",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Projets_Utilisateurs_IdInvestisseur",
                        column: x => x.IdInvestisseur,
                        principalTable: "Utilisateurs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Jalons",
                columns: table => new
                {
                    IdJalon = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Titre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Montant = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Ordre = table.Column<int>(type: "int", nullable: false),
                    DateLimite = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EstValide = table.Column<bool>(type: "bit", nullable: false),
                    DateValidation = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IdProjet = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Jalons", x => x.IdJalon);
                    table.ForeignKey(
                        name: "FK_Jalons_Projets_IdProjet",
                        column: x => x.IdProjet,
                        principalTable: "Projets",
                        principalColumn: "IdProjet",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Jalons_IdProjet",
                table: "Jalons",
                column: "IdProjet");

            migrationBuilder.CreateIndex(
                name: "IX_Projets_IdEntrepreneur",
                table: "Projets",
                column: "IdEntrepreneur");

            migrationBuilder.CreateIndex(
                name: "IX_Projets_IdInvestisseur",
                table: "Projets",
                column: "IdInvestisseur");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Jalons");

            migrationBuilder.DropTable(
                name: "Projets");
        }
    }
}
