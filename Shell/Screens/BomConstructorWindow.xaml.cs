using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TelecomProd.Core.Entities;
using TelecomProd.Shell.Services;

namespace TelecomProd.Shell.Screens;

public partial class BomConstructorWindow : Window
{
    private readonly ApiClient _api = new();
    private readonly ObservableCollection<Component> _availableComponents = new();
    private readonly ObservableCollection<BomRow> _bomRows = new();
    private int? _selectedUnitId;

    public BomConstructorWindow(List<AssemblyUnit> units, List<Component> components, List<BomItem>? currentBom, int? selectedUnitId)
    {
        InitializeComponent();
        UnitCombo.ItemsSource = units;
        if (selectedUnitId.HasValue) UnitCombo.SelectedValue = selectedUnitId;
        else if (units.Count > 0) UnitCombo.SelectedIndex = 0;
        _selectedUnitId = selectedUnitId ?? (units.Count > 0 ? units[0].Id : (int?)null);
        _availableComponents.Clear();
        foreach (var c in components) _availableComponents.Add(c);
        ComponentsList.ItemsSource = _availableComponents;
        ComponentsList.DisplayMemberPath = "Name";
        BomList.ItemsSource = _bomRows;
        BomList.DisplayMemberPath = "DisplayText";
        if (currentBom != null)
            foreach (var b in currentBom)
                _bomRows.Add(new BomRow { BomItemId = b.Id, ComponentId = b.ComponentId, ComponentName = b.Component?.Name ?? b.ComponentId.ToString(), Quantity = b.Quantity });
    }

    private async void UnitCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (UnitCombo.SelectedValue is not int id) return;
        _selectedUnitId = id;
        _bomRows.Clear();
        try
        {
            var bom = await _api.GetAsync<List<BomItem>>($"AssemblyUnits/{id}/bom");
            if (bom != null)
                foreach (var b in bom)
                    _bomRows.Add(new BomRow { BomItemId = b.Id, ComponentId = b.ComponentId, ComponentName = b.Component?.Name ?? b.ComponentId.ToString(), Quantity = b.Quantity });
        }
        catch { /* ignore */ }
    }

    private async void ComponentsList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (UnitCombo.SelectedValue is not int unitId || ComponentsList.SelectedItem is not Component comp) return;
        var qty = 1;
        var added = await _api.PostAsync<BomItem>($"AssemblyUnits/{unitId}/bom", new { componentId = comp.Id, quantity = qty });
        if (added != null)
        {
            _bomRows.Add(new BomRow { BomItemId = added.Id, ComponentId = comp.Id, ComponentName = comp.Name, Quantity = added.Quantity });
        }
    }

    private async void BomList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (BomList.SelectedItem is not BomRow row) return;
        var ok = await _api.DeleteAsync($"AssemblyUnits/bom/{row.BomItemId}");
        if (ok) _bomRows.Remove(row);
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("Спецификация обновлена при добавлении/удалении. Закройте окно.", "Готово");
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}

public class BomRow
{
    public int BomItemId { get; set; }
    public int ComponentId { get; set; }
    public string ComponentName { get; set; } = "";
    public int Quantity { get; set; }
    public string DisplayText => $"{ComponentName} × {Quantity}";
}
