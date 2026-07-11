using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Oficina.Cadastro.Infrastructure.Persistencia;

#nullable disable

namespace Oficina.Cadastro.Infrastructure.Migrations;

[DbContext(typeof(CadastroDbContext))]
[Migration("20260710000000_InitialCadastroCreate")]
public partial class InitialCadastroCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Clientes",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Nome = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                Documento = table.Column<string>(type: "nvarchar(14)", maxLength: 14, nullable: false),
                ContatoEmail = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                ContatoTelefone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_Clientes", x => x.Id));

        migrationBuilder.CreateTable(
            name: "Funcionarios",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Nome = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                Cpf = table.Column<string>(type: "nvarchar(11)", maxLength: 11, nullable: false),
                SenhaHash = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                Perfil = table.Column<int>(type: "int", nullable: false),
                Ativo = table.Column<bool>(type: "bit", nullable: false),
                DataCriacao = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_Funcionarios", x => x.Id));

        migrationBuilder.CreateTable(
            name: "Servicos",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                MaoDeObra = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_Servicos", x => x.Id));

        migrationBuilder.CreateTable(
            name: "Veiculos",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ClienteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Placa = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: false),
                Renavam = table.Column<string>(type: "nvarchar(11)", maxLength: 11, nullable: false),
                ModeloDescricao = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                ModeloMarca = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                ModeloAno = table.Column<int>(type: "int", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Veiculos", x => x.Id);
                table.ForeignKey(
                    name: "FK_Veiculos_Clientes_ClienteId",
                    column: x => x.ClienteId,
                    principalTable: "Clientes",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "ServicoInsumosRequeridos",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ServicoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                InsumoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Quantidade = table.Column<int>(type: "int", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ServicoInsumosRequeridos", x => x.Id);
                table.ForeignKey(
                    name: "FK_ServicoInsumosRequeridos_Servicos_ServicoId",
                    column: x => x.ServicoId,
                    principalTable: "Servicos",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "ServicoPecasRequeridas",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ServicoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                PecaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Quantidade = table.Column<int>(type: "int", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ServicoPecasRequeridas", x => x.Id);
                table.ForeignKey(
                    name: "FK_ServicoPecasRequeridas_Servicos_ServicoId",
                    column: x => x.ServicoId,
                    principalTable: "Servicos",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex("IX_Clientes_Documento", "Clientes", "Documento", unique: true);
        migrationBuilder.CreateIndex("IX_Funcionarios_Cpf", "Funcionarios", "Cpf", unique: true);
        migrationBuilder.CreateIndex("IX_Veiculos_ClienteId", "Veiculos", "ClienteId");
        migrationBuilder.CreateIndex("IX_Veiculos_Placa", "Veiculos", "Placa", unique: true);
        migrationBuilder.CreateIndex("IX_Veiculos_Renavam", "Veiculos", "Renavam", unique: true);
        migrationBuilder.CreateIndex("IX_ServicoInsumosRequeridos_ServicoId", "ServicoInsumosRequeridos", "ServicoId");
        migrationBuilder.CreateIndex("IX_ServicoPecasRequeridas_ServicoId", "ServicoPecasRequeridas", "ServicoId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("ServicoInsumosRequeridos");
        migrationBuilder.DropTable("ServicoPecasRequeridas");
        migrationBuilder.DropTable("Veiculos");
        migrationBuilder.DropTable("Funcionarios");
        migrationBuilder.DropTable("Servicos");
        migrationBuilder.DropTable("Clientes");
    }
}
