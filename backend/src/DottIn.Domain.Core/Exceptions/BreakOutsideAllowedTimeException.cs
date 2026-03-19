namespace DottIn.Domain.Core.Exceptions
{
    public class BreakOutsideAllowedTimeException : DomainException
    {
        public BreakOutsideAllowedTimeException(TimeOnly intervalStart, TimeOnly intervalEnd)
            : base($"Operação de intervalo não permitida fora do horário autorizado ({intervalStart:HH:mm} - {intervalEnd:HH:mm}).")
        { }
    }
}
