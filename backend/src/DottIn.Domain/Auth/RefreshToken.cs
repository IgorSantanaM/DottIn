using DottIn.Domain.Core.Models;

namespace DottIn.Domain.Auth
{
    public class RefreshToken : Entity<Guid>
    {
        public Guid EmployeeId { get; private set; }
        public Guid BranchId { get; private set; }
        public string Token { get; private set; }
        public DateTime ExpiresAt { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime? RevokedAt { get; private set; }
        public bool IsRevoked => RevokedAt.HasValue;
        public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
        public bool IsActive => !IsRevoked && !IsExpired;

        private RefreshToken() { }

        public RefreshToken(Guid employeeId, Guid branchId, int expirationDays = 30)
        {
            Id = Guid.NewGuid();
            EmployeeId = employeeId;
            BranchId = branchId;
            Token = GenerateToken();
            ExpiresAt = DateTime.UtcNow.AddDays(expirationDays);
            CreatedAt = DateTime.UtcNow;
        }

        public void Revoke()
        {
            RevokedAt = DateTime.UtcNow;
        }

        private static string GenerateToken()
        {
            var bytes = new byte[64];
            using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes);
        }
    }
}
