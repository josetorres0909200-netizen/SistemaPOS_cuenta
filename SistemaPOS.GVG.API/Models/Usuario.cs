using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; // Requisito obligatorio para mapear columnas

namespace SistemaPOS.GVG.API.Models
{
    public class Usuario
    {
        [Key]
        public int IdUsuario { get; set; }

        [Required]
        [StringLength(50)]
        public string Username { get; set; }

        [Required]
        public string PasswordHash { get; set; }

        [Required]
        [StringLength(30)]
        public string Rol { get; set; }

        // AQUÍ ESTÁ LA SOLUCIÓN: Le decimos a EF Core que 'Activo' en C# es 'Estatus' en SQL Server
        [Column("Estatus")]
        public bool Activo { get; set; }
    }
}