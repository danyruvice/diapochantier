using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiaspoChantier.Data.Migrations
{
    /// <inheritdoc />
    public partial class Sprint3_PhotoAntiFraude : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Photos",
                columns: table => new
                {
                    IdPhoto = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdProjet = table.Column<int>(type: "int", nullable: false),
                    IdJalon = table.Column<int>(type: "int", nullable: false),
                    Url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    HashSHA256 = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Latitude = table.Column<double>(type: "float", nullable: false),
                    Longitude = table.Column<double>(type: "float", nullable: false),
                    Altitude = table.Column<double>(type: "float", nullable: true),
                    HorodatageGps = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EstAuthentique = table.Column<bool>(type: "bit", nullable: false),
                    Source = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DateCreation = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Photos", x => x.IdPhoto);
                    table.ForeignKey(
                        name: "FK_Photos_Jalons_IdJalon",
                        column: x => x.IdJalon,
                        principalTable: "Jalons",
                        principalColumn: "IdJalon",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Photos_Projets_IdProjet",
                        column: x => x.IdProjet,
                        principalTable: "Projets",
                        principalColumn: "IdProjet",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Photos_IdJalon",
                table: "Photos",
                column: "IdJalon");

            migrationBuilder.CreateIndex(
                name: "IX_Photos_IdProjet",
                table: "Photos",
                column: "IdProjet");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Photos");
        }
    }
}
