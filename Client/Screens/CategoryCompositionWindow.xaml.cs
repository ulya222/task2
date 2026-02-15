using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DataVault.Core.Entities;
using DataVault.Client.Services;

namespace DataVault.Client.Screens;

public partial class CategoryCompositionWindow : Window
{
    private readonly ApiClient _api = new();
    private readonly ObservableCollection<Resource> _availableResources = new();
    private readonly ObservableCollection<CategoryItemRow> _itemRows = new();
    private int? _selectedCategoryId;

    public CategoryCompositionWindow(List<Category> categories, List<Resource> resources, List<CategoryItem>? currentItems, int? selectedCategoryId)
    {
        InitializeComponent();
        CategoryCombo.ItemsSource = categories;
        if (selectedCategoryId.HasValue) CategoryCombo.SelectedValue = selectedCategoryId;
        else if (categories.Count > 0) CategoryCombo.SelectedIndex = 0;
        _selectedCategoryId = selectedCategoryId ?? (categories.Count > 0 ? categories[0].Id : (int?)null);
        _availableResources.Clear();
        foreach (var r in resources) _availableResources.Add(r);
        ResourcesList.ItemsSource = _availableResources;
        ResourcesList.DisplayMemberPath = "Name";
        ItemsList.ItemsSource = _itemRows;
        ItemsList.DisplayMemberPath = "DisplayText";
        if (currentItems != null)
            foreach (var i in currentItems)
                _itemRows.Add(new CategoryItemRow { ItemId = i.Id, ResourceId = i.ResourceId, ResourceName = i.Resource?.Name ?? i.ResourceId.ToString(), Quantity = i.Quantity });
    }

    private async void CategoryCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CategoryCombo.SelectedValue is not int id) return;
        _selectedCategoryId = id;
        _itemRows.Clear();
        try
        {
            var items = await _api.GetAsync<List<CategoryItem>>($"Categories/{id}/items");
            if (items != null)
                foreach (var i in items)
                    _itemRows.Add(new CategoryItemRow { ItemId = i.Id, ResourceId = i.ResourceId, ResourceName = i.Resource?.Name ?? i.ResourceId.ToString(), Quantity = i.Quantity });
        }
        catch { }
    }

    private async void ResourcesList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (CategoryCombo.SelectedValue is not int categoryId || ResourcesList.SelectedItem is not Resource res) return;
        var added = await _api.PostAsync<CategoryItem>($"Categories/{categoryId}/items", new { resourceId = res.Id, quantity = 1 });
        if (added != null)
            _itemRows.Add(new CategoryItemRow { ItemId = added.Id, ResourceId = res.Id, ResourceName = res.Name, Quantity = added.Quantity });
    }

    private async void ItemsList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (ItemsList.SelectedItem is not CategoryItemRow row) return;
        var ok = await _api.DeleteAsync($"Categories/items/{row.ItemId}");
        if (ok) _itemRows.Remove(row);
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}

public class CategoryItemRow
{
    public int ItemId { get; set; }
    public int ResourceId { get; set; }
    public string ResourceName { get; set; } = "";
    public int Quantity { get; set; }
    public string DisplayText => $"{ResourceName} × {Quantity}";
}
