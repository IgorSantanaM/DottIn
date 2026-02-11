namespace DottIn.Mobile.Services.Interfaces;

public interface ISecureStorageService
{
    Task<string?> GetAsync(string key);
    Task SetAsync(string key, string value);
    Task RemoveAsync(string key);
    Task ClearAllAsync();
}

public interface ILocationService
{
    Task<PermissionStatus> CheckPermissionAsync();
    Task<PermissionStatus> RequestPermissionAsync();
    Task<Location?> GetCurrentLocationAsync();
    double CalculateDistance(double lat1, double lon1, double lat2, double lon2);
}

public interface IConnectivityService
{
    bool IsConnected { get; }
    event Action<bool>? ConnectivityChanged;
}

public interface ILocalDatabaseService
{
    Task InitializeAsync();
    Task<List<PendingClockEntry>> GetPendingEntriesAsync();
    Task AddPendingEntryAsync(PendingClockEntry entry);
    Task RemovePendingEntryAsync(int id);
    Task ClearPendingEntriesAsync();
}

public record PendingClockEntry
{
    public int Id { get; set; }
    public string Type { get; set; } = string.Empty; // ClockIn, ClockOut, Break
    public Guid BranchId { get; set; }
    public Guid EmployeeId { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public DateTime CreatedAt { get; set; }
}
