namespace DottIn.Presentation.WebApi.Endpoints.Internal
{
    public interface IEndpoint
    {
        static abstract void DefineEndpoints(WebApplication app);
    }
}
