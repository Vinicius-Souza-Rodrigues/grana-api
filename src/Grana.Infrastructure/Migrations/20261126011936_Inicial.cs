using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Grana.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "categorias",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nome = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    tipo_categoria = table.Column<int>(type: "integer", nullable: false),
                    orcamento_mensal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_categorias", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "contas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nome = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    tipo_conta = table.Column<int>(type: "integer", nullable: false),
                    saldo = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    saldo_inicial = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    status_atividade_conta = table.Column<int>(type: "integer", nullable: false),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_contas", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "lancamentos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    id_conta = table.Column<Guid>(type: "uuid", nullable: false),
                    id_categoria = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo_lancamento = table.Column<int>(type: "integer", nullable: false),
                    valor = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    data = table.Column<DateOnly>(type: "date", nullable: false),
                    descricao = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_lancamentos", x => x.id);
                    table.ForeignKey(
                        name: "fk_lancamentos_categorias_id_categoria",
                        column: x => x.id_categoria,
                        principalTable: "categorias",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_lancamentos_contas_id_conta",
                        column: x => x.id_conta,
                        principalTable: "contas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "transferencias",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    id_conta_origem = table.Column<Guid>(type: "uuid", nullable: false),
                    id_conta_destino = table.Column<Guid>(type: "uuid", nullable: false),
                    valor = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    data = table.Column<DateOnly>(type: "date", nullable: false),
                    descricao = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_transferencias", x => x.id);
                    table.ForeignKey(
                        name: "fk_transferencias_contas_id_conta_destino",
                        column: x => x.id_conta_destino,
                        principalTable: "contas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_transferencias_contas_id_conta_origem",
                        column: x => x.id_conta_origem,
                        principalTable: "contas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_lancamentos_id_categoria",
                table: "lancamentos",
                column: "id_categoria");

            migrationBuilder.CreateIndex(
                name: "ix_lancamentos_id_conta",
                table: "lancamentos",
                column: "id_conta");

            migrationBuilder.CreateIndex(
                name: "ix_transferencias_id_conta_destino",
                table: "transferencias",
                column: "id_conta_destino");

            migrationBuilder.CreateIndex(
                name: "ix_transferencias_id_conta_origem",
                table: "transferencias",
                column: "id_conta_origem");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "lancamentos");

            migrationBuilder.DropTable(
                name: "transferencias");

            migrationBuilder.DropTable(
                name: "categorias");

            migrationBuilder.DropTable(
                name: "contas");
        }
    }
}
