using System;
using System.Windows;
using System.Windows.Controls;
using SistemaPOS.Desktop.Models;
using SistemaPOS.Desktop.Services;

namespace SistemaPOS.GVG.Views
{
    public partial class NuevoProductoDialog : Window
    {
        private ProductoDTO? _productoEdicion;

        public NuevoProductoDialog()
        {
            InitializeComponent();
            _productoEdicion = null;
        }

        public NuevoProductoDialog(ProductoDTO producto)
        {
            InitializeComponent();
            _productoEdicion = producto;
            CargarDatos(producto);
        }

        private void CargarDatos(ProductoDTO producto)
        {
            // Cargar los datos del producto en los campos de edición
            txtCodigoBarras.Text = producto.CodigoBarras;
            txtDescripcion.Text = producto.Descripcion;
            txtPrecioCosto.Text = producto.PrecioCosto.ToString("F2");
            txtPrecioVenta.Text = producto.PrecioVenta.ToString("F2");

            // Si existe un campo de categoría, también cargarlo
            if (!string.IsNullOrEmpty(producto.Categoria))
            {
                foreach (ComboBoxItem item in cmbCategoria.Items)
                {
                    if (item.Content.ToString() == producto.Categoria)
                    {
                        cmbCategoria.SelectedItem = item;
                        break;
                    }
                }
            }
        }

        private async void btnGuardar_Click(object sender, RoutedEventArgs e)
        {
            // 1. Validar que el usuario esté autenticado
            if (!ApiClient.Instance.IsAuthenticated)
            {
                MessageBox.Show("Sesión no válida. Por favor, inicie sesión nuevamente.", 
                    "No Autenticado", MessageBoxButton.OK, MessageBoxImage.Warning);

                // Cerrar esta ventana y redirigir al login
                this.DialogResult = false;
                this.Close();
                return;
            }

            // 2. Validaciones básicas de campos vacíos
            if (string.IsNullOrWhiteSpace(txtCodigoBarras.Text) ||
                string.IsNullOrWhiteSpace(txtDescripcion.Text) ||
                cmbCategoria.SelectedItem == null)
            {
                MessageBox.Show("Por favor, complete los campos obligatorios.", 
                    "Advertencia", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 3. Validación de conversiones numéricas (precios)
            if (!decimal.TryParse(txtPrecioCosto.Text, out decimal precioCosto) ||
                !decimal.TryParse(txtPrecioVenta.Text, out decimal precioVenta))
            {
                MessageBox.Show("Los precios deben ser valores numéricos válidos.", 
                    "Error de formato", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // 4. Validación de lógica de negocio
            if (precioCosto < 0 || precioVenta < 0)
            {
                MessageBox.Show("Los precios no pueden ser negativos.", 
                    "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (precioVenta < precioCosto)
            {
                var result = MessageBox.Show(
                    "El precio de venta es menor que el precio de costo. ¿Desea continuar?",
                    "Advertencia de Margen Negativo",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.No)
                    return;
            }

            try
            {
                // Deshabilitar el botón para evitar doble clic
                btnGuardar.IsEnabled = false;

                // 5. Crear el objeto DTO que espera la API
                var nuevoProducto = new
                {
                    CodigoBarras = txtCodigoBarras.Text.Trim(),
                    Descripcion = txtDescripcion.Text.Trim(),
                    Categoria = ((ComboBoxItem)cmbCategoria.SelectedItem).Content.ToString(),
                    Acabado = "Estándar", // Valor por defecto (campo no visible en UI)
                    Tamanio = "Estándar",  // Valor por defecto (campo no visible en UI)
                    PrecioCosto = precioCosto,
                    PrecioVenta = precioVenta,
                    Stock = 0m // Stock inicial en 0
                };

                // 6. Enviar petición usando ApiClient singleton
                var productoCreado = await ApiClient.Instance.PostAsync<ProductoDTO>(
                    "productos", 
                    nuevoProducto);

                MessageBox.Show(
                    $"Producto '{productoCreado.Descripcion}' registrado correctamente.\nCódigo: {productoCreado.CodigoBarras}",
                    "Éxito",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                this.DialogResult = true; // Cierra la ventana indicando éxito
                this.Close();
            }
            catch (UnauthorizedAccessException ex)
            {
                MessageBox.Show(
                    $"Sesión expirada o no válida.\n\n{ex.Message}\n\nPor favor, inicie sesión nuevamente.",
                    "Error de Autenticación",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                // Cerrar esta ventana y forzar re-login
                this.DialogResult = false;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error al guardar el producto:\n\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                // Rehabilitar el botón
                btnGuardar.IsEnabled = true;
            }
        }

        private void btnCancelar_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}