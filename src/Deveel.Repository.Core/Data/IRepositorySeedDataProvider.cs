using System;

namespace Deveel.Data {
	/// <summary>
	/// Provides seed data for repository initialization, without a specific entity type.
	/// </summary>
	public interface IRepositorySeedDataProvider {
		/// <summary>
		/// Retrieves the seed data as a collection of untyped objects.
		/// </summary>
		/// <returns>An enumerable of seed data objects.</returns>
		IEnumerable<object> GetSeedData();
	}
}
