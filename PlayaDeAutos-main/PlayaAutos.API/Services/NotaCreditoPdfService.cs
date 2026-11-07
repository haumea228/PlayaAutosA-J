using PlayaAutos.API.DTOs;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Globalization;

namespace PlayaAutos.API.Services
{
    public static class NotaCreditoPdfService
    {
        private const string EmpresaNombre    = "A&J AUTOS USADOS";
        private const string EmpresaSubtitulo = "\"CONFIANZA Y SEGURIDAD\"";
        private const string EmpresaRUC       = "80012345-0";
        private const string EmpresaDireccion = "Av. Principal N° 123, Asunción, Paraguay";
        private const string EmpresaTelefono  = "Tel. 021-123-456";

        private const string ColorVerde       = "#1B6B3A";
        private const string ColorVerdeClaro  = "#D4EDDA";
        private const string ColorVerdeMedio  = "#2E7D52";
        private const string ColorNegro       = "#1a1a1a";
        private const string ColorBorde       = "#999999";

        public static byte[] Generar(NotaCreditoPdfDataDto d)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(14, Unit.Point);
                    page.DefaultTextStyle(x => x.FontSize(8.5f).FontColor(ColorNegro));

                    page.Content().Column(col =>
                    {
                        // ── ENCABEZADO ────────────────────────────────────
                        col.Item().Row(row =>
                        {
                            // Logo / datos empresa
                            row.RelativeItem(3).Padding(4).Column(emp =>
                            {
                                // Intentar cargar logo
                                var logoPath = Path.Combine(
                                    AppDomain.CurrentDomain.BaseDirectory,
                                    "wwwroot", "images", "logo.png");

                                if (File.Exists(logoPath))
                                {
                                    emp.Item().Height(55).Image(logoPath).FitHeight();
                                }
                                else
                                {
                                    emp.Item().Text(EmpresaNombre).Bold().FontSize(20).FontColor(ColorVerde);
                                    emp.Item().Text(EmpresaSubtitulo).Italic().FontSize(8f);
                                }

                                emp.Item().PaddingTop(4).Text(t =>
                                {
                                    t.Span("RUC: ").Bold();
                                    t.Span(EmpresaRUC);
                                });
                                emp.Item().Text(EmpresaDireccion).FontSize(7.5f);
                                emp.Item().Text(EmpresaTelefono).FontSize(7.5f);
                            });

                            // Bloque NOTA DE CRÉDITO (derecha)
                            row.ConstantItem(175)
                               .Border(2).BorderColor(ColorVerde)
                               .Column(nc =>
                               {
                                   nc.Item()
                                     .Background(ColorVerde)
                                     .PaddingVertical(5).PaddingHorizontal(8)
                                     .AlignCenter()
                                     .Text("NOTA DE CRÉDITO")
                                     .FontColor(Colors.White).Bold().FontSize(11);

                                   nc.Item()
                                     .Background(ColorVerdeClaro)
                                     .BorderTop(1).BorderColor(ColorVerde)
                                     .Padding(4)
                                     .Row(r =>
                                     {
                                         r.ConstantItem(28).Text("No.:").Bold().FontSize(8f);
                                         r.RelativeItem().Text(d.NumeroNota)
                                             .Bold().FontSize(8f).FontColor(ColorVerdeMedio);
                                     });

                                   nc.Item()
                                     .Background(ColorVerdeClaro)
                                     .BorderTop(1).BorderColor(ColorVerde)
                                     .Padding(4)
                                     .Row(r =>
                                     {
                                         r.ConstantItem(38).Text("Fecha:").Bold().FontSize(8f);
                                         r.RelativeItem()
                                          .Text(d.FechaEmision.ToString("dd/MM/yyyy"))
                                          .FontSize(8f);
                                     });

                                   nc.Item()
                                     .Padding(4)
                                     .Text(t =>
                                     {
                                         t.Span("Fact. ref.: ").Bold().FontSize(7.5f);
                                         t.Span(d.NumeroFactura).FontSize(7.5f).FontColor(ColorVerdeMedio);
                                     });
                               });
                        });

                        col.Item().Height(6);

                        // ── DATOS CLIENTE + DETALLE DOCUMENTO ─────────────
                        col.Item().Border(1).BorderColor(ColorBorde).Row(info =>
                        {
                            // Columna: datos del cliente
                            info.RelativeItem().Column(cli =>
                            {
                                cli.Item()
                                   .Background(ColorVerde)
                                   .Padding(5)
                                   .AlignCenter()
                                   .Text("Información del cliente")
                                   .FontColor(Colors.White).Bold().FontSize(8.5f);

                                cli.Item()
                                   .BorderTop(1).BorderColor(ColorBorde)
                                   .Padding(5)
                                   .Column(c =>
                                   {
                                       c.Item().Text(t =>
                                       {
                                           t.Span("Nombre: ").Bold();
                                           t.Span(d.ClienteNombre);
                                       });
                                       if (!string.IsNullOrEmpty(d.ClienteDireccion))
                                           c.Item().PaddingTop(3).Text(t =>
                                           {
                                               t.Span("Domicilio: ").Bold();
                                               t.Span(d.ClienteDireccion);
                                           });
                                       if (!string.IsNullOrEmpty(d.ClienteTelefono))
                                           c.Item().PaddingTop(3).Text(t =>
                                           {
                                               t.Span("Teléfono: ").Bold();
                                               t.Span(d.ClienteTelefono);
                                           });
                                       c.Item().PaddingTop(3).Text(t =>
                                       {
                                           t.Span("CI/RUC: ").Bold();
                                           t.Span(d.ClienteRUC);
                                       });
                                   });
                            });

                            // Separador vertical
                            info.ConstantItem(1).Background(ColorBorde);

                            // Columna: detalle del documento
                            info.RelativeItem().Column(det =>
                            {
                                det.Item()
                                   .Background(ColorVerde)
                                   .Padding(5)
                                   .AlignCenter()
                                   .Text("Detalle del Documento")
                                   .FontColor(Colors.White).Bold().FontSize(8.5f);

                                det.Item()
                                   .BorderTop(1).BorderColor(ColorBorde)
                                   .Padding(5)
                                   .Column(c =>
                                   {
                                       c.Item().Text(t =>
                                       {
                                           t.Span("Factura no.: ").Bold();
                                           t.Span(d.NumeroFactura);
                                       });
                                       c.Item().PaddingTop(3).Text(t =>
                                       {
                                           t.Span("Fecha de emisión:  ").Bold();
                                           t.Span(d.FechaEmision.ToString("dd/MM/yyyy"));
                                       });
                                       c.Item().PaddingTop(3).Text(t =>
                                       {
                                           t.Span("Motivo:  ").Bold();
                                           t.Span(d.Motivo);
                                       });
                                   });
                            });
                        });

                        col.Item().Height(6);

                        // ── TABLA ─────────────────────────────────────────
                        col.Item().Border(1).BorderColor(ColorBorde).Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                cols.ConstantColumn(28);   // código
                                cols.RelativeColumn();      // descripción
                                cols.ConstantColumn(42);   // U/M
                                cols.ConstantColumn(32);   // Cant.
                                cols.ConstantColumn(80);   // Prec. Unit.
                                cols.ConstantColumn(88);   // Importe
                            });

                            table.Header(h =>
                            {
                                static IContainer Th(IContainer c) =>
                                    c.Background("#1B6B3A")
                                     .BorderBottom(1).BorderColor(ColorBorde)
                                     .Padding(4).AlignCenter();

                                h.Cell().Element(Th).Text("Cód.").Bold().FontColor(Colors.White).FontSize(8f);
                                h.Cell().Element(Th).Text("Descripción").Bold().FontColor(Colors.White).FontSize(8f);
                                h.Cell().Element(Th).Text("U/M").Bold().FontColor(Colors.White).FontSize(8f);
                                h.Cell().Element(Th).Text("Cant.").Bold().FontColor(Colors.White).FontSize(8f);
                                h.Cell().Element(Th).Text("Prec. Unit.").Bold().FontColor(Colors.White).FontSize(8f);
                                h.Cell().Element(Th).Text("Importe").Bold().FontColor(Colors.White).FontSize(8f);
                            });

                            // Fila: vehículo de la permuta
                            static IContainer Td(IContainer c) =>
                                c.BorderRight(1).BorderColor(ColorBorde)
                                 .MinHeight(90).Padding(4).AlignCenter().AlignMiddle();

                            table.Cell().Element(Td).Text("1");

                            // Descripción
                            var descripcion = d.PermutaMarca != null
                                ? $"{d.PermutaMarca} {d.PermutaModelo} {d.PermutaAnio}"
                                  + (d.PermutaColor != null ? $" - Color: {d.PermutaColor}" : "")
                                  + (d.PermutaKilometraje.HasValue ? $" - Kms: {d.PermutaKilometraje:N0}" : "")
                                  + "\n(Vehículo recibido en permuta)"
                                : $"Vehículo en permuta por la venta de:\n{d.VehiculoMarca} {d.VehiculoModelo} {d.VehiculoAnio}";

                            table.Cell()
                                 .BorderRight(1).BorderColor(ColorBorde)
                                 .MinHeight(90).Padding(6)
                                 .Column(dc =>
                                 {
                                     if (d.PermutaMarca != null)
                                     {
                                         dc.Item().Text(t =>
                                         {
                                             t.Span($"{d.PermutaMarca.ToUpper()} ").Bold();
                                             t.Span($"{d.PermutaModelo?.ToUpper()} ");
                                             t.Span($"AÑO {d.PermutaAnio}").Bold();
                                         });
                                         if (!string.IsNullOrEmpty(d.PermutaColor))
                                             dc.Item().PaddingTop(3).Text($"Color: {d.PermutaColor.ToUpper()}");
                                         if (d.PermutaKilometraje.HasValue)
                                             dc.Item().PaddingTop(3).Text($"Km: {d.PermutaKilometraje:N0}");
                                         if (!string.IsNullOrEmpty(d.PermutaEstado))
                                             dc.Item().PaddingTop(3).Text($"Estado: {d.PermutaEstado.ToUpper()}").FontSize(8f);
                                     }
                                     else
                                     {
                                         dc.Item().Text($"Permuta venta de {d.VehiculoMarca} {d.VehiculoModelo} {d.VehiculoAnio}");
                                     }
                                     dc.Item().PaddingTop(6)
                                       .Text($"VEHÍCULO RECIBIDO EN PERMUTA").Italic().FontSize(7.5f).FontColor(ColorVerdeMedio);
                                 });

                            table.Cell().Element(Td).Text("unidad");
                            table.Cell().Element(Td).Text("1");
                            table.Cell().Element(Td).Text(""); // precio unitario en blanco
                            table.Cell()
                                 .MinHeight(90).Padding(4).AlignRight().AlignMiddle()
                                 .Text($"Gs. {d.Monto:N0}").Bold().FontColor(ColorVerde);
                        });

                        col.Item().Height(6);

                        // ── PIE: NOTA + TOTALES ────────────────────────────
                        col.Item().Border(1).BorderColor(ColorBorde).Row(pie =>
                        {
                            // Cantidad con letras
                            pie.RelativeItem(2)
                               .BorderRight(1).BorderColor(ColorBorde)
                               .Padding(7)
                               .Column(lc =>
                               {
                                   lc.Item().Text("NOTA:").Bold();
                                   lc.Item().PaddingTop(4)
                                     .Text($"({NumeroALetras(d.Monto)} GUARANÍES---00/100)")
                                     .FontSize(7.5f);
                               });

                            // Totales
                            pie.RelativeItem().Column(tot =>
                            {
                                FilaTotales(tot, "Subtotal", $"Gs. {d.Subtotal:N0}", false, ColorBorde);
                                FilaTotales(tot, "IVA (10%)", $"Gs. {d.IVA:N0}", false, ColorBorde);
                                FilaTotales(tot, "TOTAL CRÉDITO", $"Gs. {d.Monto:N0}", true, ColorVerde);
                            });
                        });

                        col.Item().Height(6);

                        // ── DATOS DE TIMBRADO ────────────────────────────
                        col.Item()
                           .Border(1).BorderColor(ColorBorde)
                           .Background("#F9FFF9")
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
                                   t.Span(d.NumeroNota);
                                   t.Span("          FECHA DE EMISIÓN: ").Bold();
                                   t.Span(d.FechaEmision.ToString("dd/MM/yyyy"));
                               });
                           });

                        col.Item().PaddingTop(10).AlignCenter()
                           .Text("¡Gracias por confiar en A&J Autos Usados!")
                           .Bold().Italic().FontSize(10f).FontColor(ColorVerdeMedio);
                    });
                });
            }).GeneratePdf();
        }

        private static void FilaTotales(ColumnDescriptor col, string etiqueta, string valor,
            bool resaltar, string colorResalte)
        {
            col.Item()
               .Background(resaltar ? colorResalte : Colors.White)
               .BorderTop(1).BorderColor(ColorBorde)
               .Row(r =>
               {
                   r.RelativeItem().Padding(4)
                    .Text(etiqueta).Bold()
                    .FontColor(resaltar ? Colors.White : ColorNegro);
                   r.RelativeItem().Padding(4).AlignRight()
                    .Text(valor).Bold()
                    .FontColor(resaltar ? Colors.White : ColorNegro);
               });
        }

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
            if (numero > 0) partes.Add(MenosDeMil((int)numero));

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

            if (n == 100) return "CIEN";
            var sb = new System.Text.StringBuilder();
            if (n >= 100) { sb.Append(centenas[n / 100]); n %= 100; if (n > 0) sb.Append(' '); }
            if (n >= 20 && n < 30) sb.Append(veintis[n - 20]);
            else if (n >= 20) { sb.Append(decenas[n / 10]); if (n % 10 > 0) sb.Append(" Y " + unidades[n % 10]); }
            else if (n > 0) sb.Append(unidades[n]);
            return sb.ToString();
        }
    }
}