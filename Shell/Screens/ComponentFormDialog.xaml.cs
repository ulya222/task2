using System.Windows;
using TelecomProd.Core.Entities;

namespace TelecomProd.Shell.Screens;

public partial class ComponentFormDialog : Window
{
    public Component Component { get; }

    public ComponentFormDialog(Component component, List<Supplier> suppliers)
    {
        InitializeComponent();
        Component = component;
        SupplierCombo.ItemsSource = suppliers;
        if (component.SupplierId.HasValue) SupplierCombo.SelectedValue = component.SupplierId;
        DataContext = Component;
    }

    private void Apply_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(Component.Name)) { MessageBox.Show("Введите наименование.", "Проверка"); return; }
        if (string.IsNullOrWhiteSpace(Component.Code)) { MessageBox.Show("Введите код (формат TYPE-XXXXX, например ELEC-00001).", "Проверка"); return; }
        if (string.IsNullOrWhiteSpace(Component.ComponentType)) Component.ComponentType = "passive,electronic";
        if (SupplierCombo.SelectedValue is int sid) Component.SupplierId = sid;
        else Component.SupplierId = null;
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) { DialogResult = false; Close(); }
}
