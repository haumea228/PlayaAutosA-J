using Microsoft.EntityFrameworkCore;
using PlayaAutos.API.Data;
using PlayaAutos.API.Models;

namespace PlayaAutos.API.Services
{
    public record RegistroPagoCuota(
        DateTime FechaPago,
        long MontoPagado,
        long MontoRecargo,
        int FormaPagoId,
        int UsuarioRegistro,
        string? ObservacionPago,
        string? ComprobanteImagen);

    /// <summary>
    /// Única lógica de "cuota cobrada": marca la cuota como Pagada y genera el
    /// ingreso en caja. La usan tanto el cobro directo en ventanilla
    /// (PUT api/cuotas/{id}/pagar) como la aprobación de un pago informado por
    /// el cliente desde la app, para que ambos caminos produzcan exactamente
    /// el mismo movimiento de caja y el mismo ticket.
    /// </summary>
    public class CuotaPagoService
    {
        private readonly AppDbContext _context;

        public CuotaPagoService(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Recargo por mora a una fecha dada. Mismo criterio que el cálculo que
        /// ya mostraba la web (GET api/cuotas/{id}): vencida = fecha de
        /// vencimiento anterior a la fecha evaluada.
        /// </summary>
        public static long CalcularRecargo(Cuota cuota, Venta venta, DateTime fecha)
        {
            if (cuota.FechaVencimiento >= fecha) return 0;
            if (!venta.PorcentajeRecargo.HasValue || venta.PorcentajeRecargo <= 0) return 0;
            return (long)(cuota.Monto * (venta.PorcentajeRecargo.Value / 100m));
        }

        /// <summary>
        /// Reglas para que el CLIENTE pague una cuota desde la app (por
        /// transferencia o con tarjeta): pendiente, sin otro pago en revisión y
        /// la más antigua sin pagar de su venta. Devuelve el error o null.
        /// La cuota debe venir con Venta cargada.
        /// </summary>
        public async Task<string?> ValidarPagoClienteAsync(Cuota cuota)
        {
            if (cuota.Venta.Estado == "Anulada") return "La venta asociada está anulada.";
            if (cuota.Estado == EstadosCuota.Pagada) return "Esta cuota ya está pagada.";
            if (cuota.Estado == EstadosCuota.EnRevision) return "Ya informaste un pago para esta cuota y está en revisión.";
            if (cuota.Estado != EstadosCuota.Pendiente) return "Esta cuota no admite pagos.";

            var primeraNoPagada = await _context.Cuotas
                .Where(c => c.VentaId == cuota.VentaId &&
                            (c.Estado == EstadosCuota.Pendiente || c.Estado == EstadosCuota.EnRevision))
                .MinAsync(c => c.NumeroCuota);
            return primeraNoPagada != cuota.NumeroCuota
                ? $"Primero tenés que abonar la cuota N° {primeraNoPagada}."
                : null;
        }

        /// <summary>
        /// Aplica el pago sobre la cuota (que debe venir con Venta cargada) y
        /// agrega el movimiento de caja. No llama a SaveChanges: el que invoca
        /// controla la transacción. Devuelve un mensaje de error o null.
        /// </summary>
        public async Task<string?> RegistrarPagoAsync(Cuota cuota, RegistroPagoCuota datos)
        {
            if (cuota.Estado == EstadosCuota.Pagada) return "Esta cuota ya está pagada.";
            if (cuota.Estado == EstadosCuota.Anulada) return "La cuota está anulada.";
            if (cuota.Venta.Estado == "Anulada") return "La venta asociada está anulada.";

            if (!await _context.FormasPago.AnyAsync(f => f.FormaPagoId == datos.FormaPagoId))
                return "La forma de pago seleccionada no existe.";

            var tipoIngreso = await _context.TiposMovimiento.FirstOrDefaultAsync(t => t.Signo == "+");
            if (tipoIngreso == null)
                return "No se encontró el tipo de movimiento 'Ingreso' en la base de datos.";

            cuota.FechaPago = datos.FechaPago;
            cuota.MontoPagado = datos.MontoPagado;
            cuota.MontoRecargo = datos.MontoRecargo;
            cuota.FormaPagoId = datos.FormaPagoId;
            cuota.ObservacionPago = datos.ObservacionPago;
            cuota.ComprobanteImagen = datos.ComprobanteImagen;
            cuota.Estado = EstadosCuota.Pagada;
            cuota.FechaRegistroPago = DateTime.Now;

            _context.MovimientosCaja.Add(new MovimientoCaja
            {
                Fecha = DateTime.Now,
                TipoMovimientoId = tipoIngreso.TipoMovimientoId,
                Descripcion = $"Cobro Cuota #{cuota.NumeroCuota} - Venta #{cuota.VentaId}",
                Monto = datos.MontoPagado + datos.MontoRecargo,
                CuotaId = cuota.CuotaId,
                FormaPagoId = datos.FormaPagoId,
                UsuarioRegistro = datos.UsuarioRegistro,
                Comentarios = datos.MontoRecargo > 0 ? $"Recargo por mora: Gs. {datos.MontoRecargo:N0}" : null
            });

            return null;
        }
    }
}
