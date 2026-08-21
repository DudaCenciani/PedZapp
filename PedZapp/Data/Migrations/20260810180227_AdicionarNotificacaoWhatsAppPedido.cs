using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PedZapp.Data.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarNotificacaoWhatsAppPedido : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AceitaAtualizacoesWhatsApp",
                table: "Pedidos",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "WhatsAppConfirmacaoEmProcessamentoEm",
                table: "Pedidos",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "WhatsAppConfirmacaoEnviadaEm",
                table: "Pedidos",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "WhatsAppConfirmacaoFalhouEm",
                table: "Pedidos",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WhatsAppConfirmacaoUltimoStatusHttp",
                table: "Pedidos",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AceitaAtualizacoesWhatsApp",
                table: "Pedidos");

            migrationBuilder.DropColumn(
                name: "WhatsAppConfirmacaoEmProcessamentoEm",
                table: "Pedidos");

            migrationBuilder.DropColumn(
                name: "WhatsAppConfirmacaoEnviadaEm",
                table: "Pedidos");

            migrationBuilder.DropColumn(
                name: "WhatsAppConfirmacaoFalhouEm",
                table: "Pedidos");

            migrationBuilder.DropColumn(
                name: "WhatsAppConfirmacaoUltimoStatusHttp",
                table: "Pedidos");
        }
    }
}
