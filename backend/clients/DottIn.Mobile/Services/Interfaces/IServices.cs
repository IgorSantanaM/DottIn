namespace DottIn.Mobile.Services.Interfaces;

public interface ILocationService
{
    Task<PermissionStatus> CheckPermissionAsync();
    Task<PermissionStatus> RequestPermissionAsync();
    Task<Location?> GetCurrentLocationAsync();
    double CalculateDistance(double lat1, double lon1, double lat2, double lon2);
}

public sealed class LocationUnavailableException(string message, Exception? innerException = null)
    : Exception(message, innerException);

public interface IConnectivityService
{
    bool IsConnected { get; }
    event Action<bool>? ConnectivityChanged;
}
