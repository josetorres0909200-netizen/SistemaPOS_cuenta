using FluentValidation;
using SistemaPOS.GVG.API.DTOs;

namespace SistemaPOS.GVG.API.Validators
{
    public class ClienteCreateValidator : AbstractValidator<ClienteCreateDTO>
    {
        public ClienteCreateValidator()
        {
            RuleFor(c => c.Nombre)
                .NotEmpty().WithMessage("El nombre es obligatorio")
                .MinimumLength(3).WithMessage("El nombre debe tener al menos 3 caracteres")
                .MaximumLength(100).WithMessage("El nombre no puede exceder 100 caracteres")
                .Matches(@"^[a-zA-ZáéíóúÁÉÍÓÚñÑ\s]+$")
                    .WithMessage("El nombre solo puede contener letras y espacios");

            RuleFor(c => c.Telefono)
                .Matches(@"^\d{10}$").WithMessage("El teléfono debe tener 10 dígitos")
                .When(c => !string.IsNullOrEmpty(c.Telefono));

            RuleFor(c => c.Email)
                .EmailAddress().WithMessage("El email no es válido")
                .When(c => !string.IsNullOrEmpty(c.Email));

            RuleFor(c => c.Direccion)
                .MaximumLength(200).WithMessage("La dirección no puede exceder 200 caracteres")
                .When(c => !string.IsNullOrEmpty(c.Direccion));
        }
    }

    public class ClienteUpdateValidator : AbstractValidator<ClienteUpdateDTO>
    {
        public ClienteUpdateValidator()
        {
            Include(new ClienteCreateValidator());

            RuleFor(c => c.IdCliente)
                .GreaterThan(0).WithMessage("El ID del cliente debe ser válido");
        }
    }
}
