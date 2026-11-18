using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AguaNaRua.Api.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PontosMonitorados",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nome = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    Bairro = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Latitude = table.Column<double>(type: "REAL", nullable: false),
                    Longitude = table.Column<double>(type: "REAL", nullable: false),
                    Demonstracao = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PontosMonitorados", x => x.Id);
                    table.CheckConstraint("CK_Ponto_Latitude", "Latitude BETWEEN -90 AND 90");
                    table.CheckConstraint("CK_Ponto_Longitude", "Longitude BETWEEN -180 AND 180");
                });

            migrationBuilder.CreateTable(
                name: "Observacoes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PontoMonitoradoId = table.Column<int>(type: "INTEGER", nullable: false),
                    Condicao = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Origem = table.Column<string>(type: "TEXT", maxLength: 24, nullable: false),
                    ObservadoEm = table.Column<long>(type: "INTEGER", nullable: false),
                    RegistradoEm = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Observacoes", x => x.Id);
                    table.CheckConstraint("CK_Observacao_Condicao", "Condicao IN ('SEM_AGUA_ACUMULADA_OBSERVADA', 'AGUA_ACUMULADA', 'ALAGAMENTO_RELATADO')");
                    table.CheckConstraint("CK_Observacao_Horarios", "ObservadoEm <= RegistradoEm");
                    table.CheckConstraint("CK_Observacao_Origem", "Origem IN ('RELATO_CIDADAO', 'DEMONSTRACAO')");
                    table.ForeignKey(
                        name: "FK_Observacoes_PontosMonitorados_PontoMonitoradoId",
                        column: x => x.PontoMonitoradoId,
                        principalTable: "PontosMonitorados",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Observacoes_PontoMonitoradoId_ObservadoEm_RegistradoEm_Id",
                table: "Observacoes",
                columns: new[] { "PontoMonitoradoId", "ObservadoEm", "RegistradoEm", "Id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Observacoes");

            migrationBuilder.DropTable(
                name: "PontosMonitorados");
        }
    }
}
