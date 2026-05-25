using Finbuckle.MultiTenant;
#if NET7_0_OR_GREATER
using Finbuckle.MultiTenant.Abstractions;
#endif

using MongoFramework;

namespace Kista
{
	/// <summary>
	/// Provides an implementation of the <see cref="IMongoDbContext"/> 
	/// for multi-tenant applications, that intercepts the current
	/// tenant context.
	/// </summary>
	public class MongoDbMultiTenantContext : MongoDbTenantContext
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="MongoDbMultiTenantContext"/> 
		/// class with the specified database connection and tenant context accessor.
		/// </summary>
		/// <remarks>
		/// This constructor sets up the context for a multi-tenant MongoDB environment 
		/// by associating the connection with the tenant's unique identifier.
		/// Differently from the base class, this context is specifically designed
		/// to work with multi-tenant contexts through dependency injection, instead
		/// of requiring a tenant identifier to be passed directly.
		/// Note that implementations of the <paramref name="connection"/> parameter
		/// could also segregate the tenant scope by using a tenant-specific connection string,
		/// and therefore this context does not require a tenant identifier to be passed explicitly.
		/// </remarks>
		/// <param name="connection">The MongoDB connection used to interact with the database.</param>
		/// <param name="multiTenantContextAccessor">The service providing the current tenant context, 
		/// which includes tenant-specific information.</param>
		public MongoDbMultiTenantContext(IMongoDbConnection connection, IMultiTenantContextAccessor multiTenantContextAccessor) 
			: base(connection, multiTenantContextAccessor?.MultiTenantContext?.TenantInfo?.Id)
		{
		}
	}
}
