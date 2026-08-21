using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PedZapp.Data.Migrations
{
    /// <inheritdoc />
    public partial class CriarFilaImpressaoPedidos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ImpressaoPedidos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmpresaId = table.Column<int>(type: "int", nullable: false),
                    PedidoId = table.Column<int>(type: "int", nullable: false),
                    TipoImpressao = table.Column<int>(type: "int", nullable: false),
                    StatusImpressao = table.Column<int>(type: "int", nullable: false),
                    QuantidadeTentativas = table.Column<int>(type: "int", nullable: false),
                    DataCriacao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DataImpressao = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UltimoErro = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TokenPublico = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ChaveEvento = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Ativa = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImpressaoPedidos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImpressaoPedidos_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ImpressaoPedidos_Pedidos_PedidoId",
                        column: x => x.PedidoId,
                        principalTable: "Pedidos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ImpressaoPedidos_EmpresaId_StatusImpressao",
                table: "ImpressaoPedidos",
                columns: new[] { "EmpresaId", "StatusImpressao" });

            migrationBuilder.CreateIndex(
                name: "IX_ImpressaoPedidos_PedidoId_TipoImpressao_ChaveEvento",
                table: "ImpressaoPedidos",
                columns: new[] { "PedidoId", "TipoImpressao", "ChaveEvento" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ImpressaoPedidos_TokenPublico",
                table: "ImpressaoPedidos",
                column: "TokenPublico",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ImpressaoPedidos");
        }
    }
}
