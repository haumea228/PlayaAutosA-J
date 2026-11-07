using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlayaAutos.API.Migrations
{
    /// <inheritdoc />
    public partial class FixTasacionKilometrajeType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "KilometrajeVehiculo",
                table: "TasacionesVehiculo",
                type: "numeric(10,0)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(15,2)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "KilometrajeVehiculo",
                table: "TasacionesVehiculo",
                type: "numeric(15,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(10,0)");
        }
    }
}
