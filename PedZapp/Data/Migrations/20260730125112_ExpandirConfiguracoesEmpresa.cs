using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PedZapp.Data.Migrations
{
    /// <inheritdoc />
    public partial class ExpandirConfiguracoesEmpresa : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LogoAtualizadaEm",
                table: "Empresas",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "LogoDados",
                table: "Empresas",
                type: "varbinary(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LogoNomeArquivo",
                table: "Empresas",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "LogoTamanho",
                table: "Empresas",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LogoTipoConteudo",
                table: "Empresas",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AtendimentoMesasAtivo",
                table: "ConfiguracoesLoja",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "CorDestaque",
                table: "ConfiguracoesLoja",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ExibirDescricao",
                table: "ConfiguracoesLoja",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "ExibirLogo",
                table: "ConfiguracoesLoja",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "ImpressaoAutomaticaCozinha",
                table: "ConfiguracoesLoja",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "NomeExibicaoCardapio",
                table: "ConfiguracoesLoja",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TextoCurtoCardapio",
                table: "ConfiguracoesLoja",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LogoAtualizadaEm",
                table: "Empresas");

            migrationBuilder.DropColumn(
                name: "LogoDados",
                table: "Empresas");

            migrationBuilder.DropColumn(
                name: "LogoNomeArquivo",
                table: "Empresas");

            migrationBuilder.DropColumn(
                name: "LogoTamanho",
                table: "Empresas");

            migrationBuilder.DropColumn(
                name: "LogoTipoConteudo",
                table: "Empresas");

            migrationBuilder.DropColumn(
                name: "AtendimentoMesasAtivo",
                table: "ConfiguracoesLoja");

            migrationBuilder.DropColumn(
                name: "CorDestaque",
                table: "ConfiguracoesLoja");

            migrationBuilder.DropColumn(
                name: "ExibirDescricao",
                table: "ConfiguracoesLoja");

            migrationBuilder.DropColumn(
                name: "ExibirLogo",
                table: "ConfiguracoesLoja");

            migrationBuilder.DropColumn(
                name: "ImpressaoAutomaticaCozinha",
                table: "ConfiguracoesLoja");

            migrationBuilder.DropColumn(
                name: "NomeExibicaoCardapio",
                table: "ConfiguracoesLoja");

            migrationBuilder.DropColumn(
                name: "TextoCurtoCardapio",
                table: "ConfiguracoesLoja");
        }
    }
}
