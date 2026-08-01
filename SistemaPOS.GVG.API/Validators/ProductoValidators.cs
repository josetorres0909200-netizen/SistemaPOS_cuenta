using FluentValidation;
using SistemaPOS.GVG.API.DTOs;

namespace SistemaPOS.GVG.API.Validators
{
    public class ProductoCreateValidator : AbstractValidator<ProductoCreateDTO>
    {
        public ProductoCreateValidator()
        {
            RuleFor(p => p.CodigoBarras)
                .NotEmpty().WithMessage("El código de barras es obligatorio")
                .MaximumLength(50).WithMessage("El código de barras no puede exceder 50 caracteres")
                .Matches(@"^[a-zA-Z0-9\-]+$").WithMessage("El código de barras solo puede contener letras, números y guiones");

            RuleFor(p => p.Descripcion)
                .NotEmpty().WithMessage("La descripción es obligatoria")
                .MinimumLength(3).WithMessage("La descripción debe tener al menos 3 caracteres")
                .MaximumLength(200).WithMessage("La descripción no puede exceder 200 caracteres");

            RuleFor(p => p.Categoria)
                .NotEmpty().WithMessage("La categoría es obligatoria")
                .MaximumLength(50);

            RuleFor(p => p.Acabado)
                .NotEmpty().WithMessage("El acabado es obligatorio")
                .MaximumLength(50);

            RuleFor(p => p.Tamanio)
                .NotEmpty().WithMessage("El tamaño es obligatorio")
                .MaximumLength(50);

            RuleFor(p => p.PrecioCosto)
                .GreaterThanOrEqualTo(0).WithMessage("El precio de costo no puede ser negativo")
                .LessThan(1000000).WithMessage("El precio de costo es demasiado alto");

            RuleFor(p => p.PrecioVenta)
                .GreaterThan(0).WithMessage("El precio de venta debe ser mayor a 0")
                .LessThan(1000000).WithMessage("El precio de venta es demasiado alto")
                .GreaterThanOrEqualTo(p => p.PrecioCosto)
                    .WithMessage("El precio de venta debe ser mayor o igual al precio de costo");

            RuleFor(p => p.Stock)
                .GreaterThanOrEqualTo(0).WithMessage("El stock no puede ser negativo")
                .LessThan(10000000).WithMessage("El stock es demasiado alto");
        }
    }

    public class ProductoUpdateValidator : AbstractValidator<ProductoUpdateDTO>
    {
        public ProductoUpdateValidator()
        {
            Include(new ProductoCreateValidator());

            RuleFor(p => p.IdProducto)
                .GreaterThan(0).WithMessage("El ID del producto debe ser válido");
        }
    }
}
