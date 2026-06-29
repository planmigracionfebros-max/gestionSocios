using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GestionSpa.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddFamilia : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FamiliaId",
                table: "Socios",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Familias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "text", nullable: false),
                    CuotaMensual = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    Observaciones = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Familias", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Socios_FamiliaId",
                table: "Socios",
                column: "FamiliaId");

            migrationBuilder.CreateIndex(
                name: "IX_Familias_Nombre",
                table: "Familias",
                column: "Nombre",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Socios_Familias_FamiliaId",
                table: "Socios",
                column: "FamiliaId",
                principalTable: "Familias",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Socios_Familias_FamiliaId",
                table: "Socios");

            migrationBuilder.DropTable(
                name: "Familias");

            migrationBuilder.DropIndex(
                name: "IX_Socios_FamiliaId",
                table: "Socios");

            migrationBuilder.DropColumn(
                name: "FamiliaId",
                table: "Socios");
        }
    }
}
