using System.Windows;
using DataVault.Core.Entities;

namespace DataVault.Client.Screens;

public partial class PhaseFormDialog : Window
{
    public int SelectedPhaseId { get; private set; }

    public PhaseFormDialog(List<TaskPhase> phases, int currentPhaseId)
    {
        InitializeComponent();
        PhaseCombo.ItemsSource = phases;
        foreach (var p in phases)
        {
            if (p.Id == currentPhaseId) { PhaseCombo.SelectedItem = p; break; }
        }
        if (PhaseCombo.SelectedItem == null && phases.Count > 0) PhaseCombo.SelectedIndex = 0;
    }

    private void Apply_Click(object sender, RoutedEventArgs e)
    {
        if (PhaseCombo.SelectedValue is int id) SelectedPhaseId = id;
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) { DialogResult = false; Close(); }
}
