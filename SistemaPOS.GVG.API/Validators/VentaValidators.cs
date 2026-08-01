using FluentValidation;
using SistemaPOS.GVG.API.DTOs;

namespace SistemaPOS.GVG.API.Validators
{
    public class VentaCreateValidator : AbstractValidator<VentaCreateDTO>
    {
        public VentaCreateValidator()
        {
            RuleFor(v => v.Detalles)
                .NotEmpty().WithMessage("La venta debe incluir al menos un producto")
                .Must(detalles => detalles.Count > 0)
                    .WithMessage("Debe agregar al menos un producto a la venta");

            RuleFor(v => v.Impuesto)
                .GreaterThanOrEqualTo(0).WithMessage("El impuesto no puede ser negativo")
                .LessThan(100000).WithMessage("El impuesto parece incorrecto");

            RuleFor(v => v.Pagado)
                .GreaterThan(0).WithMessage("El monto pagado debe ser mayor a 0");

            RuleFor(v => v.TipoPago)
                .NotEmpty().WithMessage("El tipo de pago es obligatorio")
                .Must(tipo => new[] { "Efectivo", "Tarjeta", "Transferencia" }.Contains(tipo))
                    .WithMessage("El tipo de pago debe ser: Efectivo, Tarjeta o Transferencia");

            RuleFor(v => v.Observaciones)
                .MaximumLength(500).WithMessage("Las observaciones no pueden exceder 500 caracteres");

            RuleForEach(v => v.Detalles)
                .SetValidator(new DetalleVentaCreateValidator());
        }
    }

    public class DetalleVentaCreateValidator : AbstractValidator<DetalleVentaCreateDTO>
    {
        public DetalleVentaCreateValidator()
        {
            RuleFor(d => d.IdProducto)
                .GreaterThan(0).WithMessage("Debe especificar un producto válido");

            RuleFor(d => d.Cantidad)
                .GreaterThan(0).WithMessage("La cantidad debe ser mayor a 0")
                .LessThan(10000).WithMessage("La cantidad parece incorrecta");

            RuleFor(d => d.PrecioUnitario)
                .GreaterThan(0).WithMessage("El precio unitario debe ser mayor a 0")
                .LessThan(1000000).WithMessage("El precio unitario es demasiado alto");
        }
    }
}
