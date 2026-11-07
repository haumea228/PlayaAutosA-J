using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PlayaAutos.API.Services
{
    public class TicketCuotaData
    {
        public int CuotaId { get; set; }
        public int NumeroCuota { get; set; }
        public int CantidadCuotas { get; set; }
        public string Cliente { get; set; } = "";
        public string Vehiculo { get; set; } = "";
        public string FormaPago { get; set; } = "";
        public string? Observacion { get; set; }
        public DateTime FechaPago { get; set; }
        public long SaldoInicial { get; set; }
        public long SaldoAnterior { get; set; }
        public long Abono { get; set; }
        public long Recargo { get; set; }
        public long SaldoActual { get; set; }
        public string UsuarioRegistro { get; set; } = "";
        public string CI_RUC { get; set; } = ""; 
    }

    public static class TicketCuotaPdfService
    {
        private const string ColorVerde = "#1B6B3A";
        private const string ColorVerdeClaro = "#D4EDDA";
        private const string ColorTexto = "#1a1a1a";

        public static byte[] Generar(TicketCuotaData d)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    // Formato ticket: 80mm de ancho
                    page.Size(226, 390, Unit.Point);
                    page.Margin(10, Unit.Point);
                    page.DefaultTextStyle(x => x.FontSize(9f).FontColor(ColorTexto));

                    page.Content().Column(col =>
                    {
                        // ── LOGO / ENCABEZADO ─────────────────────────
                        var logoPath = Path.Combine(
                            AppDomain.CurrentDomain.BaseDirectory, "wwwroot", "images", "logo.png");

                        col.Item().AlignCenter().Column(header =>
                        {
                            if (File.Exists(logoPath))
                            {
                                header.Item().AlignCenter()
                                      .Width(70).Image(logoPath, ImageScaling.FitWidth);
                                header.Item().Height(4);
                            }
                            else
                            {
                                header.Item().AlignCenter()
                                      .Text("A&J AUTOS USADOS")
                                      .Bold().FontSize(13f).FontColor(ColorVerde);
                            }

                            header.Item().AlignCenter()
                                  .Text("COMPROBANTE DE PAGO DE CUOTA")
                                  .Bold().FontSize(8.5f);

                            header.Item().Height(3);

                            header.Item().AlignCenter()
                                  .Text($"CUOTA: {d.NumeroCuota}/{d.CantidadCuotas}")
                                  .Bold().FontSize(10f).FontColor(ColorVerde);

                            header.Item().Height(2);

                            header.Item().AlignCenter()
                                  .Text($"CLIENTE: {d.Cliente.ToUpper()}")
                                  .FontSize(8f);

                            if (!string.IsNullOrWhiteSpace(d.CI_RUC))
                            {
                                header.Item().AlignCenter()
                                      .Text($"CI/RUC: {d.CI_RUC}")
                                      .FontSize(7.5f).FontColor("#555555");
                            }
                        });

                        col.Item().Height(6);

                        // ── SEPARADOR ─────────────────────────────────
                        col.Item().AlignCenter()
                           .Text("════════════════════════")
                           .FontSize(8f).FontColor("#888888");

                        col.Item().Height(4);

                        // ── BLOQUE DE SALDOS ──────────────────────────
                        col.Item().Border(1).BorderColor(ColorVerde).Column(saldos =>
                        {
                            FilaSaldo(saldos, "SALDO INICIAL", d.SaldoInicial, false);
                            FilaSaldo(saldos, "SALDO ANTERIOR", d.SaldoAnterior, false);
                            FilaSaldo(saldos, "ABONO", d.Abono, false);
                            if (d.Recargo > 0)
                                FilaSaldo(saldos, "RECARGO", d.Recargo, false);
                            FilaSaldo(saldos, "SALDO ACTUAL", d.SaldoActual, true);
                        });

                        col.Item().Height(6);

                        // ── DATOS DEL PAGO ────────────────────────────
                        col.Item().Column(datos =>
                        {
                            FilaDato(datos, "VEHÍCULO", d.Vehiculo.ToUpper());
                            FilaDato(datos, "FORMA DE PAGO", d.FormaPago.ToUpper());
                            if (!string.IsNullOrWhiteSpace(d.Observacion))
                                FilaDato(datos, "OBSERVACIÓN", d.Observacion.ToUpper());
                            FilaDato(datos, "FECHA Y HORA",
                                d.FechaPago.ToString("yyyy/MM/dd") + " - " + d.FechaPago.ToString("hh:mm:ss tt"));
                        });

                        col.Item().Height(8);

                        // ── SEPARADOR / USUARIO ───────────────────────
                        col.Item().AlignCenter()
                           .Text("════════════════════════")
                           .FontSize(8f).FontColor("#888888");

                        col.Item().Height(4);

                        col.Item().AlignCenter()
                           .Text(d.UsuarioRegistro.ToUpper())
                           .FontSize(8f).FontColor("#555555");

                        col.Item().Height(4);

                        col.Item().AlignCenter()
                           .Text("════════════════════════")
                           .FontSize(8f).FontColor("#888888");
                    });
                });
            }).GeneratePdf();
        }

        private static void FilaSaldo(ColumnDescriptor col, string etiqueta, long monto, bool resaltar)
        {
            col.Item()
               .Background(resaltar ? ColorVerdeClaro : Colors.White)
               .BorderBottom(1).BorderColor(ColorVerde)
               .Padding(5)
               .Row(row =>
               {
                   row.RelativeItem()
                      .Text(etiqueta + ":")
                      .Bold().FontSize(8.5f)
                      .FontColor(resaltar ? ColorVerde : ColorTexto);

                   row.ConstantItem(90)
                      .AlignRight()
                      .Text($"Gs. {monto:N0}")
                      .Bold().FontSize(8.5f)
                      .FontColor(resaltar ? ColorVerde : ColorTexto);
               });
        }

        private static void FilaDato(ColumnDescriptor col, string etiqueta, string valor)
        {
            col.Item().PaddingBottom(3).Row(row =>
            {
                row.ConstantItem(70)
                   .Text(etiqueta + ":")
                   .Bold().FontSize(7.5f).FontColor("#555555");

                row.RelativeItem()
                   .Text(valor)
                   .FontSize(7.5f);
            });
        }
    }
}