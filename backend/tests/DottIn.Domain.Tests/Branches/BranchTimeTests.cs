using DottIn.Domain.Branches;

namespace DottIn.Domain.Tests.Branches;

public class BranchTimeTests
{
    [Fact]
    public void GetLocalDate_UsesBranchTimezoneInsteadOfUtcDate()
    {
        var utc = new DateTime(2026, 7, 24, 1, 30, 0, DateTimeKind.Utc);

        var localDate = BranchTime.GetLocalDate(utc, "America/Sao_Paulo");

        Assert.Equal(new DateOnly(2026, 7, 23), localDate);
    }

    [Fact]
    public void ToLocal_ConvertsUtcAndKeepsUnspecifiedKindForApiSerialization()
    {
        var utc = new DateTime(2026, 7, 24, 12, 0, 0, DateTimeKind.Utc);

        var local = BranchTime.ToLocal(utc, "America/Sao_Paulo");

        Assert.Equal(new DateTime(2026, 7, 24, 9, 0, 0, DateTimeKind.Unspecified), local);
    }

    [Fact]
    public void NormalizeUtc_RejectsLocalOrUnspecifiedTimestamps()
    {
        Assert.ThrowsAny<Exception>(() => BranchTime.NormalizeUtc(new DateTime(2026, 7, 24, 12, 0, 0)));
    }
}
