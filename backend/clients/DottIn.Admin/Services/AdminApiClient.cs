using System.Net.Http.Json;
using System.Text.Json;
using DottIn.Admin.Models;

namespace DottIn.Admin.Services;

public class AdminApiClient(HttpClient http)
{
    public async Task<List<BranchSummary>> GetBranchesByOwnerAsync(Guid ownerId)
    {
        var result = await http.GetFromJsonAsync<List<BranchSummary>>($"/api/branches/owner/{ownerId}");
        return result ?? [];
    }

    public async Task<List<EmployeeSummary>> GetEmployeesByBranchAsync(Guid branchId)
    {
        var result = await http.GetFromJsonAsync<List<EmployeeSummary>>($"/api/branches/{branchId}/employees");
        return result ?? [];
    }

    public async Task<List<EmployeeSummary>> GetActiveEmployeesAsync(Guid branchId)
    {
        var result = await http.GetFromJsonAsync<List<EmployeeSummary>>($"/api/branches/{branchId}/employees/active");
        return result ?? [];
    }

    public async Task<List<TimeKeepingRecord>> GetBranchHistoryAsync(Guid branchId, DateOnly start, DateOnly? end = null)
    {
        var url = $"/api/timekeeping/branch/{branchId}/history?startDate={start:yyyy-MM-dd}";
        if (end.HasValue) url += $"&endDate={end:yyyy-MM-dd}";
        var result = await http.GetFromJsonAsync<List<TimeKeepingRecord>>(url);
        return result ?? [];
    }

    public async Task<List<TimeKeepingRecord>> GetEmployeeHistoryAsync(Guid employeeId, DateOnly start, DateOnly? end = null)
    {
        var url = $"/api/timekeeping/employee/{employeeId}/history?startDate={start:yyyy-MM-dd}";
        if (end.HasValue) url += $"&endDate={end:yyyy-MM-dd}";
        var result = await http.GetFromJsonAsync<List<TimeKeepingRecord>>(url);
        return result ?? [];
    }

    public async Task<TimeKeepingDetails?> GetTimeKeepingByIdAsync(Guid id)
    {
        return await http.GetFromJsonAsync<TimeKeepingDetails>($"/api/timekeeping/{id}");
    }

    public async Task ClockInAsync(ClockInRequest request)
    {
        var response = await http.PostAsJsonAsync("/api/timekeeping/clock-in", request);
        await EnsureSuccessOrThrowAsync(response);
    }

    public async Task ClockOutAsync(ClockOutRequest request)
    {
        var response = await http.PostAsJsonAsync("/api/timekeeping/clock-out", request);
        await EnsureSuccessOrThrowAsync(response);
    }

    public async Task BreakAsync(BreakRequest request)
    {
        var response = await http.PostAsJsonAsync("/api/timekeeping/break", request);
        await EnsureSuccessOrThrowAsync(response);
    }

    public async Task LogoutAsync()
    {
        try
        {
            await http.PostAsync("/api/auth/logout", null);
        }
        catch { }
    }

    private static async Task EnsureSuccessOrThrowAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode) return;

        var body = await response.Content.ReadAsStringAsync();
        try
        {
            var problem = JsonSerializer.Deserialize<ApiProblem>(body, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            if (!string.IsNullOrWhiteSpace(problem?.Title))
                throw new ApiException(problem.Title);
        }
        catch (JsonException) { }

        throw new ApiException("Ocorreu um erro inesperado. Tente novamente.");
    }
}
