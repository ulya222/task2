using System.Windows;
using DataVault.Core.Entities;

namespace DataVault.Client.Screens;

public partial class TaskFormDialog : Window
{
    public int SelectedCategoryId { get; private set; }
    public int Quantity { get; private set; }
    public DateTime? PlannedFinishAt { get; private set; }

    public TaskFormDialog(List<Category> categories, List<TaskPhase> _)
    {
        InitializeComponent();
        CategoryCombo.ItemsSource = categories;
        if (categories.Count > 0) CategoryCombo.SelectedIndex = 0;
    }

    private void Apply_Click(object sender, RoutedEventArgs e)
    {
        if (CategoryCombo.SelectedValue is int id) SelectedCategoryId = id;
        if (int.TryParse(QtyBox.Text, out var q) && q > 0) Quantity = q;
        else { MessageBox.Show("Введите корректное количество.", "Проверка"); return; }
        PlannedFinishAt = PlanDatePicker.SelectedDate;
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) { DialogResult = false; Close(); }
}
