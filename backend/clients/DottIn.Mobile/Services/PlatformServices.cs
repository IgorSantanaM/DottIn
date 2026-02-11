using DottIn.Mobile.Services.Interfaces;

namespace DottIn.Mobile.Services;

public class SecureStorageService : ISecureStorageService
{
    public async Task<string?> GetAsync(string key)
    {
        try
        {
            return await SecureStorage.Default.GetAsync(key);
        }
        catch
        {
            return null;
        }
    }

    public async Task SetAsync(string key, string value)
    {
        await SecureStorage.Default.SetAsync(key, value);
    }

    public Task RemoveAsync(string key)
    {
        SecureStorage.Default.Remove(key);
        return Task.CompletedTask;
    }

    public Task ClearAllAsync()
    {
        SecureStorage.Default.RemoveAll();
        return Task.CompletedTask;
    }
}

public class LocationService : ILocationService
{
    public async Task<PermissionStatus> CheckPermissionAsync()
    {
        return await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
    }

    public async Task<PermissionStatus> RequestPermissionAsync()
    {
        return await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
    }

    public async Task<Location?> GetCurrentLocationAsync()
    {
        try
        {
            var status = await CheckPermissionAsync();
            
            if (status != PermissionStatus.Granted)
            {
                status = await RequestPermissionAsync();
                if (status != PermissionStatus.Granted)
                    return null;
            }

            var request = new GeolocationRequest(GeolocationAccuracy.High, TimeSpan.FromSeconds(10));
            var location = await Geolocation.Default.GetLocationAsync(request);

            return location;
        }
        catch (Exception)
        {
            return null;
        }
    }

    public double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        var location1 = new Location(lat1, lon1);
        var location2 = new Location(lat2, lon2);
        return Location.CalculateDistance(location1, location2, DistanceUnits.Kilometers) * 1000; // meters
    }
}

public class ConnectivityService : IConnectivityService
{
    public bool IsConnected => Connectivity.Current.NetworkAccess == NetworkAccess.Internet;

    public event Action<bool>? ConnectivityChanged;

    public ConnectivityService()
    {
        Connectivity.Current.ConnectivityChanged += (s, e) =>
        {
            ConnectivityChanged?.Invoke(e.NetworkAccess == NetworkAccess.Internet);
        };
    }
}
