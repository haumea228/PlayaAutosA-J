using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlayaAutos.API.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Agrega las columnas de inventario a TasacionesVehiculo
            // (el esquema base fue creado por migraciones anteriores)
            migrationBuilder.AddColumn<int>(
                name: "CondicionId",
                table: "TasacionesVehiculo",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ModeloId",
                table: "TasacionesVehiculo",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OrigenId",
                table: "TasacionesVehiculo",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TipoId",
                table: "TasacionesVehiculo",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VehiculoId",
                table: "TasacionesVehiculo",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TasacionesVehiculo_VehiculoId",
                table: "TasacionesVehiculo",
                column: "VehiculoId");

            migrationBuilder.AddForeignKey(
                name: "FK_TasacionesVehiculo_Vehiculos_VehiculoId",
                table: "TasacionesVehiculo",
                column: "VehiculoId",
                principalTable: "Vehiculos",
                principalColumn: "VehiculoId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TasacionesVehiculo_Vehiculos_VehiculoId",
                table: "TasacionesVehiculo");

            migrationBuilder.DropIndex(
                name: "IX_TasacionesVehiculo_VehiculoId",
                table: "TasacionesVehiculo");

            migrationBuilder.DropColumn(name: "CondicionId", table: "TasacionesVehiculo");
            migrationBuilder.DropColumn(name: "ModeloId", table: "TasacionesVehiculo");
            migrationBuilder.DropColumn(name: "OrigenId", table: "TasacionesVehiculo");
            migrationBuilder.DropColumn(name: "TipoId", table: "TasacionesVehiculo");
            migrationBuilder.DropColumn(name: "VehiculoId", table: "TasacionesVehiculo");
        }
    }
}