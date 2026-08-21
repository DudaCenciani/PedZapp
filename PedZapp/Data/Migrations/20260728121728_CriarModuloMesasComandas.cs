using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PedZapp.Data.Migrations
{
    /// <inheritdoc />
    public partial class CriarModuloMesasComandas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ComandaId",
                table: "Pedidos",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CriadoPorUsuarioId",
                table: "Pedidos",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NomeFuncionarioSnapshot",
                table: "Pedidos",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Mesas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmpresaId = table.Column<int>(type: "int", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Numero = table.Column<int>(type: "int", nullable: true),
                    Capacidade = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Ativa = table.Column<bool>(type: "bit", nullable: false),
                    Observacao = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OrdemExibicao = table.Column<int>(type: "int", nullable: false),
                    CodigoPublicoSeguro = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DataCriacao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DataAtualizacao = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Mesas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Mesas_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Comandas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmpresaId = table.Column<int>(type: "int", nullable: false),
                    MesaId = table.Column<int>(type: "int", nullable: false),
                    NumeroComanda = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CriadaPorUsuarioId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    NomeFuncionarioSnapshot = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DataAbertura = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DataFechamento = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Subtotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PercentualTaxaServico = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    ValorTaxaServico = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TaxaServicoAplicada = table.Column<bool>(type: "bit", nullable: false),
                    Total = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    FormaPagamentoId = table.Column<int>(type: "int", nullable: true),
                    NomeFormaPagamentoSnapshot = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Observacao = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Ativa = table.Column<bool>(type: "bit", nullable: false),
                    CodigoPublicoSeguro = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Comandas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Comandas_AspNetUsers_CriadaPorUsuarioId",
                        column: x => x.CriadaPorUsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Comandas_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Comandas_FormasPagamento_FormaPagamentoId",
                        column: x => x.FormaPagamentoId,
                        principalTable: "FormasPagamento",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Comandas_Mesas_MesaId",
                        column: x => x.MesaId,
                        principalTable: "Mesas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ComandaItens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ComandaId = table.Column<int>(type: "int", nullable: false),
                    ProdutoId = table.Column<int>(type: "int", nullable: false),
                    NomeProdutoSnapshot = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PrecoUnitario = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Quantidade = table.Column<int>(type: "int", nullable: false),
                    Observacao = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Subtotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    EnviadoParaCozinha = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComandaItens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ComandaItens_Comandas_ComandaId",
                        column: x => x.ComandaId,
                        principalTable: "Comandas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ComandaItens_Produtos_ProdutoId",
                        column: x => x.ProdutoId,
                        principalTable: "Produtos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ComandaItemAdicionais",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ComandaItemId = table.Column<int>(type: "int", nullable: false),
                    AdicionalId = table.Column<int>(type: "int", nullable: false),
                    NomeAdicionalSnapshot = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PrecoUnitario = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComandaItemAdicionais", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ComandaItemAdicionais_ComandaItens_ComandaItemId",
                        column: x => x.ComandaItemId,
                        principalTable: "ComandaItens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Pedidos_ComandaId",
                table: "Pedidos",
                column: "ComandaId");

            migrationBuilder.CreateIndex(
                name: "IX_ComandaItemAdicionais_ComandaItemId",
                table: "ComandaItemAdicionais",
                column: "ComandaItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ComandaItens_ComandaId",
                table: "ComandaItens",
                column: "ComandaId");

            migrationBuilder.CreateIndex(
                name: "IX_ComandaItens_ProdutoId",
                table: "ComandaItens",
                column: "ProdutoId");

            migrationBuilder.CreateIndex(
                name: "IX_Comandas_CodigoPublicoSeguro",
                table: "Comandas",
                column: "CodigoPublicoSeguro",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Comandas_CriadaPorUsuarioId",
                table: "Comandas",
                column: "CriadaPorUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Comandas_EmpresaId",
                table: "Comandas",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_Comandas_FormaPagamentoId",
                table: "Comandas",
                column: "FormaPagamentoId");

            migrationBuilder.CreateIndex(
                name: "IX_Comandas_MesaId_Ativa",
                table: "Comandas",
                columns: new[] { "MesaId", "Ativa" },
                unique: true,
                filter: "[Ativa] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_Mesas_CodigoPublicoSeguro",
                table: "Mesas",
                column: "CodigoPublicoSeguro",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Mesas_EmpresaId_Nome",
                table: "Mesas",
                columns: new[] { "EmpresaId", "Nome" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Mesas_EmpresaId_Numero",
                table: "Mesas",
                columns: new[] { "EmpresaId", "Numero" },
                unique: true,
                filter: "[Numero] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Pedidos_Comandas_ComandaId",
                table: "Pedidos",
                column: "ComandaId",
                principalTable: "Comandas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Pedidos_Comandas_ComandaId",
                table: "Pedidos");

            migrationBuilder.DropTable(
                name: "ComandaItemAdicionais");

            migrationBuilder.DropTable(
                name: "ComandaItens");

            migrationBuilder.DropTable(
                name: "Comandas");

            migrationBuilder.DropTable(
                name: "Mesas");

            migrationBuilder.DropIndex(
                name: "IX_Pedidos_ComandaId",
                table: "Pedidos");

            migrationBuilder.DropColumn(
                name: "ComandaId",
                table: "Pedidos");

            migrationBuilder.DropColumn(
                name: "CriadoPorUsuarioId",
                table: "Pedidos");

            migrationBuilder.DropColumn(
                name: "NomeFuncionarioSnapshot",
                table: "Pedidos");
        }
    }
}
