using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiaspoChantier.Data.Migrations
{
    /// <inheritdoc />
    public partial class CompteEtNavigation_Fixes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DateDernierRejet",
                table: "Utilisateurs",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PieceIdentiteHashSha256",
                table: "Utilisateurs",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PieceIdentiteUrl",
                table: "Utilisateurs",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MotifRefus",
                table: "Invitations",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdUtilisateur = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Token = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    DateExpiration = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateCreation = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EstRevoque = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Sequestres",
                columns: table => new
                {
                    IdSequestre = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdProjet = table.Column<int>(type: "int", nullable: false),
                    IdInvestisseur = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    MontantBloque = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MontantLibere = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Statut = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DateCreation = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateMiseAJour = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sequestres", x => x.IdSequestre);
                    table.ForeignKey(
                        name: "FK_Sequestres_Projets_IdProjet",
                        column: x => x.IdProjet,
                        principalTable: "Projets",
                        principalColumn: "IdProjet",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Sequestres_Utilisateurs_IdInvestisseur",
                        column: x => x.IdInvestisseur,
                        principalTable: "Utilisateurs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WebhookEvents",
                columns: table => new
                {
                    IdWebhookEvent = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EventId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    PayloadHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ReceivedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WebhookEvents", x => x.IdWebhookEvent);
                });

            migrationBuilder.CreateTable(
                name: "Paiements",
                columns: table => new
                {
                    IdPaiement = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdProjet = table.Column<int>(type: "int", nullable: false),
                    IdInvestisseur = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    IdEntrepreneur = table.Column<int>(type: "int", nullable: true),
                    IdSequestre = table.Column<int>(type: "int", nullable: true),
                    Montant = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TypePaiement = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Canal = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Statut = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReferenceExterne = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RecuEmailEnvoye = table.Column<bool>(type: "bit", nullable: false),
                    DateCreation = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateMiseAJour = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DatePaiement = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Paiements", x => x.IdPaiement);
                    table.ForeignKey(
                        name: "FK_Paiements_Entrepreneurs_IdEntrepreneur",
                        column: x => x.IdEntrepreneur,
                        principalTable: "Entrepreneurs",
                        principalColumn: "IdEntrepreneur",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Paiements_Projets_IdProjet",
                        column: x => x.IdProjet,
                        principalTable: "Projets",
                        principalColumn: "IdProjet",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Paiements_Sequestres_IdSequestre",
                        column: x => x.IdSequestre,
                        principalTable: "Sequestres",
                        principalColumn: "IdSequestre",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Paiements_Utilisateurs_IdInvestisseur",
                        column: x => x.IdInvestisseur,
                        principalTable: "Utilisateurs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Paiements_IdEntrepreneur",
                table: "Paiements",
                column: "IdEntrepreneur");

            migrationBuilder.CreateIndex(
                name: "IX_Paiements_IdInvestisseur",
                table: "Paiements",
                column: "IdInvestisseur");

            migrationBuilder.CreateIndex(
                name: "IX_Paiements_IdProjet",
                table: "Paiements",
                column: "IdProjet");

            migrationBuilder.CreateIndex(
                name: "IX_Paiements_IdSequestre",
                table: "Paiements",
                column: "IdSequestre");

            migrationBuilder.CreateIndex(
                name: "IX_Paiements_ReferenceExterne",
                table: "Paiements",
                column: "ReferenceExterne",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_Token",
                table: "RefreshTokens",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sequestres_IdInvestisseur",
                table: "Sequestres",
                column: "IdInvestisseur");

            migrationBuilder.CreateIndex(
                name: "IX_Sequestres_IdProjet_IdInvestisseur",
                table: "Sequestres",
                columns: new[] { "IdProjet", "IdInvestisseur" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WebhookEvents_EventId",
                table: "WebhookEvents",
                column: "EventId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Paiements");

            migrationBuilder.DropTable(
                name: "RefreshTokens");

            migrationBuilder.DropTable(
                name: "WebhookEvents");

            migrationBuilder.DropTable(
                name: "Sequestres");

            migrationBuilder.DropColumn(
                name: "DateDernierRejet",
                table: "Utilisateurs");

            migrationBuilder.DropColumn(
                name: "PieceIdentiteHashSha256",
                table: "Utilisateurs");

            migrationBuilder.DropColumn(
                name: "PieceIdentiteUrl",
                table: "Utilisateurs");

            migrationBuilder.DropColumn(
                name: "MotifRefus",
                table: "Invitations");
        }
    }
}
