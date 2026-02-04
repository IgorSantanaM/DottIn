namespace DottIn.Application.Exceptions
{
    public class NotFoundException : Exception
    {
        public NotFoundException() : base()
        { }

        public NotFoundException(string message) : base(message)
        { }

        public NotFoundException(string message, Exception innerException) : base(message, innerException)
        { }

        public static NotFoundException ForEntity(string entityName, object id)
        => new($"{entityName} with ID=[{id}] not found");
    }
}
