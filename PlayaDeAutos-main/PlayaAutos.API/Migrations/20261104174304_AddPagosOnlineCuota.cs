using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlayaAutos.API.Migrations
{
    /// <inheritdoc />
    public partial class AddPagosOnlineCuota : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PagosOnlineCuota",
                columns: table => new
                {
                    PagoOnlineCuotaId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CuotaId = table.Column<int>(type: "int", nullable: false),
                    ClienteId = table.Column<int>(type: "int", nullable: false),
                    Proveedor = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SesionId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    PagoProveedorId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    UrlPago = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Monto = table.Column<decimal>(type: "numeric(15,0)", nullable: false),
                    MontoRecargo = table.Column<decimal>(type: "numeric(15,0)", nullable: false),
                    Moneda = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaExpiracion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaConfirmacion = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PagosOnlineCuota", x => x.PagoOnlineCuotaId);
                    table.ForeignKey(
                        name: "FK_PagosOnlineCuota_Clientes_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Clientes",
                        principalColumn: "ClienteId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PagosOnlineCuota_Cuotas_CuotaId",
                        column: x => x.CuotaId,
                        principalTable: "Cuotas",
                        principalColumn: "CuotaId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PagosOnlineCuota_ClienteId",
                table: "PagosOnlineCuota",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_PagosOnlineCuota_CuotaId_Estado",
                table: "PagosOnlineCuota",
                columns: new[] { "CuotaId", "Estado" });

            migrationBuilder.CreateIndex(
                name: "IX_PagosOnlineCuota_SesionId",
                table: "PagosOnlineCuota",
                column: "SesionId",
                unique: true);

            // Forma de pago para los cobros con la pasarela (distingue en caja y
            // reportes el pago con tarjeta online del cobrado en el local).
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM [FormasPago] WHERE [Descripcion] = N'Tarjeta online (Stripe)')
    INSERT INTO [FormasPago] ([Descripcion], [GeneraNotaCredito]) VALUES (N'Tarjeta online (Stripe)', 0);
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PagosOnlineCuota");
        }
    }
}
