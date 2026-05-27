using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

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

        /// <summary>
        /// Registers a singleton <see cref="IUserAccessor{TKey}"/> service
        /// that resolves the current user identifier from the HTTP request
        /// using <see cref="HttpUserAccessor{TKey}"/>.
        /// </summary>
        /// <typeparam name="TKey">
        /// The type of the user identifier key.
        /// </typeparam>
        /// <param name="services">
        /// The collection of services to register the user accessor.
        /// </param>
        /// <param name="configure">
        /// An optional delegate to configure the <see cref="HttpUserAccessorOptions"/>.
        /// </param>
        /// <returns>
        /// Returns the given collection of services for chaining calls.
        /// </returns>
        public static IServiceCollection AddHttpUserAccessor<TKey>(this IServiceCollection services, Action<HttpUserAccessorOptions>? configure = null) {
            services.AddHttpContextAccessor();
            services.TryAddSingleton<IUserAccessor<TKey>, HttpUserAccessor<TKey>>();
            services.Configure(configure ?? (_ => { }));

            return services;
        }
    }
}