using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlayaAutos.API.Migrations
{
    /// <summary>
    /// Pagos de cuotas informados por el cliente desde la app.
    ///
    /// NOTA: el snapshot anterior estaba desfasado respecto de la base real
    /// (Ventas.PorcentajeRecargo, MovimientosCaja.CierreId y la tabla CierreCaja
    /// se crearon fuera de las migraciones). Se agregan acá de forma idempotente
    /// (solo si no existen): no rompe las bases que ya los tienen y deja
    /// completas las bases creadas desde cero. El snapshot queda sincronizado.
    /// </summary>
    public partial class AddSolicitudesPagoCuota : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── Sincronización del desfase previo (idempotente) ──────────────
            migrationBuilder.Sql(@"
IF COL_LENGTH('Ventas', 'PorcentajeRecargo') IS NULL
    ALTER TABLE [Ventas] ADD [PorcentajeRecargo] decimal(18,2) NULL;

IF OBJECT_ID(N'[CierreCaja]', N'U') IS NULL
BEGIN
    CREATE TABLE [CierreCaja] (
        [CierreId] int NOT NULL IDENTITY(1,1),
        [FechaCierre] datetime2 NOT NULL,
        [UsuarioCierre] int NOT NULL,
        [TotalIngresos] bigint NOT NULL,
        [TotalEgresos] bigint NOT NULL,
        [SaldoNeto] bigint NOT NULL,
        [CantidadMovimientos] int NOT NULL,
        [Observacion] nvarchar(500) NULL,
        CONSTRAINT [PK_CierreCaja] PRIMARY KEY ([CierreId]),
        CONSTRAINT [FK_CierreCaja_Usuarios_UsuarioCierre] FOREIGN KEY ([UsuarioCierre])
            REFERENCES [Usuarios] ([UsuarioId])
    );
    CREATE INDEX [IX_CierreCaja_UsuarioCierre] ON [CierreCaja] ([UsuarioCierre]);
END;

IF COL_LENGTH('MovimientosCaja', 'CierreId') IS NULL
BEGIN
    ALTER TABLE [MovimientosCaja] ADD [CierreId] int NULL;
END;
");
            // En un batch aparte: la columna recién agregada no es visible
            // dentro del mismo batch en el que se creó.
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_MovimientosCaja_CierreId')
    CREATE INDEX [IX_MovimientosCaja_CierreId] ON [MovimientosCaja] ([CierreId]);

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE parent_object_id = OBJECT_ID(N'[MovimientosCaja]')
               AND referenced_object_id = OBJECT_ID(N'[CierreCaja]'))
    ALTER TABLE [MovimientosCaja] ADD CONSTRAINT [FK_MovimientosCaja_CierreCaja_CierreId]
        FOREIGN KEY ([CierreId]) REFERENCES [CierreCaja] ([CierreId]);
");

            // ── Pagos de cuotas informados desde la app ──────────────────────
            migrationBuilder.CreateTable(
                name: "SolicitudesPagoCuota",
                columns: table => new
                {
                    SolicitudPagoCuotaId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CuotaId = table.Column<int>(type: "int", nullable: false),
                    ClienteId = table.Column<int>(type: "int", nullable: false),
                    FechaReporte = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Monto = table.Column<decimal>(type: "numeric(15,0)", nullable: false),
                    MontoRecargo = table.Column<decimal>(type: "numeric(15,0)", nullable: false),
                    ComprobanteArchivo = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    ComprobanteContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ObservacionCliente = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    MotivoRechazo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    UsuarioRevisionId = table.Column<int>(type: "int", nullable: true),
                    FechaRevision = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SolicitudesPagoCuota", x => x.SolicitudPagoCuotaId);
                    table.ForeignKey(
                        name: "FK_SolicitudesPagoCuota_Clientes_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Clientes",
                        principalColumn: "ClienteId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SolicitudesPagoCuota_Cuotas_CuotaId",
                        column: x => x.CuotaId,
                        principalTable: "Cuotas",
                        principalColumn: "CuotaId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SolicitudesPagoCuota_Usuarios_UsuarioRevisionId",
                        column: x => x.UsuarioRevisionId,
                        principalTable: "Usuarios",
                        principalColumn: "UsuarioId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesPagoCuota_ClienteId",
                table: "SolicitudesPagoCuota",
                column: "ClienteId");

            // Una sola solicitud abierta por cuota.
            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesPagoCuota_CuotaId_Pendiente",
                table: "SolicitudesPagoCuota",
                column: "CuotaId",
                unique: true,
                filter: "[Estado] = 'Pendiente'");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesPagoCuota_Estado",
                table: "SolicitudesPagoCuota",
                column: "Estado");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesPagoCuota_UsuarioRevisionId",
                table: "SolicitudesPagoCuota",
                column: "UsuarioRevisionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SolicitudesPagoCuota");
        }
    }
}
