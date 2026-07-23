namespace DottIn.Admin.Services;

public sealed class OperationalAccessService(HttpClient http, AdminState state)
{
    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        if (!state.IsAuthenticated || !state.HasCompletedConfiguration)
        {
            state.SetOperationalAccess(hasLinkedPlan: false);
            return;
        }

        var hasLinkedPlan = false;

        try
        {
            using var response = await http.GetAsync("/api/billing/subscription", cancellationToken);
            hasLinkedPlan = response.IsSuccessStatusCode;
        }
        catch (HttpRequestException)
        {
            // Fail closed: modules remain unavailable while eligibility cannot be confirmed.
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // Network timeout: modules remain unavailable while eligibility cannot be confirmed.
        }

        state.SetOperationalAccess(hasLinkedPlan);
    }
}
