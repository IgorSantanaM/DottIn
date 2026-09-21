using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DottIn.Mobile.Services.Interfaces;
using Refit;

namespace DottIn.Mobile.Tests;

public class ApiContractTests
{
    [Fact]
    public async Task Dominio_sends_all_desktop_export_parameters()
    {
        string? requested = null;
        using var client = Client(request => { requested = request.RequestUri!.Query; return new(HttpStatusCode.OK) { Content = new StringContent("file") }; });
        var api = RestService.For<IExportApi>(client);
        using var result = await api.ExportDominioAsync(Guid.NewGuid(), "2026-09", "15", "1", "150", "250", "11");
        foreach (var expected in new[] { "month=2026-09", "companyCode=15", "normalRubricCode=1", "nocturnalRubricCode=150", "holidayRubricCode=250", "processType=11" })
            Assert.Contains(expected, requested);
    }

    [Fact]
    public async Task Mapping_contract_includes_employee_CPF()
    {
        using var client = Client(_ => new(HttpStatusCode.OK) { Content = JsonContent.Create(new[] { new { employeeId = Guid.NewGuid(), employeeName = "Test", employeeDocument = "12345678901", dominioCode = "0000000001" } }) });
        var result = Assert.Single(await RestService.For<IExportApi>(client).GetDominioMappingsAsync(Guid.NewGuid()));
        Assert.Equal("12345678901", result.EmployeeDocument);
        Assert.Equal("0000000001", result.DominioCode);
    }

    [Fact]
    public async Task Csv_uses_exact_custom_period()
    {
        string? query = null;
        using var client = Client(request => { query = Uri.UnescapeDataString(request.RequestUri!.Query); return new(HttpStatusCode.OK); });
        using var response = await RestService.For<IExportApi>(client).ExportCsvAsync(Guid.NewGuid(), new(2026, 8, 25), new(2026, 9, 12));
        Assert.Contains("startDate=", query);
        Assert.Contains("endDate=", query);
        // Explicit yyyy-MM-dd formatting keeps requests independent of device culture.
        Assert.Contains("2026-08-25", query);
        Assert.Contains("2026-09-12", query);
    }

    [Fact]
    public async Task Branch_history_requests_a_bounded_page()
    {
        Uri? requested = null;
        using var client = Client(request =>
        {
            requested = request.RequestUri;
            return new(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new { items = Array.Empty<object>(), totalPages = 3, totalCount = 51 })
            };
        });

        var branchId = Guid.NewGuid();
        var result = await RestService.For<ITimeKeepingApi>(client).GetPagedBranchHistoryAsync(
            branchId, new(2026, 9, 1), new(2026, 9, 30), 2, 25);

        Assert.Equal(51, result.TotalCount);
        Assert.Contains($"/api/timekeeping/branch/{branchId}/history/paged", requested!.AbsolutePath);
        Assert.Contains("pageNumber=2", requested.Query);
        Assert.Contains("pageSize=25", requested.Query);
        Assert.Contains("2026-09-01", Uri.UnescapeDataString(requested.Query));
        Assert.Contains("2026-09-30", Uri.UnescapeDataString(requested.Query));
    }

    [Fact]
    public void Company_address_number_is_numeric()
    {
        var address = new BranchAddress("Street", 12, "City", "MT", "78000000", null);
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(address, JsonSerializerOptions.Web));
        Assert.Equal(JsonValueKind.Number, json.RootElement.GetProperty("number").ValueKind);
    }

    internal static HttpClient Client(Func<HttpRequestMessage, HttpResponseMessage> handle)
        => new(new Handler(handle)) { BaseAddress = new Uri("https://test.invalid") };
    private sealed class Handler(Func<HttpRequestMessage, HttpResponseMessage> handle) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = handle(request);
            response.RequestMessage = request;
            return Task.FromResult(response);
        }
    }
}
