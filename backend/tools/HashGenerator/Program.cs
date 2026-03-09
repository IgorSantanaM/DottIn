var password = "123456";
var hash = BCrypt.Net.BCrypt.EnhancedHashPassword(password, 11);
Console.WriteLine("Enhanced BCrypt hash for '123456':");
Console.WriteLine(hash);
Console.WriteLine("Verification: " + BCrypt.Net.BCrypt.EnhancedVerify(password, hash));
