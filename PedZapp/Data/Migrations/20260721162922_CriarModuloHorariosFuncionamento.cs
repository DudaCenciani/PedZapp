using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PedZapp.Data.Migrations
{
    /// <inheritdoc />
    public partial class CriarModuloHorariosFuncionamento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HorariosFuncionamento",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmpresaId = table.Column<int>(type: "int", nullable: false),
                    DiaSemana = table.Column<int>(type: "int", nullable: false),
                    Fechado = table.Column<bool>(type: "bit", nullable: false),
                    Abertura1 = table.Column<TimeOnly>(type: "time", nullable: true),
                    Fechamento1 = table.Column<TimeOnly>(type: "time", nullable: true),
                    Abertura2 = table.Column<TimeOnly>(type: "time", nullable: true),
                    Fechamento2 = table.Column<TimeOnly>(type: "time", nullable: true),
                    Ativo = table.Column<bool>(type: "bit", nullable: false),
                    OrdemExibicao = table.Column<int>(type: "int", nullable: false),
                    DataCriacao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DataAtualizacao = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HorariosFuncionamento", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HorariosFuncionamento_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HorariosFuncionamento_EmpresaId_DiaSemana",
                table: "HorariosFuncionamento",
                columns: new[] { "EmpresaId", "DiaSemana" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HorariosFuncionamento");
        }
    }
}
