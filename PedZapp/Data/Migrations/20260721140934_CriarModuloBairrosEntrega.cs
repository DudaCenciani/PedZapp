using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PedZapp.Data.Migrations
{
    /// <inheritdoc />
    public partial class CriarModuloBairrosEntrega : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BairrosEntrega",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmpresaId = table.Column<int>(type: "int", nullable: false),
                    NomeBairro = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TaxaEntrega = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TempoEstimadoEntregaMinutos = table.Column<int>(type: "int", nullable: true),
                    PedidoMinimo = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Ativo = table.Column<bool>(type: "bit", nullable: false),
                    OrdemExibicao = table.Column<int>(type: "int", nullable: false),
                    DataCriacao = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BairrosEntrega", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BairrosEntrega_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BairrosEntrega_EmpresaId_NomeBairro",
                table: "BairrosEntrega",
                columns: new[] { "EmpresaId", "NomeBairro" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BairrosEntrega");
        }
    }
}
