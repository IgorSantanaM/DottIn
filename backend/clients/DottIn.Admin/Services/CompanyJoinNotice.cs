namespace DottIn.Admin.Services;

public sealed class CompanyJoinNotice
{
    public const string Message = "Você já faz parte desta empresa!";
    private Guid _employeeId;
    private DateTime _expiresAt;

    public void Queue(Guid employeeId)
    {
        _employeeId = employeeId;
        _expiresAt = DateTime.UtcNow.AddMinutes(1);
    }

    public string? Consume(Guid employeeId)
    {
        var show = employeeId != Guid.Empty && employeeId == _employeeId && DateTime.UtcNow < _expiresAt;
        _employeeId = Guid.Empty;
        return show ? Message : null;
    }
}
