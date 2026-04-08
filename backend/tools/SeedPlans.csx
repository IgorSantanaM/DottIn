using Npgsql;

var connectionString = "Host=localhost;Port=5432;Database=dottindb;Username=postgres;Password=postgres";
using var conn = new NpgsqlConnection(connectionString);
conn.Open();

// Check if plans already exist
using (var checkCmd = new NpgsqlCommand("SELECT COUNT(*) FROM \"SubscriptionPlans\"", conn))
{
    var count = (long)checkCmd.ExecuteScalar();
    Console.WriteLine($"Existing plans: {count}");
    if (count > 0) 
    {
        Console.WriteLine("Plans already exist, skipping seed.");
        return;
    }
}

var plans = new[]
{
    ("Free", (string)null, 5, 1, 0m),
    ("Starter", "price_starter_test", 15, 3, 49.90m),
    ("Professional", "price_professional_test", 50, 10, 149.90m),
    ("Enterprise", "price_enterprise_test", -1, -1, 299.90m)
};

foreach (var (name, priceId, maxEmp, maxBranch, price) in plans)
{
    using var cmd = new NpgsqlCommand(@"
        INSERT INTO ""SubscriptionPlans"" (""Id"", ""Name"", ""StripePriceId"", ""MaxEmployees"", ""MaxBranches"", ""MonthlyPriceBRL"", ""IsActive"", ""CreatedAt"")
        VALUES (@id, @name, @priceId, @maxEmp, @maxBranch, @price, true, @now)", conn);
    
    cmd.Parameters.AddWithValue("id", Guid.NewGuid());
    cmd.Parameters.AddWithValue("name", name);
    cmd.Parameters.AddWithValue("priceId", (object)priceId ?? DBNull.Value);
    cmd.Parameters.AddWithValue("maxEmp", maxEmp);
    cmd.Parameters.AddWithValue("maxBranch", maxBranch);
    cmd.Parameters.AddWithValue("price", price);
    cmd.Parameters.AddWithValue("now", DateTime.UtcNow);
    
    cmd.ExecuteNonQuery();
    Console.WriteLine($"Inserted plan: {name}");
}
Console.WriteLine("Done!");
