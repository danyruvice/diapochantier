using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiaspoChantier.Data.Migrations
{
    /// <inheritdoc />
    public partial class FeatureComplete_MateriauxAvisPhotos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AlerteEnvoyee",
                table: "Photos",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "AvisEntrepreneurs",
                columns: table => new
                {
                    IdAvis = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdEntrepreneur = table.Column<int>(type: "int", nullable: false),
                    IdInvestisseur = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    IdProjet = table.Column<int>(type: "int", nullable: false),
                    Note = table.Column<int>(type: "int", nullable: false),
                    Commentaire = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DateCreation = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AvisEntrepreneurs", x => x.IdAvis);
                });

            migrationBuilder.CreateTable(
                name: "Materiaux",
                columns: table => new
                {
                    IdMateriau = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdProjet = table.Column<int>(type: "int", nullable: false),
                    IdEntrepreneur = table.Column<int>(type: "int", nullable: false),
                    Fournisseur = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Designation = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Quantite = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    Unite = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    PrixTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    UrlFacture = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    HashSHA256 = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    QrCodePayload = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    DateCreation = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Materiaux", x => x.IdMateriau);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AvisEntrepreneurs_IdEntrepreneur_IdProjet_IdInvestisseur",
                table: "AvisEntrepreneurs",
                columns: new[] { "IdEntrepreneur", "IdProjet", "IdInvestisseur" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Materiaux_HashSHA256",
                table: "Materiaux",
                column: "HashSHA256");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "AvisEntrepreneurs");
            migrationBuilder.DropTable(name: "Materiaux");
            migrationBuilder.DropColumn(name: "AlerteEnvoyee", table: "Photos");
        }
    }
}
