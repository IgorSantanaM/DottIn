using DottIn.Domain.Branches;
using DottIn.Domain.Employees;
using DottIn.Domain.Subscriptions;
using DottIn.Domain.TimeKeepings;
using DottIn.Infra.Data.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace DottIn.WebApi.IntegrationTests.Persistence;

public sealed class RelationalModelSecurityTests
{
    [Fact]
    public void TimeKeeping_RequiresEmployeeToBelongToTheSameBranch()
    {
        using var context = CreateContext();
        var entity = context.Model.FindEntityType(typeof(TimeKeeping))!;

        var foreignKey = entity.GetForeignKeys().SingleOrDefault(fk =>
            fk.PrincipalEntityType.ClrType == typeof(Employee) &&
            fk.Properties.Select(property => property.Name)
                .SequenceEqual([nameof(TimeKeeping.BranchId), nameof(TimeKeeping.EmployeeId)]));

        Assert.NotNull(foreignKey);
    }

    [Fact]
    public void TimeKeeping_HasBranchForeignKey()
    {
        using var context = CreateContext();
        var entity = context.Model.FindEntityType(typeof(TimeKeeping))!;

        Assert.Contains(entity.GetForeignKeys(), fk =>
            fk.PrincipalEntityType.ClrType == typeof(Branch) &&
            fk.Properties.Select(property => property.Name).SequenceEqual([nameof(TimeKeeping.BranchId)]));
    }

    [Fact]
    public void EmployeeInvitation_RequiresInviterToBelongToTheSameBranch()
    {
        using var context = CreateContext();
        var entity = context.Model.FindEntityType(typeof(EmployeeInvitation))!;

        Assert.Contains(entity.GetForeignKeys(), fk =>
            fk.PrincipalEntityType.ClrType == typeof(Employee) &&
            fk.Properties.Select(property => property.Name)
                .SequenceEqual([nameof(EmployeeInvitation.BranchId), nameof(EmployeeInvitation.InvitedByEmployeeId)]));
    }

    [Fact]
    public void SecurityEnums_HaveDatabaseCheckConstraints()
    {
        using var context = CreateContext();

        AssertConstraint(context, typeof(Employee), "CK_Employees_Role");
        AssertConstraint(context, typeof(EmployeeInvitation), "CK_EmployeeInvitations_Role");
        AssertConstraint(context, typeof(StripeWebhookReceipt), "CK_StripeWebhookReceipts_Status");
        AssertConstraint(context, typeof(TimeKeeping), "CK_TimeKeepings_Source");
    }

    private static void AssertConstraint(DottInContext context, Type entityType, string constraintName)
    {
        var model = context.GetService<IDesignTimeModel>().Model;
        var entity = model.FindEntityType(entityType)!;
        Assert.Contains(entity.GetCheckConstraints(), constraint => constraint.Name == constraintName);
    }

    private static DottInContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<DottInContext>()
            .UseNpgsql("Host=localhost;Database=model_only;Username=model_only;Password=model_only")
            .Options;

        return new DottInContext(options);
    }
}
