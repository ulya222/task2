using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace DataVault.Client.Services;

internal static class JsonOptions
{
    public static readonly JsonSerializerOptions Default = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
}

public class ApiClient
{
    private readonly HttpClient _http;

    public ApiClient()
    {
        _http = new HttpClient { BaseAddress = new Uri("http://127.0.0.1:5050") };
        _http.DefaultRequestHeaders.Add("Accept", "application/json");
    }

    public async Task<bool> CheckHealthAsync()
    {
        try { var r = await _http.GetAsync("/api/Health"); return r.IsSuccessStatusCode; }
        catch { return false; }
    }

    public async Task<LoginResponse?> LoginAsync(string login, string password)
    {
        var body = JsonSerializer.Serialize(new { login, password });
        var content = new StringContent(body, Encoding.UTF8, "application/json");
        var r = await _http.PostAsync("/api/Session/login", content);
        if (!r.IsSuccessStatusCode) return null;
        return await r.Content.ReadFromJsonAsync<LoginResponse>(JsonOptions.Default);
    }

    public async Task<T?> GetAsync<T>(string path)
    {
        var url = path.StartsWith("/") ? path : $"/api/{path}";
        var r = await _http.GetAsync(url);
        return r.IsSuccessStatusCode ? await r.Content.ReadFromJsonAsync<T>(JsonOptions.Default) : default;
    }

    public async Task<T?> PostAsync<T>(string path, object body)
    {
        var url = path.StartsWith("/") ? path : $"/api/{path}";
        var json = JsonSerializer.Serialize(body);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var r = await _http.PostAsync(url, content);
        if (r.IsSuccessStatusCode) return await r.Content.ReadFromJsonAsync<T>(JsonOptions.Default);
        var errBody = await r.Content.ReadAsStringAsync();
        throw new HttpRequestException($"{(int)r.StatusCode}: {ParseApiError(errBody)}");
    }

    private static string ParseApiError(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.TryGetProperty("detail", out var d)) return d.GetString() ?? json;
            if (root.TryGetProperty("title", out var t)) return t.GetString() ?? json;
            if (root.TryGetProperty("errors", out var errs) && errs.ValueKind == JsonValueKind.Object)
            {
                var parts = new List<string>();
                foreach (var p in errs.EnumerateObject())
                    foreach (var v in p.Value.EnumerateArray())
                        if (v.ValueKind == JsonValueKind.String) parts.Add($"{p.Name}: {v.GetString()}");
                if (parts.Count > 0) return string.Join("; ", parts);
            }
        }
        catch { }
        return string.IsNullOrWhiteSpace(json) ? "Ошибка сервера" : json;
    }

    public async Task<bool> PutAsync(string path, object body)
    {
        var url = path.StartsWith("/") ? path : $"/api/{path}";
        var json = JsonSerializer.Serialize(body);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var r = await _http.PutAsync(url, content);
        return r.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteAsync(string path)
    {
        var url = path.StartsWith("/") ? path : $"/api/{path}";
        var r = await _http.DeleteAsync(url);
        return r.IsSuccessStatusCode;
    }

    public async Task<byte[]?> GetBytesAsync(string path)
    {
        var url = path.StartsWith("/") ? path : $"/api/{path}";
        var r = await _http.GetAsync(url);
        return r.IsSuccessStatusCode ? await r.Content.ReadAsByteArrayAsync() : null;
    }
}

public class LoginResponse
{
    public int UserId { get; set; }
    public string Login { get; set; } = "";
    public string FullName { get; set; } = "";
    public int RoleId { get; set; }
    public string RoleName { get; set; } = "";
}
