using DottIn.Mobile.Services.Interfaces;
using SQLite;

namespace DottIn.Mobile.Services;

public class LocalDatabaseService : ILocalDatabaseService
{
    private SQLiteAsyncConnection? _database;
    private readonly string _dbPath;

    public LocalDatabaseService()
    {
        _dbPath = Path.Combine(FileSystem.AppDataDirectory, "dottin.db3");
    }

    public async Task InitializeAsync()
    {
        if (_database is not null)
            return;

        _database = new SQLiteAsyncConnection(_dbPath, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache);
        await _database.CreateTableAsync<PendingClockEntryEntity>();
    }

    public async Task<List<PendingClockEntry>> GetPendingEntriesAsync()
    {
        await InitializeAsync();
        var entities = await _database!.Table<PendingClockEntryEntity>().ToListAsync();
        return entities.Select(e => e.ToRecord()).ToList();
    }

    public async Task AddPendingEntryAsync(PendingClockEntry entry)
    {
        await InitializeAsync();
        var entity = PendingClockEntryEntity.FromRecord(entry);
        await _database!.InsertAsync(entity);
    }

    public async Task RemovePendingEntryAsync(int id)
    {
        await InitializeAsync();
        await _database!.DeleteAsync<PendingClockEntryEntity>(id);
    }

    public async Task ClearPendingEntriesAsync()
    {
        await InitializeAsync();
        await _database!.DeleteAllAsync<PendingClockEntryEntity>();
    }
}

[Table("PendingClockEntries")]
internal class PendingClockEntryEntity
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string BranchId { get; set; } = string.Empty;
    public string EmployeeId { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public DateTime CreatedAt { get; set; }

    public PendingClockEntry ToRecord() => new()
    {
        Id = Id,
        Type = Type,
        BranchId = Guid.Parse(BranchId),
        EmployeeId = Guid.Parse(EmployeeId),
        Latitude = Latitude,
        Longitude = Longitude,
        CreatedAt = CreatedAt
    };

    public static PendingClockEntryEntity FromRecord(PendingClockEntry record) => new()
    {
        Type = record.Type,
        BranchId = record.BranchId.ToString(),
        EmployeeId = record.EmployeeId.ToString(),
        Latitude = record.Latitude,
        Longitude = record.Longitude,
        CreatedAt = record.CreatedAt
    };
}
