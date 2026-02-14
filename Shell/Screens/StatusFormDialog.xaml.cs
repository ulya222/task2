using System.Windows;
using TelecomProd.Core.Entities;

namespace TelecomProd.Shell.Screens;

public partial class StatusFormDialog : Window
{
    public int SelectedStatusId { get; private set; }

    public StatusFormDialog(List<OrderStatus> statuses, int currentStatusId)
    {
        InitializeComponent();
        StatusCombo.ItemsSource = statuses;
        foreach (OrderStatus s in statuses) { if (s.Id == currentStatusId) { StatusCombo.SelectedItem = s; break; } }
        if (StatusCombo.SelectedItem == null && statuses.Count > 0) StatusCombo.SelectedIndex = 0;
    }

    private void Apply_Click(object sender, RoutedEventArgs e)
    {
        if (StatusCombo.SelectedValue is int id) SelectedStatusId = id;
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) { DialogResult = false; Close(); }
}
