using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DataVault.Core.Entities;
using DataVault.Client.Screens;
using DataVault.Client.Services;

namespace DataVault.Client.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly ApiClient _api = new();
    private readonly LoginResponse? _currentUser;

    [ObservableProperty] private ObservableCollection<Resource> resources = new();
    [ObservableProperty] private ObservableCollection<WorkTask> tasks = new();
    [ObservableProperty] private ObservableCollection<TaskKanbanColumn> taskColumns = new();
    [ObservableProperty] private ObservableCollection<TaskPhase> taskPhases = new();
    [ObservableProperty] private ObservableCollection<Category> categories = new();
    [ObservableProperty] private ObservableCollection<ResourceBalance> resourceBalances = new();
    [ObservableProperty] private ObservableCollection<ResourceTransaction> resourceTransactions = new();
    [ObservableProperty] private ObservableCollection<Verification> verifications = new();
    [ObservableProperty] private ObservableCollection<Remark> remarks = new();
    [ObservableProperty] private Resource? selectedResource;
    [ObservableProperty] private WorkTask? selectedTask;
    [ObservableProperty] private string filterText = string.Empty;
    [ObservableProperty] private int activePageIndex;
    [ObservableProperty] private int tasksToday;
    [ObservableProperty] private int tasksInProgress;
    [ObservableProperty] private ObservableCollection<LowStockDto> lowStockItems = new();
    [ObservableProperty] private ObservableCollection<UrgentTaskDto> urgentTasksList = new();
    [ObservableProperty] private bool allowEdit;
    [ObservableProperty] private bool allowVerification;
    [ObservableProperty] private bool allowTasks = true;
    [ObservableProperty] private string scanCode = string.Empty;
    [ObservableProperty] private int lowStockCount;

    public MainViewModel()
    {
        _currentUser = App.Current.Properties["CurrentUser"] as LoginResponse;
        var roleId = _currentUser?.RoleId ?? 0;
        AllowEdit = roleId == 1 || roleId == 2 || roleId == 4;
        AllowVerification = roleId <= 3;
        AllowTasks = true;
        _ = RefreshAllAsync();
    }

    private async Task RefreshAllAsync()
    {
        await RefreshResourcesAsync();
        await RefreshTasksAsync();
        await RefreshPhasesAsync();
        await RefreshCategoriesAsync();
        await RefreshOverviewAsync();
        await RefreshVerificationsAsync();
    }

    [RelayCommand]
    private async Task RefreshResourcesAsync()
    {
        try
        {
            var path = $"Resources?search={Uri.EscapeDataString(FilterText)}&sortBy=name&ascending=true";
            var list = await _api.GetAsync<List<Resource>>(path) ?? new List<Resource>();
            Resources.Clear();
            foreach (var r in list) Resources.Add(r);
        }
        catch (Exception ex) { MessageBox.Show("Ошибка загрузки ресурсов: " + ex.Message, "Ошибка"); }
    }

    [RelayCommand]
    private async Task RefreshTasksAsync()
    {
        try
        {
            var list = await _api.GetAsync<List<WorkTask>>("WorkTasks") ?? new List<WorkTask>();
            Tasks.Clear();
            foreach (var t in list) Tasks.Add(t);
            RefreshKanbanColumns();
        }
        catch (Exception ex) { MessageBox.Show("Ошибка загрузки задач: " + ex.Message, "Ошибка"); }
    }

    private void RefreshKanbanColumns()
    {
        var phases = TaskPhases.ToList();
        TaskColumns.Clear();
        foreach (var p in phases)
        {
            var colTasks = Tasks.Where(t => t.PhaseId == p.Id).ToList();
            TaskColumns.Add(new TaskKanbanColumn { PhaseId = p.Id, PhaseName = p.Name, Tasks = colTasks });
        }
    }

    [RelayCommand]
    private async Task RefreshPhasesAsync()
    {
        try
        {
            var list = await _api.GetAsync<List<TaskPhase>>("TaskPhases") ?? new List<TaskPhase>();
            TaskPhases.Clear();
            foreach (var p in list) TaskPhases.Add(p);
        }
        catch { }
    }

    [RelayCommand]
    private async Task RefreshCategoriesAsync()
    {
        try
        {
            var list = await _api.GetAsync<List<Category>>("Categories") ?? new List<Category>();
            Categories.Clear();
            foreach (var c in list) Categories.Add(c);
        }
        catch { }
    }

    [RelayCommand]
    private async Task RefreshOverviewAsync()
    {
        try
        {
            var data = await _api.GetAsync<OverviewData>("Analytics/overview");
            if (data != null)
            {
                TasksToday = data.TasksToday;
                TasksInProgress = data.TasksInProgress;
                LowStockItems.Clear();
                foreach (var x in data.LowStock ?? new List<LowStockDto>()) LowStockItems.Add(x);
                LowStockCount = LowStockItems.Count;
                UrgentTasksList.Clear();
                foreach (var x in data.UrgentTasks ?? new List<UrgentTaskDto>()) UrgentTasksList.Add(x);
            }
        }
        catch { }
    }

    [RelayCommand]
    private async Task RefreshVerificationsAsync()
    {
        try
        {
            var listV = await _api.GetAsync<List<Verification>>("Verifications/list") ?? new List<Verification>();
            var listR = await _api.GetAsync<List<Remark>>("Verifications/remarks") ?? new List<Remark>();
            Verifications.Clear();
            Remarks.Clear();
            foreach (var v in listV.Take(50)) Verifications.Add(v);
            foreach (var r in listR.Take(50)) Remarks.Add(r);
        }
        catch { }
    }

    [RelayCommand]
    private async Task ShowTransactionsAsync()
    {
        var rid = SelectedResource?.Id;
        try
        {
            var path = rid.HasValue ? $"Inventory/transactions?resourceId={rid}&limit=50" : "Inventory/transactions?limit=50";
            var list = await _api.GetAsync<List<ResourceTransaction>>(path) ?? new List<ResourceTransaction>();
            var d = new TransactionsDialog(list, rid);
            d.ShowDialog();
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Ошибка"); }
    }

    [RelayCommand]
    private async Task OpenCategoryCompositionAsync()
    {
        try
        {
            var cats = Categories.ToList();
            var resList = await _api.GetAsync<List<Resource>>("Resources") ?? new List<Resource>();
            List<CategoryItem>? currentItems = null;
            var catId = cats.Count > 0 ? cats[0].Id : (int?)null;
            if (catId.HasValue)
                currentItems = await _api.GetAsync<List<CategoryItem>>($"Categories/{catId}/items");
            var d = new CategoryCompositionWindow(cats, resList, currentItems, catId);
            d.ShowDialog();
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Ошибка"); }
    }

    [RelayCommand]
    private void CreateTask()
    {
        if (Categories.Count == 0) { MessageBox.Show("Нет категорий.", "Внимание"); return; }
        var d = new TaskFormDialog(Categories.ToList(), TaskPhases.ToList());
        if (d.ShowDialog() != true) return;
        var catId = d.SelectedCategoryId;
        var qty = d.Quantity;
        var plannedFinish = d.PlannedFinishAt;
        if (catId <= 0) return;
        _ = CreateTaskAsync(catId, qty, plannedFinish);
    }

    private async Task CreateTaskAsync(int categoryId, int qty, DateTime? plannedFinish)
    {
        try
        {
            var body = new { categoryId, quantity = qty, userId = _currentUser?.UserId, plannedFinishAt = plannedFinish };
            var created = await _api.PostAsync<WorkTask>("WorkTasks", body);
            if (created != null) { await RefreshTasksAsync(); await RefreshOverviewAsync(); MessageBox.Show("Задача создана", "Готово"); }
            else MessageBox.Show("Ошибка создания задачи.", "Ошибка");
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Ошибка"); }
    }

    [RelayCommand]
    private void UpdateTaskPhase()
    {
        if (SelectedTask == null) { MessageBox.Show("Выберите задачу.", "Внимание"); return; }
        var d = new PhaseFormDialog(TaskPhases.ToList(), SelectedTask.PhaseId);
        if (d.ShowDialog() != true) return;
        _ = UpdatePhaseAsync(SelectedTask.Id, d.SelectedPhaseId);
    }

    private async Task UpdatePhaseAsync(int taskId, int phaseId)
    {
        try
        {
            var ok = await _api.PutAsync($"WorkTasks/{taskId}/phase", new { phaseId });
            if (ok) { await RefreshTasksAsync(); await RefreshOverviewAsync(); MessageBox.Show("Фаза обновлена.", "Готово"); }
            else MessageBox.Show("Ошибка обновления.", "Ошибка");
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Ошибка"); }
    }

    [RelayCommand]
    private async Task AddResource()
    {
        var vendors = new List<Vendor>();
        try { vendors = await _api.GetAsync<List<Vendor>>("Vendors") ?? new List<Vendor>(); } catch { }
        var d = new ResourceFormDialog(new Resource { Code = "RES-", MinStock = 0, MaxStock = 1000 }, vendors);
        if (d.ShowDialog() != true) return;
        await CreateResourceAsync(d.Resource);
    }

    private async Task CreateResourceAsync(Resource res)
    {
        try
        {
            var body = new { code = res.Code.Trim(), name = res.Name.Trim(), resourceKind = res.ResourceKind ?? "material", manufacturer = res.Manufacturer ?? "", minStock = res.MinStock >= 0 ? res.MinStock : 0, maxStock = res.MaxStock > 0 ? res.MaxStock : 1000, vendorId = res.VendorId };
            var created = await _api.PostAsync<Resource>("Resources", body);
            if (created != null) { await RefreshResourcesAsync(); MessageBox.Show("Ресурс добавлен", "Готово"); }
            else MessageBox.Show("Ошибка ответа сервера.", "Ошибка");
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Ошибка"); }
    }

    [RelayCommand]
    private async Task EditResource()
    {
        if (SelectedResource == null) return;
        var vendors = new List<Vendor>();
        try { vendors = await _api.GetAsync<List<Vendor>>("Vendors") ?? new List<Vendor>(); } catch { }
        var d = new ResourceFormDialog(SelectedResource, vendors);
        if (d.ShowDialog() != true) return;
        await UpdateResourceAsync(d.Resource);
    }

    private async Task UpdateResourceAsync(Resource res)
    {
        try
        {
            var ok = await _api.PutAsync($"Resources/{res.Id}", res);
            if (ok) { await RefreshResourcesAsync(); MessageBox.Show("Сохранено.", "Готово"); }
            else MessageBox.Show("Ошибка сохранения.", "Ошибка");
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Ошибка"); }
    }

    [RelayCommand]
    private async Task RemoveResourceAsync()
    {
        if (SelectedResource == null) return;
        if (MessageBox.Show("Удалить ресурс?", "Подтверждение", MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
        try
        {
            var ok = await _api.DeleteAsync($"Resources/{SelectedResource.Id}");
            if (ok) { await RefreshResourcesAsync(); MessageBox.Show("Удалено.", "Готово"); }
            else MessageBox.Show("Ошибка удаления.", "Ошибка");
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Ошибка"); }
    }

    [RelayCommand]
    private void AddVerification()
    {
        if (SelectedTask == null) { MessageBox.Show("Выберите задачу.", "Внимание"); return; }
        var d = new VerificationFormDialog(SelectedTask.Id);
        if (d.ShowDialog() != true) return;
        _ = CreateVerificationAsync(d.ProcedureName, d.ResultValue, d.Passed, d.CertificateNumber);
    }

    private async Task CreateVerificationAsync(string procedure, string? result, bool passed, string? cert)
    {
        if (SelectedTask == null) return;
        try
        {
            var body = new { workTaskId = SelectedTask.Id, procedureName = procedure, resultValue = result, passed, certificateNumber = cert };
            var created = await _api.PostAsync<Verification>("Verifications/add", body);
            if (created != null) { await RefreshVerificationsAsync(); MessageBox.Show("Проверка зарегистрирована.", "Готово"); }
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Ошибка"); }
    }

    [RelayCommand]
    private async Task ShowPassportAsync()
    {
        if (SelectedTask == null) { MessageBox.Show("Выберите задачу.", "Внимание"); return; }
        try
        {
            var passport = await _api.GetAsync<object>($"Documents/passport/{SelectedTask.Id}");
            if (passport != null)
            {
                var json = System.Text.Json.JsonSerializer.Serialize(passport, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                var d = new PassportDialog(json, SelectedTask.Id);
                d.ShowDialog();
            }
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Ошибка"); }
    }

    [RelayCommand]
    private void AddRemark()
    {
        if (SelectedTask == null) { MessageBox.Show("Выберите задачу.", "Внимание"); return; }
        var d = new RemarkFormDialog(SelectedTask.Id);
        if (d.ShowDialog() != true) return;
        _ = CreateRemarkAsync(d.RemarkType, d.Description);
    }

    private async Task CreateRemarkAsync(string remarkType, string? description)
    {
        if (SelectedTask == null) return;
        try
        {
            var body = new { workTaskId = SelectedTask.Id, remarkType, description };
            var created = await _api.PostAsync<Remark>("Verifications/remark", body);
            if (created != null) { await RefreshVerificationsAsync(); MessageBox.Show("Замечание зарегистрировано.", "Готово"); }
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Ошибка"); }
    }

    [RelayCommand]
    private async Task ScanCodeAsync(string? code)
    {
        var c = (code ?? ScanCode).Trim();
        if (string.IsNullOrEmpty(c)) return;
        try
        {
            var res = await _api.GetAsync<Resource>($"Resources/bycode/{Uri.EscapeDataString(c)}");
            if (res != null)
            {
                var inList = Resources.FirstOrDefault(x => x.Id == res.Id);
                SelectedResource = inList ?? res;
                if (inList == null) { Resources.Insert(0, res); SelectedResource = res; }
                ScanCode = string.Empty;
                MessageBox.Show($"Найден: {res.Code} — {res.Name}", "Сканирование");
            }
            else
                MessageBox.Show("Ресурс не найден.", "Сканирование");
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Ошибка"); }
    }

    [RelayCommand]
    private async Task SendLowStockNotifyAsync()
    {
        try
        {
            var result = await _api.PostAsync<object>("Alerts/low-stock-notify", new { });
            MessageBox.Show(result != null ? "Запрос отправки выполнен." : "Ошибка.", "Уведомление");
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Ошибка"); }
    }

    [RelayCommand]
    private void EndSession()
    {
        App.Current.Properties.Remove("CurrentUser");
        new LoginWindow().Show();
        Application.Current.Windows.OfType<MainWindow>().First().Close();
    }

    [RelayCommand] private void GoToOverview() => ActivePageIndex = 4;
    [RelayCommand] private void GoToResources() => ActivePageIndex = 1;
    [RelayCommand] private void GoToTasks() => ActivePageIndex = 2;
    [RelayCommand] private void GoToVerifications() => ActivePageIndex = 3;
    [RelayCommand] private void GoToSummary() => ActivePageIndex = 0;

    [RelayCommand]
    private async Task ExportResourcesAsync()
    {
        try
        {
            var url = $"/api/Analytics/export/resources?search={Uri.EscapeDataString(FilterText)}";
            var bytes = await _api.GetBytesAsync(url);
            if (bytes != null)
            {
                var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "resources_export.xlsx");
                await File.WriteAllBytesAsync(path, bytes);
                MessageBox.Show("Файл сохранён: " + path, "Готово");
            }
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Ошибка"); }
    }

    [RelayCommand]
    private async Task ExportTasksAsync()
    {
        try
        {
            var bytes = await _api.GetBytesAsync("Analytics/export/tasks");
            if (bytes != null)
            {
                var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "tasks_export.xlsx");
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
            var bytes = await _api.GetBytesAsync("Analytics/export/overview-pdf");
            if (bytes != null)
            {
                var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "overview.pdf");
                await File.WriteAllBytesAsync(path, bytes);
                MessageBox.Show("PDF сохранён: " + path, "Готово");
            }
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Ошибка"); }
    }

    partial void OnFilterTextChanged(string value) => _ = RefreshResourcesAsync();
}

public class OverviewData
{
    public int TasksToday { get; set; }
    public int TasksInProgress { get; set; }
    public List<LowStockDto>? LowStock { get; set; }
    public List<UrgentTaskDto>? UrgentTasks { get; set; }
}

public class LowStockDto
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public int Quantity { get; set; }
    public int MinStock { get; set; }
}

public class UrgentTaskDto
{
    public int Id { get; set; }
    public string CategoryName { get; set; } = "";
    public string PhaseName { get; set; } = "";
    public DateTime? PlannedFinishAt { get; set; }
}

public class TaskKanbanColumn
{
    public int PhaseId { get; set; }
    public string PhaseName { get; set; } = "";
    public List<WorkTask> Tasks { get; set; } = new();
}
