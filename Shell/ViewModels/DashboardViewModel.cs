using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TelecomProd.Core.Entities;
using TelecomProd.Shell.Screens;
using TelecomProd.Shell.Services;

namespace TelecomProd.Shell.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    private readonly ApiClient _api = new();
    private readonly LoginResponse? _currentUser;

    [ObservableProperty] private ObservableCollection<Component> components = new();
    [ObservableProperty] private ObservableCollection<ProductionOrder> orders = new();
    [ObservableProperty] private ObservableCollection<OrderKanbanColumn> orderColumns = new();
    [ObservableProperty] private ObservableCollection<OrderStatus> orderStatuses = new();
    [ObservableProperty] private ObservableCollection<AssemblyUnit> assemblyUnits = new();
    [ObservableProperty] private ObservableCollection<StockBalance> stockBalances = new();
    [ObservableProperty] private ObservableCollection<StockMovement> stockMovements = new();
    [ObservableProperty] private ObservableCollection<QualityTest> qualityTests = new();
    [ObservableProperty] private ObservableCollection<DefectRecord> defects = new();
    [ObservableProperty] private Component? selectedComponent;
    [ObservableProperty] private ProductionOrder? selectedOrder;
    [ObservableProperty] private string filterText = string.Empty;
    [ObservableProperty] private int activePageIndex;
    [ObservableProperty] private int ordersToday;
    [ObservableProperty] private int ordersInProgress;
    [ObservableProperty] private ObservableCollection<LowStockItem> lowStockItems = new();
    [ObservableProperty] private ObservableCollection<UrgentOrderItem> urgentOrdersList = new();
    [ObservableProperty] private bool allowEdit;
    [ObservableProperty] private bool allowQuality;
    [ObservableProperty] private bool allowOrders;
    [ObservableProperty] private string barcodeText = string.Empty;
    [ObservableProperty] private int lowStockCount;

    public DashboardViewModel()
    {
        _currentUser = App.Current.Properties["CurrentUser"] as LoginResponse;
        var roleId = _currentUser?.RoleId ?? 0;
        AllowEdit = roleId == 1 || roleId == 2 || roleId == 4; // Admin, Chief, Storekeeper
        AllowQuality = roleId <= 3; // Admin, Chief, Technologist
        AllowOrders = true;
        _ = RefreshAllAsync();
    }

    private async Task RefreshAllAsync()
    {
        await RefreshComponentsAsync();
        await RefreshOrdersAsync();
        await RefreshStatusesAsync();
        await RefreshAssemblyUnitsAsync();
        await RefreshDashboardAsync();
        await RefreshQualityAsync();
    }

    [RelayCommand]
    private async Task RefreshComponentsAsync()
    {
        try
        {
            var path = $"Components?search={Uri.EscapeDataString(FilterText)}&sortBy=name&ascending=true";
            var list = await _api.GetAsync<List<Component>>(path) ?? new List<Component>();
            Components.Clear();
            foreach (var c in list) Components.Add(c);
        }
        catch (Exception ex) { MessageBox.Show("Ошибка загрузки компонентов: " + ex.Message, "Ошибка"); }
    }

    [RelayCommand]
    private async Task RefreshOrdersAsync()
    {
        try
        {
            var list = await _api.GetAsync<List<ProductionOrder>>("ProductionOrders") ?? new List<ProductionOrder>();
            Orders.Clear();
            foreach (var o in list) Orders.Add(o);
            RefreshKanbanColumns();
        }
        catch (Exception ex) { MessageBox.Show("Ошибка загрузки заказов: " + ex.Message, "Ошибка"); }
    }

    private void RefreshKanbanColumns()
    {
        var statuses = OrderStatuses.ToList();
        OrderColumns.Clear();
        foreach (var s in statuses)
        {
            var colOrders = Orders.Where(o => o.StatusId == s.Id).ToList();
            OrderColumns.Add(new OrderKanbanColumn { StatusId = s.Id, StatusName = s.Name, Orders = colOrders });
        }
    }

    [RelayCommand]
    private async Task RefreshStatusesAsync()
    {
        try
        {
            var list = await _api.GetAsync<List<OrderStatus>>("OrderStatuses") ?? new List<OrderStatus>();
            OrderStatuses.Clear();
            foreach (var s in list) OrderStatuses.Add(s);
        }
        catch { /* ignore */ }
    }

    [RelayCommand]
    private async Task RefreshAssemblyUnitsAsync()
    {
        try
        {
            var list = await _api.GetAsync<List<AssemblyUnit>>("AssemblyUnits") ?? new List<AssemblyUnit>();
            AssemblyUnits.Clear();
            foreach (var a in list) AssemblyUnits.Add(a);
        }
        catch { /* ignore */ }
    }

    [RelayCommand]
    private async Task RefreshDashboardAsync()
    {
        try
        {
            var data = await _api.GetAsync<DashboardData>("Reports/dashboard");
            if (data != null)
            {
                OrdersToday = data.OrdersToday;
                OrdersInProgress = data.OrdersInProgress;
                LowStockItems.Clear();
                foreach (var x in data.LowStock ?? new List<LowStockItem>()) LowStockItems.Add(x);
                LowStockCount = LowStockItems.Count;
                UrgentOrdersList.Clear();
                foreach (var x in data.UrgentOrders ?? new List<UrgentOrderItem>()) UrgentOrdersList.Add(x);
            }
        }
        catch { /* ignore */ }
    }

    [RelayCommand]
    private async Task RefreshQualityAsync()
    {
        try
        {
            var tests = await _api.GetAsync<List<QualityTest>>("Quality/tests") ?? new List<QualityTest>();
            var defs = await _api.GetAsync<List<DefectRecord>>("Quality/defects") ?? new List<DefectRecord>();
            QualityTests.Clear();
            Defects.Clear();
            foreach (var t in tests.Take(50)) QualityTests.Add(t);
            foreach (var d in defs.Take(50)) Defects.Add(d);
        }
        catch { /* ignore */ }
    }

    [RelayCommand]
    private async Task ShowMovementsAsync()
    {
        var cid = SelectedComponent?.Id;
        try
        {
            var path = cid.HasValue ? $"Stock/movements?componentId={cid}&limit=50" : "Stock/movements?limit=50";
            var list = await _api.GetAsync<List<StockMovement>>(path) ?? new List<StockMovement>();
            var d = new MovementsDialog(list, cid);
            d.ShowDialog();
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Ошибка"); }
    }

    [RelayCommand]
    private async Task RefreshStockAsync()
    {
        try
        {
            var list = await _api.GetAsync<List<StockBalance>>("Stock/balances") ?? new List<StockBalance>();
            StockBalances.Clear();
            foreach (var b in list) StockBalances.Add(b);
        }
        catch (Exception ex) { MessageBox.Show("Ошибка: " + ex.Message, "Ошибка"); }
    }

    [RelayCommand]
    private async Task OpenBomConstructorAsync()
    {
        try
        {
            var units = AssemblyUnits.ToList();
            var comps = await _api.GetAsync<List<Component>>("Components") ?? new List<Component>();
            List<BomItem>? currentBom = null;
            var unitId = units.Count > 0 ? units[0].Id : (int?)null;
            if (unitId.HasValue)
                currentBom = await _api.GetAsync<List<BomItem>>($"AssemblyUnits/{unitId}/bom");
            var d = new BomConstructorWindow(units, comps, currentBom, unitId);
            d.ShowDialog();
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Ошибка"); }
    }

    [RelayCommand]
    private void CreateOrder()
    {
        if (AssemblyUnits.Count == 0) { MessageBox.Show("Нет узлов сборки.", "Внимание"); return; }
        var d = new OrderFormDialog(AssemblyUnits.ToList(), OrderStatuses.ToList());
        if (d.ShowDialog() != true) return;
        var unitId = d.SelectedUnitId;
        var qty = d.Quantity;
        var plannedFinish = d.PlannedFinishAt;
        if (unitId <= 0) return;
        _ = CreateOrderAsync(unitId, qty, plannedFinish);
    }

    private async Task CreateOrderAsync(int unitId, int qty, DateTime? plannedFinish)
    {
        try
        {
            var body = new { assemblyUnitId = unitId, quantity = qty, userId = _currentUser?.UserId, plannedFinishAt = plannedFinish };
            var created = await _api.PostAsync<ProductionOrder>("ProductionOrders", body);
            if (created != null) { await RefreshOrdersAsync(); await RefreshDashboardAsync(); MessageBox.Show("Заказ создан", "Готово"); }
            else MessageBox.Show("Ошибка создания заказа.", "Ошибка");
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Ошибка"); }
    }

    [RelayCommand]
    private void UpdateOrderStatus()
    {
        if (SelectedOrder == null) { MessageBox.Show("Выберите заказ.", "Внимание"); return; }
        var d = new StatusFormDialog(OrderStatuses.ToList(), SelectedOrder.StatusId);
        if (d.ShowDialog() != true) return;
        _ = UpdateStatusAsync(SelectedOrder.Id, d.SelectedStatusId);
    }

    private async Task UpdateStatusAsync(int orderId, int statusId)
    {
        try
        {
            var ok = await _api.PutAsync($"ProductionOrders/{orderId}/status", new { statusId });
            if (ok) { await RefreshOrdersAsync(); await RefreshDashboardAsync(); MessageBox.Show("Статус обновлён.", "Готово"); }
            else MessageBox.Show("Ошибка обновления.", "Ошибка");
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Ошибка"); }
    }

    [RelayCommand]
    private async Task AddComponent()
    {
        var suppliers = new List<Supplier>();
        try { suppliers = await _api.GetAsync<List<Supplier>>("Suppliers") ?? new List<Supplier>(); } catch { }
        var d = new ComponentFormDialog(new Component { Code = "ELEC-", MinStock = 0, MaxStock = 1000 }, suppliers);
        if (d.ShowDialog() != true) return;
        await CreateComponentAsync(d.Component);
    }

    private async Task CreateComponentAsync(Component comp)
    {
        try
        {
            var componentType = string.IsNullOrWhiteSpace(comp.ComponentType) ? "passive,electronic" : comp.ComponentType;
            var body = new { code = comp.Code.Trim(), name = comp.Name.Trim(), componentType, manufacturer = comp.Manufacturer ?? "", minStock = comp.MinStock >= 0 ? comp.MinStock : 0, maxStock = comp.MaxStock > 0 ? comp.MaxStock : 1000, supplierId = comp.SupplierId };
            var created = await _api.PostAsync<Component>("Components", body);
            if (created != null) { await RefreshComponentsAsync(); MessageBox.Show("Компонент добавлен", "Готово"); }
            else MessageBox.Show("Ошибка ответа сервера.", "Ошибка");
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Ошибка"); }
    }

    [RelayCommand]
    private async Task EditComponent()
    {
        if (SelectedComponent == null) return;
        var suppliers = new List<Supplier>();
        try { suppliers = await _api.GetAsync<List<Supplier>>("Suppliers") ?? new List<Supplier>(); } catch { }
        var d = new ComponentFormDialog(SelectedComponent, suppliers);
        if (d.ShowDialog() != true) return;
        await UpdateComponentAsync(d.Component);
    }

    private async Task UpdateComponentAsync(Component comp)
    {
        try
        {
            var ok = await _api.PutAsync($"Components/{comp.Id}", comp);
            if (ok) { await RefreshComponentsAsync(); MessageBox.Show("Сохранено.", "Готово"); }
            else MessageBox.Show("Ошибка сохранения.", "Ошибка");
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Ошибка"); }
    }

    [RelayCommand]
    private async Task RemoveComponentAsync()
    {
        if (SelectedComponent == null) return;
        if (MessageBox.Show("Удалить компонент?", "Подтверждение", MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
        try
        {
            var ok = await _api.DeleteAsync($"Components/{SelectedComponent.Id}");
            if (ok) { await RefreshComponentsAsync(); MessageBox.Show("Удалено.", "Готово"); }
            else MessageBox.Show("Ошибка удаления.", "Ошибка");
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Ошибка"); }
    }

    [RelayCommand]
    private void AddQualityTest()
    {
        if (SelectedOrder == null) { MessageBox.Show("Выберите заказ.", "Внимание"); return; }
        var d = new QualityFormDialog(SelectedOrder.Id);
        if (d.ShowDialog() != true) return;
        _ = CreateQualityTestAsync(d.TestProcedure, d.MeasurementResult, d.Passed, d.CertificateNumber);
    }

    private async Task CreateQualityTestAsync(string procedure, string? result, bool passed, string? cert)
    {
        if (SelectedOrder == null) return;
        try
        {
            var body = new { productionOrderId = SelectedOrder.Id, testProcedure = procedure, measurementResult = result, passed, certificateNumber = cert };
            var created = await _api.PostAsync<QualityTest>("Quality/test", body);
            if (created != null) { await RefreshQualityAsync(); MessageBox.Show("Тест зарегистрирован.", "Готово"); }
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Ошибка"); }
    }

    [RelayCommand]
    private async Task ShowPassportAsync()
    {
        if (SelectedOrder == null) { MessageBox.Show("Выберите заказ.", "Внимание"); return; }
        try
        {
            var passport = await _api.GetAsync<object>($"Documents/passport/{SelectedOrder.Id}");
            if (passport != null)
            {
                var json = System.Text.Json.JsonSerializer.Serialize(passport, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                var d = new PassportDialog(json, SelectedOrder.Id);
                d.ShowDialog();
            }
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Ошибка"); }
    }

    [RelayCommand]
    private void AddDefect()
    {
        if (SelectedOrder == null) { MessageBox.Show("Выберите заказ.", "Внимание"); return; }
        var d = new DefectFormDialog(SelectedOrder.Id);
        if (d.ShowDialog() != true) return;
        _ = CreateDefectAsync(d.DefectType, d.Description);
    }

    private async Task CreateDefectAsync(string defectType, string? description)
    {
        if (SelectedOrder == null) return;
        try
        {
            var body = new { productionOrderId = SelectedOrder.Id, defectType, description };
            var created = await _api.PostAsync<DefectRecord>("Quality/defect", body);
            if (created != null) { await RefreshQualityAsync(); MessageBox.Show("Брак зарегистрирован.", "Готово"); }
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Ошибка"); }
    }

    [RelayCommand]
    private async Task BarcodeScannedAsync(string? code)
    {
        var c = (code ?? BarcodeText).Trim();
        if (string.IsNullOrEmpty(c)) return;
        try
        {
            var comp = await _api.GetAsync<Component>($"Components/bycode/{Uri.EscapeDataString(c)}");
            if (comp != null)
            {
                var inList = Components.FirstOrDefault(x => x.Id == comp.Id);
                SelectedComponent = inList ?? comp;
                if (inList == null) { Components.Insert(0, comp); SelectedComponent = comp; }
                BarcodeText = string.Empty;
                MessageBox.Show($"Найден: {comp.Code} — {comp.Name}", "Штрих-код");
            }
            else
                MessageBox.Show("Компонент не найден.", "Штрих-код");
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Ошибка"); }
    }

    [RelayCommand]
    private async Task SendLowStockEmailAsync()
    {
        try
        {
            var result = await _api.PostAsync<object>("Notifications/low-stock-email", new { });
            MessageBox.Show(result != null ? "Запрос отправки выполнен. Проверьте настройку Email в appsettings." : "Ошибка.", "Email");
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Ошибка"); }
    }

    [RelayCommand]
    private void EndSession()
    {
        App.Current.Properties.Remove("CurrentUser");
        new AuthScreen().Show();
        Application.Current.Windows.OfType<DashboardWindow>().First().Close();
    }

    [RelayCommand] private void GoToDashboard() => ActivePageIndex = 0;
    [RelayCommand] private void GoToComponents() => ActivePageIndex = 1;
    [RelayCommand] private void GoToOrders() => ActivePageIndex = 2;
    [RelayCommand] private void GoToQuality() => ActivePageIndex = 3;
    [RelayCommand] private void GoToReports() => ActivePageIndex = 4;

    [RelayCommand]
    private async Task ExportComponentsAsync()
    {
        try
        {
            var url = $"/api/Reports/export/components?search={Uri.EscapeDataString(FilterText)}";
            var bytes = await _api.GetBytesAsync(url);
            if (bytes != null)
            {
                var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "components_export.xlsx");
                await File.WriteAllBytesAsync(path, bytes);
                MessageBox.Show("Файл сохранён: " + path, "Готово");
            }
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Ошибка"); }
    }

    [RelayCommand]
    private async Task ExportOrdersAsync()
    {
        try
        {
            var bytes = await _api.GetBytesAsync("Reports/export/orders");
            if (bytes != null)
            {
                var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "orders_export.xlsx");
                await File.WriteAllBytesAsync(path, bytes);
                MessageBox.Show("Файл сохранён: " + path, "Готово");
            }
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Ошибка"); }
    }

    [RelayCommand]
    private async Task ExportPdfAsync()
    {
        try
        {
            var bytes = await _api.GetBytesAsync("Reports/export/dashboard-pdf");
            if (bytes != null)
            {
                var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "dashboard.pdf");
                await File.WriteAllBytesAsync(path, bytes);
                MessageBox.Show("PDF сохранён: " + path, "Готово");
            }
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Ошибка"); }
    }

    partial void OnFilterTextChanged(string value) => _ = RefreshComponentsAsync();
}

public class DashboardData
{
    public int OrdersToday { get; set; }
    public int OrdersInProgress { get; set; }
    public List<LowStockItem>? LowStock { get; set; }
    public List<UrgentOrderItem>? UrgentOrders { get; set; }
}

public class LowStockItem
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public int Quantity { get; set; }
    public int MinStock { get; set; }
}

public class UrgentOrderItem
{
    public int Id { get; set; }
    public string AssemblyUnit { get; set; } = "";
    public string Status { get; set; } = "";
    public DateTime? PlannedFinishAt { get; set; }
}

public class OrderKanbanColumn
{
    public int StatusId { get; set; }
    public string StatusName { get; set; } = "";
    public List<ProductionOrder> Orders { get; set; } = new();
}
