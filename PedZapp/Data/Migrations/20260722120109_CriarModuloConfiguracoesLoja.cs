using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PedZapp.Data.Migrations
{
    /// <inheritdoc />
    public partial class CriarModuloConfiguracoesLoja : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ConfiguracoesLoja",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmpresaId = table.Column<int>(type: "int", nullable: false),
                    AceitaPedidos = table.Column<bool>(type: "bit", nullable: false),
                    PedidoMinimo = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    TempoMedioPreparoMinutos = table.Column<int>(type: "int", nullable: true),
                    MensagemAutomatica = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TelefoneAtendimento = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WhatsAppAtendimento = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Instagram = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Facebook = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CorPrimaria = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CorSecundaria = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ObservacoesInternas = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DataAtualizacao = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfiguracoesLoja", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConfiguracoesLoja_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConfiguracoesLoja_EmpresaId",
                table: "ConfiguracoesLoja",
                column: "EmpresaId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConfiguracoesLoja");
        }
    }
}
