using OfficeOpenXml;
using System.Data;
using System.Drawing;

namespace PlayaAutos.Web.Helpers
{
    public static class ExcelHelper
    {
        public static byte[] GenerarExcelDesdeDiccionario(List<Dictionary<string, object>> datos, string nombreHoja)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add(nombreHoja);

            if (datos == null || datos.Count == 0)
            {
                worksheet.Cells["A1"].Value = "Sin datos disponibles";
                return package.GetAsByteArray();
            }

            // Obtener columnas del primer registro
            var columnas = datos[0].Keys.ToArray();

            // Escribir encabezados
            for (int col = 0; col < columnas.Length; col++)
            {
                worksheet.Cells[1, col + 1].Value = columnas[col];
                worksheet.Cells[1, col + 1].Style.Font.Bold = true;
                worksheet.Cells[1, col + 1].Style.Font.Color.SetColor(Color.White);
                worksheet.Cells[1, col + 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                worksheet.Cells[1, col + 1].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(30, 30, 40));
            }

            // Escribir datos
            for (int fila = 0; fila < datos.Count; fila++)
            {
                for (int col = 0; col < columnas.Length; col++)
                {
                    var valor = datos[fila].ContainsKey(columnas[col]) ? datos[fila][columnas[col]] : "";
                    worksheet.Cells[fila + 2, col + 1].Value = valor;
                }
            }

            worksheet.Cells.AutoFitColumns();

            return package.GetAsByteArray();
        }

        public static byte[] GenerarExcelMultiHoja(Dictionary<string, List<Dictionary<string, object>>> hojas)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using var package = new ExcelPackage();

            foreach (var kvp in hojas)
            {
                var nombre = kvp.Key.Length > 31 ? kvp.Key.Substring(0, 31) : kvp.Key;
                var worksheet = package.Workbook.Worksheets.Add(nombre);

                if (kvp.Value != null && kvp.Value.Count > 0)
                {
                    var columnas = kvp.Value[0].Keys.ToArray();

                    for (int col = 0; col < columnas.Length; col++)
                    {
                        worksheet.Cells[1, col + 1].Value = columnas[col];
                        worksheet.Cells[1, col + 1].Style.Font.Bold = true;
                        worksheet.Cells[1, col + 1].Style.Font.Color.SetColor(Color.White);
                        worksheet.Cells[1, col + 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                        worksheet.Cells[1, col + 1].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(30, 30, 40));
                    }

                    for (int fila = 0; fila < kvp.Value.Count; fila++)
                    {
                        for (int col = 0; col < columnas.Length; col++)
                        {
                            var valor = kvp.Value[fila].ContainsKey(columnas[col]) ? kvp.Value[fila][columnas[col]] : "";
                            worksheet.Cells[fila + 2, col + 1].Value = valor;
                        }
                    }

                    worksheet.Cells.AutoFitColumns();
                }
                else
                {
                    worksheet.Cells["A1"].Value = "Sin datos";
                }
            }

            return package.GetAsByteArray();
        }
    }
}