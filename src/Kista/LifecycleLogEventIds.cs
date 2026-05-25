namespace Kista {
	/// <summary>
/// Defines the event IDs used by the repository lifecycle logging system.
/// </summary>
static class LifecycleLogEventIds {
		/// <summary>
		/// Event ID for resolving a lifecycle handler.
		/// </summary>
		public const int ResolvingHandler = 20000;
		/// <summary>
		/// Event ID for a successfully resolved lifecycle handler.
		/// </summary>
		public const int HandlerResolved = 20001;
		/// <summary>
		/// Event ID for falling back to a controllable repository.
		/// </summary>
		public const int FallingBack = 20002;
		/// <summary>
		/// Event ID for when no lifecycle handler is found.
		/// </summary>
		public const int NoHandlerFound = 20003;
		/// <summary>
		/// Event ID for deleting an existing repository.
		/// </summary>
		public const int DeletingExisting = 20004;
		/// <summary>
		/// Event ID for skipping an existing repository.
		/// </summary>
		public const int SkippingExisting = 20005;
		/// <summary>
		/// Event ID for creating a repository.
		/// </summary>
		public const int Creating = 20006;
		/// <summary>
		/// Event ID for a successfully created repository.
		/// </summary>
		public const int Created = 20007;
		/// <summary>
		/// Event ID for skipping deletion when repository does not exist.
		/// </summary>
		public const int NotExistsSkipping = 20008;
		/// <summary>
		/// Event ID for dropping a repository.
		/// </summary>
		public const int Dropping = 20009;
		/// <summary>
		/// Event ID for a successfully dropped repository.
		/// </summary>
		public const int Dropped = 20010;
		/// <summary>
		/// Event ID for skipping seed when repository already exists.
		/// </summary>
		public const int SkippingSeed = 20011;
		/// <summary>
		/// Event ID for seeding a repository.
		/// </summary>
		public const int Seeding = 20012;
		/// <summary>
		/// Event ID for a successfully seeded repository.
		/// </summary>
		public const int SeedCompleted = 20013;
		/// <summary>
		/// Event ID for when no seed data is available.
		/// </summary>
		public const int NoSeedData = 20014;
		/// <summary>
		/// Event ID for a not supported error during lifecycle operations.
		/// </summary>
		public const int NotSupportedError = 20015;
		/// <summary>
		/// Event ID for a repository-specific error.
		/// </summary>
		public const int RepositoryError = 20016;
		/// <summary>
		/// Event ID for a general error during lifecycle operations.
		/// </summary>
		public const int GeneralError = 20017;
	}
}
