using System.Windows;
using TelecomProd.Core.Entities;

namespace TelecomProd.Shell.Screens;

public partial class OrderFormDialog : Window
{
    public int SelectedUnitId { get; private set; }
    public int Quantity { get; private set; }
    public DateTime? PlannedFinishAt { get; private set; }

    public OrderFormDialog(List<AssemblyUnit> units, List<OrderStatus> _)
    {
        InitializeComponent();
        UnitCombo.ItemsSource = units;
        if (units.Count > 0) UnitCombo.SelectedIndex = 0;
    }

    private void Apply_Click(object sender, RoutedEventArgs e)
    {
        if (UnitCombo.SelectedValue is int id) SelectedUnitId = id;
        if (int.TryParse(QtyBox.Text, out var q) && q > 0) Quantity = q;
        else { MessageBox.Show("Введите корректное количество.", "Проверка"); return; }
        PlannedFinishAt = PlanDatePicker.SelectedDate;
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) { DialogResult = false; Close(); }
}
