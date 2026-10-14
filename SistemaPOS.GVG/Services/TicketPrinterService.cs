using SistemaPOS.Desktop.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SistemaPOS.GVG.Services
{
    public static class TicketPrinterService
    {
        // Ruta donde se guardarán los tickets (Carpeta Mis Documentos\TicketsPOS)
        private static readonly string CarpetaTickets = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "TicketsPOS"
        );

        public static void ImprimirTicket(VentaDTO venta, List<DetalleVentaDTO> detalles)
        {
            try
            {
                // Asegurar que la carpeta exista, si no, crearla
                if (!Directory.Exists(CarpetaTickets))
                {
                    Directory.CreateDirectory(CarpetaTickets);
                }

                // Generar un nombre de archivo único usando el ID de Venta y la fecha
                string nombreArchivo = $"Ticket_Venta_{venta.IdVenta}_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
                string rutaCompleta = Path.Combine(CarpetaTickets, nombreArchivo);

                // Ensamblar el texto del ticket
                StringBuilder ticket = new StringBuilder();

                // --- CABECERA ---
                ticket.AppendLine("========================================");
                ticket.AppendLine("            MI TIENDA POS               ");
                ticket.AppendLine("      Tu eslogan o dirección aquí       ");
                ticket.AppendLine("========================================");
                ticket.AppendLine($"Fecha: {venta.FechaVenta:dd/MM/yyyy HH:mm}");
                ticket.AppendLine($"Ticket #: {venta.IdVenta}");
                ticket.AppendLine($"Atendió: Cajero(a) ID {venta.UsuarioId}");
                ticket.AppendLine("----------------------------------------");
                ticket.AppendLine("CANT   DESCRIPCION        PRECIO   SUBT ");
                ticket.AppendLine("----------------------------------------");

                // --- DETALLES DE PRODUCTOS ---
                foreach (var item in detalles)
                {
                    // Si no hay descripción disponible, usar ID de producto como fallback
                    string desc = item.GetType().GetProperty("DescripcionProducto") != null
                                    ? (item.GetType().GetProperty("DescripcionProducto")?.GetValue(item)?.ToString() ?? "Producto")
                                    : $"Producto {item.IdProducto}";

                    if (desc.Length > 15) desc = desc.Substring(0, 15);
                    desc = desc.PadRight(15);

                    // Formato tabular
                    ticket.AppendLine($"{item.Cantidad.ToString("0.00").PadRight(6)} " +
                                      $"{desc} " +
                                      $"${item.PrecioUnitario.ToString("0.00").PadRight(7)} " +
                                      $"${item.Subtotal.ToString("0.00")}");
                }

                // --- TOTALES ---
                ticket.AppendLine("----------------------------------------");
                ticket.AppendLine($"                  SUBTOTAL: ${venta.Subtotal:0.00}");
                ticket.AppendLine($"                  IMPUESTO: ${venta.Impuesto:0.00}");
                ticket.AppendLine($"                     TOTAL: ${venta.Total:0.00}");
                ticket.AppendLine("----------------------------------------");
                ticket.AppendLine($"TIPO DE PAGO: {venta.TipoPago}");

                if (venta.TipoPago != "Crédito")
                {
                    ticket.AppendLine($"                  PAGÓ CON: ${venta.Pagado:0.00}");
                    ticket.AppendLine($"                    CAMBIO: ${venta.Cambio:0.00}");
                }
                else
                {
                    ticket.AppendLine($"      ** COBRO A CUENTA DE CLIENTE **   ");
                    ticket.AppendLine($"                ID CLIENTE: {venta.IdCliente}");
                }

                // --- PIE DE PÁGINA ---
                ticket.AppendLine("========================================");
                ticket.AppendLine("       ¡GRACIAS POR SU COMPRA!          ");
                ticket.AppendLine("========================================");

                // Escribir el archivo
                File.WriteAllText(rutaCompleta, ticket.ToString(), Encoding.UTF8);

                // Opcional: Abrir el archivo de texto automáticamente en Notepad para que se vea la "impresión"
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo()
                {
                    FileName = rutaCompleta,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                // En WPF, podrías lanzar esto a la UI, por ahora lo dejamos en consola/debug
                System.Diagnostics.Debug.WriteLine($"Error al generar ticket: {ex.Message}");
            }
        }
    }
}
