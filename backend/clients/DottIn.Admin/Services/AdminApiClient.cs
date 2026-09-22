using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Components.WebAssembly.Http;
using DottIn.Admin.Models;

namespace DottIn.Admin.Services;

public class AdminApiClient(HttpClient http, AdminQueryCache cache, DashboardSessionCache? dashboardCache = null)
{
    public Task<List<BranchSummary>> GetBranchesByOwnerAsync(Guid ownerId, bool forceRefresh = false)
        => cache.GetOrCreateAsync(
            $"owner:{ownerId}:branches",
            TimeSpan.FromMinutes(5),
            async () => await http.GetFromJsonAsync<List<BranchSummary>>($"/api/branches/owner/{ownerId}") ?? [],
            forceRefresh);

    public Task<BranchClockResponse> GetBranchClockAsync(
        Guid branchId,
        CancellationToken cancellationToken = default)
        => cache.GetOrCreateAsync(
            $"branch:{branchId}:clock",
            TimeSpan.FromMinutes(5),
            async () => await http.GetFromJsonAsync<BranchClockResponse>(
                $"/api/branches/{branchId}/clock",
                cancellationToken) ?? throw new ApiException("Não foi possível obter o horário da filial."),
            cancellationToken: cancellationToken);

    public Task<List<EmployeeSummary>> GetEmployeesByBranchAsync(Guid branchId, bool forceRefresh = false)
        => cache.GetOrCreateAsync(
            $"branch:{branchId}:employees:all",
            TimeSpan.FromSeconds(30),
            async () => await http.GetFromJsonAsync<List<EmployeeSummary>>(
                $"/api/branches/{branchId}/employees") ?? [],
            forceRefresh);

    public Task<PagedResponse<EmployeeSummary>> GetPagedEmployeesAsync(
        Guid branchId,
        int pageNumber,
        int pageSize,
        string? search,
        bool? isActive,
        bool forceRefresh = false,
        CancellationToken cancellationToken = default)
    {
        var url = $"/api/branches/{branchId}/employees/paged?pageNumber={pageNumber}&pageSize={pageSize}";
        if (!string.IsNullOrWhiteSpace(search))
            url += $"&search={Uri.EscapeDataString(search.Trim())}";
        if (isActive.HasValue)
            url += $"&isActive={isActive.Value.ToString().ToLowerInvariant()}";

        return cache.GetOrCreateAsync(
            $"branch:{branchId}:employees:paged:{pageNumber}:{pageSize}:{search}:{isActive}",
            TimeSpan.FromSeconds(30),
            async () => await http.GetFromJsonAsync<PagedResponse<EmployeeSummary>>(url, cancellationToken)
                ?? new PagedResponse<EmployeeSummary>([], 0, 0),
            forceRefresh,
            cancellationToken);
    }
    public Task<List<EmployeeSummary>> GetActiveEmployeesAsync(Guid branchId, bool forceRefresh = false)
        => cache.GetOrCreateAsync(
            $"branch:{branchId}:employees:active",
            TimeSpan.FromSeconds(30),
            async () => await http.GetFromJsonAsync<List<EmployeeSummary>>(
                $"/api/branches/{branchId}/employees/active") ?? [],
            forceRefresh);

    public Task<List<TimeKeepingRecord>> GetBranchHistoryAsync(
        Guid branchId,
        DateOnly start,
        DateOnly? end = null,
        bool forceRefresh = false)
        => cache.GetOrCreateAsync(
            $"branch:{branchId}:history:{start:yyyyMMdd}:{end:yyyyMMdd}",
            TimeSpan.FromSeconds(15),
            async () =>
            {
                var url = $"/api/timekeeping/branch/{branchId}/history?startDate={start:yyyy-MM-dd}";
                if (end.HasValue) url += $"&endDate={end:yyyy-MM-dd}";
                return await http.GetFromJsonAsync<List<TimeKeepingRecord>>(url) ?? [];
            },
            forceRefresh);

    public Task<PagedResponse<TimeKeepingRecord>> GetPagedBranchHistoryAsync(
        Guid branchId,
        DateOnly start,
        DateOnly? end,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default,
        bool forceRefresh = false)
        => cache.GetOrCreateAsync(
            $"branch:{branchId}:history:paged:{start:yyyyMMdd}:{end:yyyyMMdd}:{pageNumber}:{pageSize}",
            TimeSpan.FromSeconds(15),
            async () =>
            {
                var url = $"/api/timekeeping/branch/{branchId}/history/paged?startDate={start:yyyy-MM-dd}" +
                          $"&pageNumber={pageNumber}&pageSize={pageSize}";
                if (end.HasValue) url += $"&endDate={end:yyyy-MM-dd}";
                return await http.GetFromJsonAsync<PagedResponse<TimeKeepingRecord>>(url, cancellationToken)
                       ?? new PagedResponse<TimeKeepingRecord>([], 0, 0);
            },
            forceRefresh,
            cancellationToken);

    public Task<PagedResponse<PersonalTimeKeepingRecord>> GetPagedEmployeeHistoryAsync(
        Guid employeeId,
        DateOnly start,
        DateOnly? end,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default,
        bool forceRefresh = false)
        => cache.GetOrCreateAsync(
            $"employee:{employeeId}:history:paged:{start:yyyyMMdd}:{end:yyyyMMdd}:{pageNumber}:{pageSize}",
            TimeSpan.FromSeconds(15),
            async () =>
            {
                var url = $"/api/timekeeping/employee/{employeeId}/history/paged?startDate={start:yyyy-MM-dd}" +
                          $"&pageNumber={pageNumber}&pageSize={pageSize}";
                if (end.HasValue)
                    url += $"&endDate={end:yyyy-MM-dd}";
                return await http.GetFromJsonAsync<PagedResponse<PersonalTimeKeepingRecord>>(url, cancellationToken)
                       ?? new PagedResponse<PersonalTimeKeepingRecord>([], 0, 0);
            },
            forceRefresh,
            cancellationToken);
    public Task<List<TimeKeepingRecord>> GetEmployeeHistoryAsync(
        Guid employeeId,
        DateOnly start,
        DateOnly? end = null,
        bool forceRefresh = false)
        => cache.GetOrCreateAsync(
            $"employee:{employeeId}:history:{start:yyyyMMdd}:{end:yyyyMMdd}",
            TimeSpan.FromSeconds(15),
            async () =>
            {
                var url = $"/api/timekeeping/employee/{employeeId}/history?startDate={start:yyyy-MM-dd}";
                if (end.HasValue) url += $"&endDate={end:yyyy-MM-dd}";
                return await http.GetFromJsonAsync<List<TimeKeepingRecord>>(url) ?? [];
            },
            forceRefresh);

    public Task<TimeKeepingDetails?> GetTimeKeepingByIdAsync(Guid id, bool forceRefresh = false)
        => cache.GetOrCreateAsync(
            $"timekeeping:{id}:details",
            TimeSpan.FromSeconds(15),
            () => http.GetFromJsonAsync<TimeKeepingDetails>($"/api/timekeeping/{id}"),
            forceRefresh);
    public Task<DashboardSummary> GetDashboardSummaryAsync(
        Guid branchId,
        DateOnly localDate,
        bool forceRefresh = false,
        CancellationToken cancellationToken = default)
        => cache.GetOrCreateAsync(
            $"branch:{branchId}:dashboard:{localDate:yyyyMMdd}",
            TimeSpan.FromSeconds(15),
            async () => await http.GetFromJsonAsync<DashboardSummary>(
                $"/api/branches/{branchId}/dashboard",
                cancellationToken) ?? throw new ApiException("Não foi possível carregar o dashboard."),
            forceRefresh,
            cancellationToken);
    public async Task ClockInAsync(ClockInRequest request)
    {
        var response = await http.PostAsJsonAsync("/api/timekeeping/clock-in", request);
        await EnsureSuccessOrThrowAsync(response);
        await InvalidateTimeKeepingAsync(request.BranchId, request.EmployeeId);
    }

    public async Task ClockOutAsync(ClockOutRequest request)
    {
        var response = await http.PostAsJsonAsync("/api/timekeeping/clock-out", request);
        await EnsureSuccessOrThrowAsync(response);
        await InvalidateTimeKeepingAsync(request.BranchId, request.EmployeeId);
    }

    public async Task BreakAsync(BreakRequest request)
    {
        var response = await http.PostAsJsonAsync("/api/timekeeping/break", request);
        await EnsureSuccessOrThrowAsync(response);
        await InvalidateTimeKeepingAsync(request.BranchId, request.EmployeeId);
    }

    public async Task<TimeKeepingAdjustmentItem> CreateTimeKeepingAdjustmentAsync(
        Guid branchId,
        Guid timeKeepingId,
        CreateTimeKeepingAdjustmentRequest request)
    {
        var response = await http.PostAsJsonAsync($"/api/timekeeping/{timeKeepingId}/adjustments", request);
        await EnsureSuccessOrThrowAsync(response);
        var item = await response.Content.ReadFromJsonAsync<TimeKeepingAdjustmentItem>()
            ?? throw new ApiException("Não foi possível criar a solicitação de correção.");
        cache.Invalidate($"branch:{branchId}:adjustments:");
        cache.Invalidate($"timekeeping:{timeKeepingId}:");
        return item;
    }

    public Task<List<TimeKeepingAdjustmentItem>> GetTimeKeepingAdjustmentsAsync(
        Guid branchId,
        string? status = null,
        bool forceRefresh = false)
        => cache.GetOrCreateAsync(
            $"branch:{branchId}:adjustments:{status}",
            TimeSpan.FromSeconds(15),
            async () =>
            {
                var url = $"/api/branches/{branchId}/timekeeping-adjustments";
                if (!string.IsNullOrWhiteSpace(status))
                    url += $"?status={Uri.EscapeDataString(status)}";
                return await http.GetFromJsonAsync<List<TimeKeepingAdjustmentItem>>(url) ?? [];
            },
            forceRefresh);

    public async Task<TimeKeepingAdjustmentItem> ReviewTimeKeepingAdjustmentAsync(
        Guid branchId,
        Guid adjustmentId,
        bool approve,
        string? reviewNote = null)
    {
        var response = await http.PutAsJsonAsync(
            $"/api/branches/{branchId}/timekeeping-adjustments/{adjustmentId}/decision",
            new ReviewTimeKeepingAdjustmentRequest(approve, reviewNote));
        await EnsureSuccessOrThrowAsync(response);
        cache.Invalidate($"branch:{branchId}:adjustments:");
        cache.Invalidate($"branch:{branchId}:history:");
        cache.Invalidate($"branch:{branchId}:dashboard");
        if (dashboardCache is not null)
            await dashboardCache.ClearAsync();
        cache.Invalidate($"branch:{branchId}:holiday-work:");
        var item = await response.Content.ReadFromJsonAsync<TimeKeepingAdjustmentItem>()
            ?? throw new ApiException("Não foi possível analisar a solicitação.");
        cache.Invalidate($"employee:{item.EmployeeId}:history:");
        cache.Invalidate($"timekeeping:{item.TimeKeepingId}:");
        return item;
    }

    // Holiday Calendar
    public Task<List<HolidayCalendarSummary>> GetHolidayCalendarsAsync(Guid branchId, bool forceRefresh = false)
        => cache.GetOrCreateAsync(
            $"branch:{branchId}:calendars",
            TimeSpan.FromMinutes(5),
            async () => await http.GetFromJsonAsync<List<HolidayCalendarSummary>>(
                $"/api/branches/{branchId}/holiday-calendars") ?? [],
            forceRefresh);

    public Task<HolidayCalendarDetails?> GetHolidayCalendarByIdAsync(
        Guid branchId,
        Guid calendarId,
        bool forceRefresh = false)
        => cache.GetOrCreateAsync(
            $"branch:{branchId}:calendars:{calendarId}",
            TimeSpan.FromMinutes(5),
            () => http.GetFromJsonAsync<HolidayCalendarDetails>(
                $"/api/branches/{branchId}/holiday-calendars/{calendarId}"),
            forceRefresh);

    public Task<List<HolidayItem>> GetHolidaysInRangeAsync(
        Guid branchId,
        DateOnly start,
        DateOnly end,
        bool forceRefresh = false)
        => cache.GetOrCreateAsync(
            $"branch:{branchId}:holidays:{start:yyyyMMdd}:{end:yyyyMMdd}",
            TimeSpan.FromMinutes(5),
            async () => await http.GetFromJsonAsync<List<HolidayItem>>(
                $"/api/branches/{branchId}/holiday-calendars/holidays/range?startDate={start:yyyy-MM-dd}&endDate={end:yyyy-MM-dd}") ?? [],
            forceRefresh);

    public Task<List<HolidayWorkRecord>> GetHolidayWorkRecordsAsync(
        Guid branchId,
        int year,
        bool forceRefresh = false)
        => cache.GetOrCreateAsync(
            $"branch:{branchId}:holiday-work:{year}",
            TimeSpan.FromSeconds(30),
            async () => await http.GetFromJsonAsync<List<HolidayWorkRecord>>(
                $"/api/branches/{branchId}/holiday-calendars/work-records/{year}") ?? [],
            forceRefresh);

    public async Task<Guid> CreateHolidayCalendarAsync(Guid branchId, CreateHolidayCalendarRequest request)
    {
        var response = await http.PostAsJsonAsync($"/api/branches/{branchId}/holiday-calendars", request);
        await EnsureSuccessOrThrowAsync(response);
        cache.Invalidate($"branch:{branchId}:calendars");
        cache.Invalidate($"branch:{branchId}:holidays:");
        cache.Invalidate($"branch:{branchId}:holiday-work:");
        return await response.Content.ReadFromJsonAsync<Guid>();
    }

    public async Task AddHolidaysAsync(Guid branchId, Guid calendarId, AddHolidaysRequest request)
    {
        var response = await http.PostAsJsonAsync($"/api/branches/{branchId}/holiday-calendars/{calendarId}/holidays", request);
        await EnsureSuccessOrThrowAsync(response);
        cache.Invalidate($"branch:{branchId}:calendars");
        cache.Invalidate($"branch:{branchId}:holidays:");
        cache.Invalidate($"branch:{branchId}:holiday-work:");
    }

    public async Task RemoveHolidayAsync(Guid branchId, Guid calendarId, DateOnly date)
    {
        var response = await http.DeleteAsync($"/api/branches/{branchId}/holiday-calendars/{calendarId}/holidays/{date:yyyy-MM-dd}");
        await EnsureSuccessOrThrowAsync(response);
        cache.Invalidate($"branch:{branchId}:calendars");
        cache.Invalidate($"branch:{branchId}:holidays:");
        cache.Invalidate($"branch:{branchId}:holiday-work:");
    }

    // Domínio Mappings
    public Task<List<DominioMappingDto>> GetDominioMappingsAsync(Guid branchId)
        => cache.GetOrCreateAsync(
            $"branch:{branchId}:dominio-mappings",
            TimeSpan.FromSeconds(30),
            async () => await http.GetFromJsonAsync<List<DominioMappingDto>>(
                $"/api/branches/{branchId}/dominio-mappings") ?? []);

    public async Task SaveDominioMappingsAsync(Guid branchId, IEnumerable<SaveDominioMappingRequest> mappings)
    {
        var response = await http.PutAsJsonAsync($"/api/branches/{branchId}/dominio-mappings", mappings);
        await EnsureSuccessOrThrowAsync(response);
        cache.Invalidate($"branch:{branchId}:dominio-mappings");
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
    public Task<List<SubscriptionPlan>> GetSubscriptionPlansAsync(CancellationToken cancellationToken = default)
        => cache.GetOrCreateAsync(
            "billing:plans",
            TimeSpan.FromMinutes(5),
            async () => await http.GetFromJsonAsync<List<SubscriptionPlan>>(
                "/api/billing/plans", cancellationToken) ?? [],
            cancellationToken: cancellationToken);

    public Task<BillingInfo?> GetBillingInfoAsync(CancellationToken cancellationToken = default)
        => cache.GetOrCreateAsync(
            "billing:subscription",
            TimeSpan.FromSeconds(10),
            async () =>
            {
                using var response = await http.GetAsync("/api/billing/subscription", cancellationToken);
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    return null;

                await EnsureSuccessOrThrowAsync(response);
                return await response.Content.ReadFromJsonAsync<BillingInfo>(cancellationToken: cancellationToken);
            },
            cancellationToken: cancellationToken);
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

    public async Task<CompanyJoinLinkResponse> GetCompanyJoinLinkAsync(Guid branchId, CancellationToken cancellationToken = default)
    {
        var response = await http.GetAsync($"/api/branches/{branchId}/company-join-link", cancellationToken);
        await EnsureSuccessOrThrowAsync(response);
        return (await response.Content.ReadFromJsonAsync<CompanyJoinLinkResponse>(cancellationToken: cancellationToken))
            ?? throw new ApiException("Não foi possível obter o link de convite.");
    }

    public async Task<CompanyJoinLinkResolutionResponse?> ResolveCompanyJoinLinkAsync(string token, CancellationToken cancellationToken = default)
    {
        var response = await http.GetAsync($"/api/company-join-links/resolve?token={Uri.EscapeDataString(token)}", cancellationToken);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<CompanyJoinLinkResolutionResponse>(cancellationToken: cancellationToken);
    }

    public async Task<(bool Success, string? Error)> RegisterFromCompanyJoinLinkAsync(
        RegisterFromCompanyJoinLinkRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await http.PostAsJsonAsync("/api/company-join-links/register", request, cancellationToken);
        if (response.IsSuccessStatusCode) return (true, null);

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        try
        {
            using var document = JsonDocument.Parse(body);
            if (document.RootElement.TryGetProperty("message", out var message) && !string.IsNullOrWhiteSpace(message.GetString()))
                return (false, message.GetString());
        }
        catch (JsonException) { }
        return (false, "Não foi possível criar a conta.");
    }

    public async Task LogoutAsync()
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/logout");
            request.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);
            using var response = await http.SendAsync(request);
        }
        catch { }
    }

    private async Task InvalidateTimeKeepingAsync(Guid branchId, Guid employeeId)
    {
        cache.Invalidate($"branch:{branchId}:history:");
        cache.Invalidate($"branch:{branchId}:dashboard");
        cache.Invalidate($"branch:{branchId}:holiday-work:");
        cache.Invalidate($"employee:{employeeId}:history:");
        cache.Invalidate("timekeeping:");
        if (dashboardCache is not null)
            await dashboardCache.ClearAsync();
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
            {
                var supportCode = string.IsNullOrWhiteSpace(problem.TraceId)
                    ? string.Empty
                    : $" (código {problem.TraceId[..Math.Min(8, problem.TraceId.Length)]})";
                throw new ApiException(problem.Title + supportCode);
            }

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
