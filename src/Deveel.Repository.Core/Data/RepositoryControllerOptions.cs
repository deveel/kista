using System;

namespace Deveel.Data {
	/// <summary>
	/// Provides configuration options for the obsolete <see cref="IRepositoryController"/>
	/// lifecycle model. Use <see cref="RepositoryLifecycleOptions"/> instead.
	/// </summary>
	[Obsolete("Use RepositoryLifecycleOptions instead")]
	public class RepositoryControllerOptions {
		/// <summary>
		/// Gets or sets whether a repository should be deleted if it already exists.
		/// Defaults to <c>true</c>.
		/// </summary>
		public bool DeleteIfExists { get; set; } = true;

		/// <summary>
		/// Gets or sets whether non-controllable repositories should be ignored
		/// without throwing. Defaults to <c>true</c>.
		/// </summary>
		public bool IgnoreNotControllable { get; set; } = true;

		/// <summary>
		/// Gets or sets whether creation should be skipped when the repository already exists.
		/// Defaults to <c>true</c>.
		/// </summary>
		public bool DontCreateExisting { get; set; } = true;
	}
}
