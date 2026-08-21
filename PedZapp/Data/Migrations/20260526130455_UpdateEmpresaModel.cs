using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PedZapp.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateEmpresaModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Bairro",
                table: "Empresas",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CEP",
                table: "Empresas",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Cidade",
                table: "Empresas",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DataExpiracaoPlano",
                table: "Empresas",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Descricao",
                table: "Empresas",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Endereco",
                table: "Empresas",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Estado",
                table: "Empresas",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Numero",
                table: "Empresas",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "PlanoAtivo",
                table: "Empresas",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Bairro",
                table: "Empresas");

            migrationBuilder.DropColumn(
                name: "CEP",
                table: "Empresas");

            migrationBuilder.DropColumn(
                name: "Cidade",
                table: "Empresas");

            migrationBuilder.DropColumn(
                name: "DataExpiracaoPlano",
                table: "Empresas");

            migrationBuilder.DropColumn(
                name: "Descricao",
                table: "Empresas");

            migrationBuilder.DropColumn(
                name: "Endereco",
                table: "Empresas");

            migrationBuilder.DropColumn(
                name: "Estado",
                table: "Empresas");

            migrationBuilder.DropColumn(
                name: "Numero",
                table: "Empresas");

            migrationBuilder.DropColumn(
                name: "PlanoAtivo",
                table: "Empresas");
        }
    }
}
