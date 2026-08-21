using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PedZapp.Data.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarFluxoPedidosEmpresa : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TipoFluxoPedido",
                table: "ConfiguracoesLoja",
                type: "int",
                nullable: false,
                // 1 corresponde a Completo: nenhuma empresa existente muda de fluxo ao aplicar a migration.
                defaultValue: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TipoFluxoPedido",
                table: "ConfiguracoesLoja");
        }
    }
}
