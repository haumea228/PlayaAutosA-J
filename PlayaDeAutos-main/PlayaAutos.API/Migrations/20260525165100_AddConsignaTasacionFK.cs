using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlayaAutos.API.Migrations
{
    /// <inheritdoc />
    public partial class AddConsignaTasacionFK : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TasacionVehiculoId",
                table: "ContratosConsigna",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContratosConsigna_TasacionVehiculoId",
                table: "ContratosConsigna",
                column: "TasacionVehiculoId");

            migrationBuilder.AddForeignKey(
                name: "FK_ContratosConsigna_TasacionesVehiculo_TasacionVehiculoId",
                table: "ContratosConsigna",
                column: "TasacionVehiculoId",
                principalTable: "TasacionesVehiculo",
                principalColumn: "TasacionVehiculoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ContratosConsigna_TasacionesVehiculo_TasacionVehiculoId",
                table: "ContratosConsigna");

            migrationBuilder.DropIndex(
                name: "IX_ContratosConsigna_TasacionVehiculoId",
                table: "ContratosConsigna");

            migrationBuilder.DropColumn(
                name: "TasacionVehiculoId",
                table: "ContratosConsigna");
        }
    }
}
