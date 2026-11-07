using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Globalization;

namespace PlayaAutos.API.Services
{
    public class FacturaPdfDataDto
    {
        public string NumeroFactura { get; set; } = "";
        public DateTime FechaEmision { get; set; }

        // Timbrado
        public string NumeroTimbrado { get; set; } = "";
        public DateTime TimbradoVigenciaDesde { get; set; }
        public DateTime TimbradoVigenciaHasta { get; set; }

        // Cliente
        public string ClienteNombre { get; set; } = "";
        public string ClienteRUC { get; set; } = "";
        public string? ClienteDireccion { get; set; }
        public string? ClienteTelefono { get; set; }

        // Vehículo
        public string VehiculoMarca { get; set; } = "";
        public string VehiculoModelo { get; set; } = "";
        public string VehiculoTipo { get; set; } = "";
        public string VehiculoCondicion { get; set; } = "";
        public int VehiculoAnio { get; set; }
        public string? VehiculoColor { get; set; }
        public string? VehiculoDescripcion { get; set; }

        // Venta
        public string FormaPago { get; set; } = "";

        // Montos
        public long Subtotal { get; set; }
        public long IVA { get; set; }
        public long Total { get; set; }
    }

    public static class FacturaPdfService
    {
        private const string EmpresaNombre    = "PLAYA DE AUTOS";
        private const string EmpresaSubtitulo = "\"COMISIONISTA EN VENTA DE VEHÍCULOS USADOS\"";
        private const string EmpresaRUC       = "80012345-0";
        private const string EmpresaDireccion = "Av. Principal N° 123, Asunción, Paraguay";
        private const string EmpresaTelefono  = "Tel. 021-123-456";

        private const string ColorAmarillo = "#FFD100";
        private const string ColorNegro    = "#1a1a1a";
        private const string ColorBorde    = "#999999";

        public static byte[] Generar(FacturaPdfDataDto d)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var culturaEs = new CultureInfo("es-PY");

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(14, Unit.Point);
                    page.DefaultTextStyle(x => x.FontSize(8.5f).FontColor(ColorNegro));

                    page.Content().Column(col =>
                    {
                        // ── ENCABEZADO ──────────────────────────────────
                        col.Item().Row(row =>
                        {
                            row.RelativeItem(3).Padding(4).Column(emp =>
                            {
                                emp.Item().Text(EmpresaNombre)
                                   .Bold().FontSize(22);

                                emp.Item().Text(EmpresaSubtitulo)
                                   .Italic().FontSize(8f);

                                emp.Item().PaddingTop(4).Text(t =>
                                {
                                    t.Span("RUC: ").Bold();
                                    t.Span(EmpresaRUC);
                                });

                                emp.Item().Text(EmpresaDireccion).FontSize(7.5f);
                                emp.Item().Text(EmpresaTelefono).FontSize(7.5f);
                            });

                            row.ConstantItem(170)
                               .Border(2).BorderColor(ColorNegro)
                               .Column(fac =>
                               {
                                   fac.Item()
                                      .Background(ColorNegro)
                                      .PaddingVertical(5).PaddingHorizontal(10)
                                      .AlignCenter()
                                      .Text("FACTURA")
                                      .FontColor(Colors.White).Bold().FontSize(12);

                                   fac.Item()
                                      .PaddingVertical(10)
                                      .AlignCenter()
                                      .Text($"N° {d.NumeroFactura}")
                                      .Bold().FontSize(15);
                               });
                        });

                        col.Item().Height(6);

                        // ── DATOS DEL CLIENTE ────────────────────────────
                        col.Item().Border(1).BorderColor(ColorBorde).Column(cli =>
                        {
                            // Fila: NOMBRE + FECHA (etiqueta)
                            cli.Item().Row(row =>
                            {
                                row.RelativeItem()
                                   .Background(ColorAmarillo)
                                   .BorderBottom(1).BorderColor(ColorBorde)
                                   .Padding(4)
                                   .Text(t =>
                                   {
                                       t.Span("NOMBRE: ").Bold();
                                       t.Span(d.ClienteNombre.ToUpper());
                                   });

                                row.ConstantItem(130)
                                   .Background(ColorAmarillo)
                                   .Border(1).BorderColor(ColorBorde)
                                   .Padding(4).AlignCenter()
                                   .Text("FECHA").Bold();
                            });

                            // Fila: DIRECCIÓN + FECHA (valor)
                            cli.Item().Row(row =>
                            {
                                row.RelativeItem()
                                   .BorderBottom(1).BorderColor(ColorBorde)
                                   .Padding(4)
                                   .Text(t =>
                                   {
                                       t.Span("DIRECCIÓN: ").Bold();
                                       t.Span((d.ClienteDireccion ?? "").ToUpper());
                                   });

                                row.ConstantItem(130)
                                   .Border(1).BorderColor(ColorBorde)
                                   .Padding(4).AlignCenter()
                                   .Text(FechaLarga(d.FechaEmision)).Bold().FontSize(8f);
                            });

                            // Fila: CI/RUC + TELÉFONO
                            cli.Item()
                               .Background("#F5F5F5")
                               .Padding(4)
                               .Text(t =>
                               {
                                   t.Span("CI / RUC: ").Bold();
                                   t.Span(d.ClienteRUC);
                                   if (!string.IsNullOrEmpty(d.ClienteTelefono))
                                   {
                                       t.Span("          TEL: ").Bold();
                                       t.Span(d.ClienteTelefono);
                                   }
                               });
                        });

                        col.Item().Height(6);

                        // ── TABLA DE ARTÍCULOS ───────────────────────────
                        col.Item().Border(1).BorderColor(ColorBorde).Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                cols.ConstantColumn(38);   // CANT.
                                cols.RelativeColumn();      // DESCRIPCIÓN
                                cols.ConstantColumn(82);   // P. UNIT.
                                cols.ConstantColumn(90);   // IMPORTE
                            });

                            table.Header(h =>
                            {
                                static IContainer Th(IContainer c) =>
                                    c.Background(ColorAmarillo)
                                     .BorderBottom(2).BorderColor(ColorNegro)
                                     .Padding(4).AlignCenter();

                                h.Cell().Element(Th).Text("CANT.").Bold();
                                h.Cell().Element(Th).Text("DESCRIPCIÓN").Bold();
                                h.Cell().Element(Th).Text("PRECIO UNITARIO").Bold();
                                h.Cell().Element(Th).Text("IMPORTE").Bold();
                            });

                            // Celda: CANTIDAD
                            table.Cell()
                                 .BorderRight(1).BorderColor(ColorBorde)
                                 .MinHeight(120)
                                 .Padding(4).AlignCenter()
                                 .Text("1").Bold();

                            // Celda: DESCRIPCIÓN del vehículo
                            var articulo = EsArticuloFemenino(d.VehiculoTipo) ? "UNA" : "UN";
                            table.Cell()
                                 .BorderRight(1).BorderColor(ColorBorde)
                                 .Padding(6)
                                 .Column(desc =>
                                 {
                                     desc.Item().Text(t =>
                                     {
                                         t.Span($"{articulo} {d.VehiculoTipo.ToUpper()} MARCA ").Bold();
                                         t.Span(d.VehiculoMarca.ToUpper()).Bold();
                                         t.Span(" MODELO ").Bold();
                                         t.Span(d.VehiculoModelo.ToUpper()).Bold();
                                         t.Span($" AÑO {d.VehiculoAnio}").Bold();
                                     });

                                     if (!string.IsNullOrEmpty(d.VehiculoColor))
                                         desc.Item().PaddingTop(3)
                                             .Text($"COLOR: {d.VehiculoColor.ToUpper()}");

                                     if (!string.IsNullOrWhiteSpace(d.VehiculoDescripcion))
                                         desc.Item().PaddingTop(3)
                                             .Text(d.VehiculoDescripcion.ToUpper())
                                             .FontSize(8f);

                                     desc.Item().PaddingTop(10)
                                         .Text($"VEHÍCULO {d.VehiculoCondicion.ToUpper()} Y EN EL ESTADO QUE SE ENCUENTRA, SIN GARANTÍA.")
                                         .Italic().FontSize(8f);
                                 });

                            // Celda: PRECIO UNITARIO (vacío)
                            table.Cell()
                                 .BorderRight(1).BorderColor(ColorBorde)
                                 .Padding(4).AlignRight()
                                 .Text("");

                            // Celda: IMPORTE
                            table.Cell()
                                 .Padding(4).AlignRight()
                                 .Text($"Gs. {d.Total:N0}").Bold();
                        });

                        col.Item().Height(6);

                        // ── PIE: CANTIDAD CON LETRAS + TOTALES ───────────
                        col.Item().Border(1).BorderColor(ColorBorde).Row(pie =>
                        {
                            pie.RelativeItem(2)
                               .BorderRight(1).BorderColor(ColorBorde)
                               .Padding(7)
                               .Column(lc =>
                               {
                                   lc.Item().Text("CANTIDAD CON LETRA:").Bold();
                                   lc.Item().PaddingTop(3)
                                            .Text($"({NumeroALetras(d.Total)} GUARANÍES---00/100 M.N.)")
                                            .FontSize(8f);
                                   lc.Item().PaddingTop(10).Text(t =>
                                   {
                                       t.Span("FORMA DE PAGO: ").Bold();
                                       t.Span(d.FormaPago.ToUpper());
                                   });
                               });

                            pie.RelativeItem().Column(tot =>
                            {
                                FilaTotales(tot, "SUB-TOTAL:", $"Gs. {d.Subtotal:N0}", false);
                                FilaTotales(tot, "IVA (10%):", $"Gs. {d.IVA:N0}", false);
                                FilaTotales(tot, "TOTAL:", $"Gs. {d.Total:N0}", true);
                            });
                        });

                        col.Item().Height(6);

                        // ── DATOS DEL TIMBRADO ───────────────────────────
                        col.Item().Border(1).BorderColor(ColorBorde)
                           .Background("#F9F9F9")
                           .Padding(7)
                           .Column(fis =>
                           {
                               fis.Item().Text(t =>
                               {
                                   t.Span("TIMBRADO N°: ").Bold();
                                   t.Span(d.NumeroTimbrado);
                                   t.Span("          VIGENCIA: ").Bold();
                                   t.Span($"{d.TimbradoVigenciaDesde:dd/MM/yyyy} AL {d.TimbradoVigenciaHasta:dd/MM/yyyy}");
                               });

                               fis.Item().PaddingTop(3).Text(t =>
                               {
                                   t.Span("N° COMPROBANTE: ").Bold();
                                   t.Span(d.NumeroFactura);
                                   t.Span("          FECHA DE EMISIÓN: ").Bold();
                                   t.Span(d.FechaEmision.ToString("dd/MM/yyyy"));
                               });
                           });

                        // ── SLOGAN ───────────────────────────────────────
                        col.Item().PaddingTop(10).AlignCenter()
                           .Text("¡Tenemos el vehículo adecuado a sus necesidades!")
                           .Bold().Italic().FontSize(11f);
                    });
                });
            }).GeneratePdf();
        }

        private static void FilaTotales(ColumnDescriptor col, string etiqueta, string valor, bool resaltar)
        {
            col.Item()
               .Background(resaltar ? ColorAmarillo : Colors.White)
               .BorderTop(1).BorderColor(ColorBorde)
               .Row(r =>
               {
                   r.RelativeItem().Padding(4).Text(etiqueta).Bold();

                   if (resaltar)
                       r.RelativeItem().Padding(4).AlignRight().Text(valor).Bold();
                   else
                       r.RelativeItem().Padding(4).AlignRight().Text(valor);
               });
        }

        private static string FechaLarga(DateTime fecha)
        {
            var meses = new[] {
                "", "ENERO", "FEBRERO", "MARZO", "ABRIL", "MAYO", "JUNIO",
                "JULIO", "AGOSTO", "SEPTIEMBRE", "OCTUBRE", "NOVIEMBRE", "DICIEMBRE"
            };
            return $"{fecha.Day} DE {meses[fecha.Month]} DE {fecha.Year}";
        }

        private static bool EsArticuloFemenino(string tipo) =>
            tipo.ToUpper() is "CAMIONETA" or "MOTO" or "MOTOCICLETA" or "VAN" or "FURGONETA";

        private static string NumeroALetras(long numero)
        {
            if (numero == 0) return "CERO";
            if (numero < 0) return "MENOS " + NumeroALetras(-numero);

            var partes = new List<string>();

            if (numero >= 1_000_000_000)
            {
                var m = numero / 1_000_000_000;
                partes.Add(m == 1 ? "MIL MILLONES" : NumeroALetras(m) + " MIL MILLONES");
                numero %= 1_000_000_000;
            }
            if (numero >= 1_000_000)
            {
                var m = numero / 1_000_000;
                partes.Add(m == 1 ? "UN MILLÓN" : NumeroALetras(m) + " MILLONES");
                numero %= 1_000_000;
            }
            if (numero >= 1_000)
            {
                var m = numero / 1_000;
                partes.Add(m == 1 ? "MIL" : NumeroALetras(m) + " MIL");
                numero %= 1_000;
            }
            if (numero > 0)
                partes.Add(MenosDeMil((int)numero));

            return string.Join(" ", partes);
        }

        private static string MenosDeMil(int n)
        {
            if (n == 0) return "";

            string[] unidades = {
                "", "UN", "DOS", "TRES", "CUATRO", "CINCO", "SEIS", "SIETE", "OCHO", "NUEVE",
                "DIEZ", "ONCE", "DOCE", "TRECE", "CATORCE", "QUINCE",
                "DIECISÉIS", "DIECISIETE", "DIECIOCHO", "DIECINUEVE"
            };
            string[] decenas = {
                "", "DIEZ", "VEINTE", "TREINTA", "CUARENTA", "CINCUENTA",
                "SESENTA", "SETENTA", "OCHENTA", "NOVENTA"
            };
            string[] centenas = {
                "", "CIENTO", "DOSCIENTOS", "TRESCIENTOS", "CUATROCIENTOS", "QUINIENTOS",
                "SEISCIENTOS", "SETECIENTOS", "OCHOCIENTOS", "NOVECIENTOS"
            };
            string[] veintis = {
                "VEINTE", "VEINTIÚN", "VEINTIDÓS", "VEINTITRÉS", "VEINTICUATRO",
                "VEINTICINCO", "VEINTISÉIS", "VEINTISIETE", "VEINTIOCHO", "VEINTINUEVE"
            };

            var sb = new System.Text.StringBuilder();

            if (n == 100) return "CIEN";

            if (n >= 100)
            {
                sb.Append(centenas[n / 100]);
                n %= 100;
                if (n > 0) sb.Append(' ');
            }

            if (n >= 20 && n < 30)
            {
                sb.Append(veintis[n - 20]);
            }
            else if (n >= 20)
            {
                sb.Append(decenas[n / 10]);
                if (n % 10 > 0)
                    sb.Append(" Y " + unidades[n % 10]);
            }
            else if (n > 0)
            {
                sb.Append(unidades[n]);
            }

            return sb.ToString();
        }
    }
}
