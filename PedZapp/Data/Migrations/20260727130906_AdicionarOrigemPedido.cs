using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PedZapp.Data.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarOrigemPedido : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Origem",
                table: "Pedidos",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Origem",
                table: "Pedidos");
        }
    }
}
