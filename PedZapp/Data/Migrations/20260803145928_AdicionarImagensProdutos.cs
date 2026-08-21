using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PedZapp.Data.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarImagensProdutos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProdutoImagens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmpresaId = table.Column<int>(type: "int", nullable: false),
                    ProdutoId = table.Column<int>(type: "int", nullable: false),
                    Dados = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    TipoConteudo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NomeArquivo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Tamanho = table.Column<long>(type: "bigint", nullable: false),
                    DataAtualizacao = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProdutoImagens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProdutoImagens_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProdutoImagens_Produtos_ProdutoId",
                        column: x => x.ProdutoId,
                        principalTable: "Produtos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProdutoImagens_EmpresaId",
                table: "ProdutoImagens",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_ProdutoImagens_ProdutoId",
                table: "ProdutoImagens",
                column: "ProdutoId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProdutoImagens");
        }
    }
}
