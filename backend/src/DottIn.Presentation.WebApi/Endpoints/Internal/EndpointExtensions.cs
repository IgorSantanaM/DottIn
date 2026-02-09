using System.Reflection;

namespace DottIn.Presentation.WebApi.Endpoints.Internal
{
    public static class EndpointExtensions
    {
        public static void UseEndpoints<TMarker>(this IApplicationBuilder app)
            => UseEndpoints(app, typeof(TMarker));

        private static void UseEndpoints(IApplicationBuilder app, Type type)
        {
            IEnumerable<TypeInfo> endpointTypes = GetEndpointsTypesFromAssemblyContaining(type);

            foreach (var endpointType in endpointTypes)
                endpointType.GetMethod(nameof(IEndpoint.DefineEndpoints))?.Invoke(null, [app]);
        }

        private static IEnumerable<TypeInfo> GetEndpointsTypesFromAssemblyContaining(Type typeMarker)
            => typeMarker.Assembly.DefinedTypes.Where(x => !x.IsAbstract && !x.IsInterface && typeof(IEndpoint).IsAssignableFrom(x));
    }
}
