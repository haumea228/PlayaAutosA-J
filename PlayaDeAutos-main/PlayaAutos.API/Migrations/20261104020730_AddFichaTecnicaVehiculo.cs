using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlayaAutos.API.Migrations
{
    /// <inheritdoc />
    public partial class AddFichaTecnicaVehiculo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CantidadPuertas",
                table: "Vehiculos",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Combustible",
                table: "Vehiculos",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Motor",
                table: "Vehiculos",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Traccion",
                table: "Vehiculos",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Transmision",
                table: "Vehiculos",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CantidadPuertas",
                table: "Vehiculos");

            migrationBuilder.DropColumn(
                name: "Combustible",
                table: "Vehiculos");

            migrationBuilder.DropColumn(
                name: "Motor",
                table: "Vehiculos");

            migrationBuilder.DropColumn(
                name: "Traccion",
                table: "Vehiculos");

            migrationBuilder.DropColumn(
                name: "Transmision",
                table: "Vehiculos");
        }
    }
}
