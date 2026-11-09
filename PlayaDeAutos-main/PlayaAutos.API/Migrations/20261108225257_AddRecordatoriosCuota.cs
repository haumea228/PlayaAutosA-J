using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlayaAutos.API.Migrations
{
    /// <inheritdoc />
    public partial class AddRecordatoriosCuota : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "RecordatorioPrevioEnviado",
                table: "Cuotas",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RecordatorioVencimientoEnviado",
                table: "Cuotas",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RecordatorioPrevioEnviado",
                table: "Cuotas");

            migrationBuilder.DropColumn(
                name: "RecordatorioVencimientoEnviado",
                table: "Cuotas");
        }
    }
}
