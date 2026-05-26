namespace Kista {
	/// <summary>
	/// Defines the strategy used to seed data into a repository during
	/// the lifecycle initialization phase.
	/// </summary>
	public enum SeedStrategy {
		/// <summary>
		/// No seeding is performed.
		/// </summary>
		Never,

		/// <summary>
		/// Seeding is always performed, regardless of whether the repository exists.
		/// </summary>
		Always,

		/// <summary>
		/// Seeding is performed only if the repository does not already exist.
		/// </summary>
		IfMissing,

		/// <summary>
		/// The seeding strategy is determined by the active environment profile
		/// (see <see cref="IRepositoryLifecycleProfile"/>).
		/// </summary>
		ByEnvironment
	}
}
