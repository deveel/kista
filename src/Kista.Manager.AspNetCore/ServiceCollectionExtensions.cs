using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Kista
{
	/// <summary>
	/// Provides extension methods for the <see cref="IServiceCollection"/> interface
	/// to register services for handling HTTP request cancellation.
	/// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers a singleton instance of the <see cref="HttpRequestCancellationSource"/> 
        /// in the collection of services.
        /// </summary>
        /// <param name="services">
        /// The collection of services to register the source.
        /// </param>
        /// <remarks>
        /// This method also tries to register the <see cref="IHttpContextAccessor"/>
        /// into the collection of services, if not already registered.
        /// </remarks>
        /// <returns>
        /// Returns the given collection of services for chaining calls.
        /// </returns>
        public static IServiceCollection AddHttpRequestTokenSource(this IServiceCollection services) {
            services.AddHttpContextAccessor();
            services.AddOperationTokenSource<HttpRequestCancellationSource>(ServiceLifetime.Singleton);

            return services;
        }
    }
}