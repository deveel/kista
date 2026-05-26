using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using MongoFramework;

namespace Kista
{
	/// <summary>
	/// Provides extension methods for configuring tenant-specific MongoDB connections 
	/// in a <see cref="MongoConnectionBuilder"/>.
	/// </summary>
	/// <remarks>
	/// This class contains methods that extend the functionality of <see cref="MongoConnectionBuilder"/> 
	/// to support tenant-specific connection configurations. 
	/// It allows the registration of services necessary for handling connections 
	/// to MongoDB databases with tenant-specific settings.
	/// </remarks>
	public static class MongoConnectionBuilderExtensions
	{
		/// <summary>
		/// Configures the <see cref="MongoConnectionBuilder"/> to use 
		/// tenant-specific MongoDB connections.
		/// </summary>
		/// <remarks>
		/// This method sets up the necessary services to support tenant-specific 
		/// connections in a MongoDB context. 
		/// It registers the required options and services to enable the use of 
		/// tenant-specific connection strings.
		/// </remarks>
		/// <param name="builder">The <see cref="MongoConnectionBuilder"/> to configure.</param>
		/// <param name="defaultConnection">The default connection string to use if no tenant-specific 
		/// connection is provided. Can be <see langword="null"/>.</param>
		/// <returns>The configured <see cref="MongoConnectionBuilder"/> instance.</returns>
		public static MongoConnectionBuilder UseTenantConnection(this MongoConnectionBuilder builder, string? defaultConnection = null)
		{
			builder.Services.AddOptions<MongoTenantConnectionOptions>()
				.Configure(options => options.DefaultConnectionString = defaultConnection);

			var connectionType = typeof(IMongoDbConnection<>).MakeGenericType(builder.ContextType);
			builder.Services.TryAdd(ServiceDescriptor.Describe(connectionType, sp =>
			{
				var implementationType = typeof(MongoDbTenantConnection<>).MakeGenericType(builder.ContextType);
				return ActivatorUtilities.CreateInstance(sp, implementationType);
			}, builder.Lifetime));

			builder.Services.TryAdd(ServiceDescriptor.Describe(typeof(IMongoDbConnection), sp =>
			{
				var connection = sp.GetRequiredService(connectionType);
				return (IMongoDbConnection)connection;
			}, builder.Lifetime));

			return builder;
		}

	}
}
