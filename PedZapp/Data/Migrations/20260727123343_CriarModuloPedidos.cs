using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PedZapp.Data.Migrations
{
    /// <inheritdoc />
    public partial class CriarModuloPedidos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Pedidos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmpresaId = table.Column<int>(type: "int", nullable: false),
                    NumeroPedido = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CodigoPublico = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ChaveIdempotencia = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    TipoAtendimento = table.Column<int>(type: "int", nullable: false),
                    NomeCliente = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TelefoneCliente = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BairroEntregaId = table.Column<int>(type: "int", nullable: true),
                    NomeBairroSnapshot = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TaxaEntrega = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Rua = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NumeroEndereco = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Complemento = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Referencia = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FormaPagamentoId = table.Column<int>(type: "int", nullable: false),
                    NomeFormaPagamentoSnapshot = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PrecisaTroco = table.Column<bool>(type: "bit", nullable: false),
                    TrocoPara = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Subtotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Total = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ObservacaoGeral = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DataCriacao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DataAtualizacao = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Pago = table.Column<bool>(type: "bit", nullable: false),
                    Cancelado = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pedidos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Pedidos_BairrosEntrega_BairroEntregaId",
                        column: x => x.BairroEntregaId,
                        principalTable: "BairrosEntrega",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Pedidos_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Pedidos_FormasPagamento_FormaPagamentoId",
                        column: x => x.FormaPagamentoId,
                        principalTable: "FormasPagamento",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PedidoItens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PedidoId = table.Column<int>(type: "int", nullable: false),
                    ProdutoId = table.Column<int>(type: "int", nullable: true),
                    NomeProdutoSnapshot = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PrecoUnitario = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Quantidade = table.Column<int>(type: "int", nullable: false),
                    Observacao = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Subtotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PedidoItens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PedidoItens_Pedidos_PedidoId",
                        column: x => x.PedidoId,
                        principalTable: "Pedidos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PedidoItens_Produtos_ProdutoId",
                        column: x => x.ProdutoId,
                        principalTable: "Produtos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "PedidoItemAdicionais",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PedidoItemId = table.Column<int>(type: "int", nullable: false),
                    AdicionalId = table.Column<int>(type: "int", nullable: true),
                    NomeAdicionalSnapshot = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PrecoUnitario = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Quantidade = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PedidoItemAdicionais", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PedidoItemAdicionais_Adicionais_AdicionalId",
                        column: x => x.AdicionalId,
                        principalTable: "Adicionais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PedidoItemAdicionais_PedidoItens_PedidoItemId",
                        column: x => x.PedidoItemId,
                        principalTable: "PedidoItens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PedidoItemAdicionais_AdicionalId",
                table: "PedidoItemAdicionais",
                column: "AdicionalId");

            migrationBuilder.CreateIndex(
                name: "IX_PedidoItemAdicionais_PedidoItemId",
                table: "PedidoItemAdicionais",
                column: "PedidoItemId");

            migrationBuilder.CreateIndex(
                name: "IX_PedidoItens_PedidoId",
                table: "PedidoItens",
                column: "PedidoId");

            migrationBuilder.CreateIndex(
                name: "IX_PedidoItens_ProdutoId",
                table: "PedidoItens",
                column: "ProdutoId");

            migrationBuilder.CreateIndex(
                name: "IX_Pedidos_BairroEntregaId",
                table: "Pedidos",
                column: "BairroEntregaId");

            migrationBuilder.CreateIndex(
                name: "IX_Pedidos_CodigoPublico",
                table: "Pedidos",
                column: "CodigoPublico",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Pedidos_EmpresaId_ChaveIdempotencia",
                table: "Pedidos",
                columns: new[] { "EmpresaId", "ChaveIdempotencia" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Pedidos_EmpresaId_NumeroPedido",
                table: "Pedidos",
                columns: new[] { "EmpresaId", "NumeroPedido" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Pedidos_FormaPagamentoId",
                table: "Pedidos",
                column: "FormaPagamentoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PedidoItemAdicionais");

            migrationBuilder.DropTable(
                name: "PedidoItens");

            migrationBuilder.DropTable(
                name: "Pedidos");
        }
    }
}
