using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Grana.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class IndicesConcorrencia : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "versao",
                table: "contas",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "ix_lancamentos_data",
                table: "lancamentos",
                column: "data");

            migrationBuilder.CreateIndex(
                name: "ix_contas_nome",
                table: "contas",
                column: "nome",
                unique: true,
                filter: "status_atividade_conta = 1");

            migrationBuilder.CreateIndex(
                name: "ix_categorias_tipo_categoria_nome",
                table: "categorias",
                columns: new[] { "tipo_categoria", "nome" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_lancamentos_data",
                table: "lancamentos");

            migrationBuilder.DropIndex(
                name: "ix_contas_nome",
                table: "contas");

            migrationBuilder.DropIndex(
                name: "ix_categorias_tipo_categoria_nome",
                table: "categorias");

            migrationBuilder.DropColumn(
                name: "versao",
                table: "contas");
        }
    }
}
