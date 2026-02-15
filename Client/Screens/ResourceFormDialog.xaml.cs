using System.Windows;
using DataVault.Core.Entities;

namespace DataVault.Client.Screens;

public partial class ResourceFormDialog : Window
{
    public Resource Resource { get; }

    public ResourceFormDialog(Resource resource, List<Vendor> vendors)
    {
        InitializeComponent();
        Resource = resource;
        VendorCombo.ItemsSource = vendors;
        if (resource.VendorId.HasValue) VendorCombo.SelectedValue = resource.VendorId;
        DataContext = Resource;
    }

    private void Apply_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(Resource.Name)) { MessageBox.Show("Введите наименование.", "Проверка"); return; }
        if (string.IsNullOrWhiteSpace(Resource.Code)) { MessageBox.Show("Введите код.", "Проверка"); return; }
        if (string.IsNullOrWhiteSpace(Resource.ResourceKind)) Resource.ResourceKind = "material";
        if (VendorCombo.SelectedValue is int vid) Resource.VendorId = vid;
        else Resource.VendorId = null;
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) { DialogResult = false; Close(); }
}
