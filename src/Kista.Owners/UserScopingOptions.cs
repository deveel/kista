namespace Kista
{
	/// <summary>
	/// Configuration options for the <see cref="UserScopedRepositoryDecorator{TEntity, TKey, TUserKey}"/>.
	/// </summary>
	public class UserScopingOptions
	{
		/// <summary>
		/// Gets or sets whether to throw an <see cref="System.InvalidOperationException"/>
		/// when no user context is available. Defaults to <c>true</c>.
		/// </summary>
		/// <remarks>
		/// When <c>false</c>, operations return empty results or <c>null</c> instead of throwing.
		/// </remarks>
		public bool ThrowWhenUserNotSet { get; set; }

		/// <summary>
		/// Gets or sets the name of the owner property to use, overriding automatic discovery.
		/// </summary>
		/// <remarks>
		/// When <c>null</c> (default), the decorator discovers the owner property by scanning
		/// for the <see cref="DataOwnerAttribute"/> attribute, then falling back to a property
		/// named <c>"Owner"</c>.
		/// </remarks>
		public string? OwnerPropertyName { get; set; }
	}
}
