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

    // Holiday Calendar
    public async Task<List<HolidayCalendarSummary>> GetHolidayCalendarsAsync(Guid branchId)
    {
        var response = await http.GetAsync($"/api/branches/{branchId}/holiday-calendars");
        if (!response.IsSuccessStatusCode) return [];
        return await response.Content.ReadFromJsonAsync<List<HolidayCalendarSummary>>() ?? [];
    }

    public async Task<HolidayCalendarDetails?> GetHolidayCalendarByIdAsync(Guid branchId, Guid calendarId)
    {
        var response = await http.GetAsync($"/api/branches/{branchId}/holiday-calendars/{calendarId}");
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<HolidayCalendarDetails>();
    }

    public async Task<List<HolidayItem>> GetHolidaysInRangeAsync(Guid branchId, DateOnly start, DateOnly end)
    {
        var response = await http.GetAsync($"/api/branches/{branchId}/holiday-calendars/holidays/range?startDate={start:yyyy-MM-dd}&endDate={end:yyyy-MM-dd}");
        if (!response.IsSuccessStatusCode) return [];
        return await response.Content.ReadFromJsonAsync<List<HolidayItem>>() ?? [];
    }

    public async Task<Guid> CreateHolidayCalendarAsync(Guid branchId, CreateHolidayCalendarRequest request)
    {
        var response = await http.PostAsJsonAsync($"/api/branches/{branchId}/holiday-calendars", request);
        await EnsureSuccessOrThrowAsync(response);
        return await response.Content.ReadFromJsonAsync<Guid>();
    }

    public async Task AddHolidaysAsync(Guid branchId, Guid calendarId, AddHolidaysRequest request)
    {
        var response = await http.PostAsJsonAsync($"/api/branches/{branchId}/holiday-calendars/{calendarId}/holidays", request);
        await EnsureSuccessOrThrowAsync(response);
    }

    public async Task RemoveHolidayAsync(Guid branchId, Guid calendarId, DateOnly date)
    {
        var response = await http.DeleteAsync($"/api/branches/{branchId}/holiday-calendars/{calendarId}/holidays/{date:yyyy-MM-dd}");
        await EnsureSuccessOrThrowAsync(response);
    }

    // Domínio Mappings
    public async Task<List<DominioMappingDto>> GetDominioMappingsAsync(Guid branchId)
    {
        var response = await http.GetAsync($"/api/branches/{branchId}/dominio-mappings");
        if (!response.IsSuccessStatusCode) return [];
        return await response.Content.ReadFromJsonAsync<List<DominioMappingDto>>() ?? [];
    }

    public async Task SaveDominioMappingsAsync(Guid branchId, IEnumerable<SaveDominioMappingRequest> mappings)
    {
        var response = await http.PutAsJsonAsync($"/api/branches/{branchId}/dominio-mappings", mappings);
        await EnsureSuccessOrThrowAsync(response);
    }

    public async Task<byte[]> ExportDominioAsync(Guid branchId, string month)
    {
        var response = await http.GetAsync($"/api/branches/{branchId}/exports/dominio?month={month}");
        await EnsureSuccessOrThrowAsync(response);
        return await response.Content.ReadAsByteArrayAsync();
    }

    public async Task<byte[]> ExportCsvAsync(Guid branchId, DateOnly start, DateOnly end)
    {
        var response = await http.GetAsync($"/api/branches/{branchId}/exports/csv?startDate={start:yyyy-MM-dd}&endDate={end:yyyy-MM-dd}");
        await EnsureSuccessOrThrowAsync(response);
        return await response.Content.ReadAsByteArrayAsync();
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
