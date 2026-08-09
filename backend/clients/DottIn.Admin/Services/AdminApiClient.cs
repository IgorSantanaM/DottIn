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

    public async Task<byte[]> ExportDominioAsync(
        Guid branchId,
        string month,
        string companyCode,
        string normalRubricCode,
        string nocturnalRubricCode,
        string holidayRubricCode,
        string processType)
    {
        var response = await http.GetAsync(
            $"/api/branches/{branchId}/exports/dominio?month={month}" +
            $"&companyCode={Uri.EscapeDataString(companyCode)}" +
            $"&normalRubricCode={Uri.EscapeDataString(normalRubricCode)}" +
            $"&nocturnalRubricCode={Uri.EscapeDataString(nocturnalRubricCode)}" +
            $"&holidayRubricCode={Uri.EscapeDataString(holidayRubricCode)}" +
            $"&processType={Uri.EscapeDataString(processType)}");
        await EnsureSuccessOrThrowAsync(response);
        return await response.Content.ReadAsByteArrayAsync();
    }

    public async Task<byte[]> ExportCsvAsync(Guid branchId, DateOnly start, DateOnly end)
    {
        var response = await http.GetAsync($"/api/branches/{branchId}/exports/csv?startDate={start:yyyy-MM-dd}&endDate={end:yyyy-MM-dd}");
        await EnsureSuccessOrThrowAsync(response);
        return await response.Content.ReadAsByteArrayAsync();
    }

    // Billing
    public async Task<List<SubscriptionPlan>> GetSubscriptionPlansAsync(CancellationToken cancellationToken = default)
    {
        var result = await http.GetFromJsonAsync<List<SubscriptionPlan>>(
            "/api/billing/plans",
            cancellationToken);
        return result ?? [];
    }

    public async Task<BillingInfo?> GetBillingInfoAsync(CancellationToken cancellationToken = default)
    {
        using var response = await http.GetAsync("/api/billing/subscription", cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;

        await EnsureSuccessOrThrowAsync(response);
        return await response.Content.ReadFromJsonAsync<BillingInfo>(cancellationToken: cancellationToken);
    }

    public async Task<string> CreateCheckoutSessionAsync(Guid planId, CancellationToken cancellationToken = default)
    {
        using var response = await http.PostAsJsonAsync(
            "/api/billing/checkout-session",
            new CreateCheckoutSessionRequest(planId),
            cancellationToken);
        await EnsureSuccessOrThrowAsync(response);

        var result = await response.Content.ReadFromJsonAsync<CheckoutSessionResponse>(cancellationToken: cancellationToken);
        if (string.IsNullOrWhiteSpace(result?.CheckoutUrl))
            throw new ApiException("Não foi possível iniciar o pagamento.");

        return result.CheckoutUrl;
    }

    public async Task<string> CreatePortalSessionAsync(CancellationToken cancellationToken = default)
    {
        using var response = await http.PostAsync("/api/billing/portal-session", null, cancellationToken);
        await EnsureSuccessOrThrowAsync(response);

        var result = await response.Content.ReadFromJsonAsync<PortalSessionResponse>(cancellationToken: cancellationToken);
        if (string.IsNullOrWhiteSpace(result?.PortalUrl))
            throw new ApiException("Não foi possível abrir o portal de cobrança.");

        return result.PortalUrl;
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

            using var document = JsonDocument.Parse(body);
            if (document.RootElement.TryGetProperty("message", out var message) ||
                document.RootElement.TryGetProperty("Message", out message))
            {
                var value = message.GetString();
                if (!string.IsNullOrWhiteSpace(value))
                    throw new ApiException(value);
            }
        }
        catch (JsonException) { }

        throw new ApiException("Ocorreu um erro inesperado. Tente novamente.");
    }
}
