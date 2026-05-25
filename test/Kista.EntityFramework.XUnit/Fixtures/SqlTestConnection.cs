using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore.Infrastructure;
namespace Kista {
	public class SqlTestConnection : IDisposable {
		public SqlTestConnection() : this("deveel-test") {
		}

		protected SqlTestConnection(string databaseName)
        {
			Connection = new SqliteConnection($"Data Source={databaseName};Mode=Memory;Cache=Shared");
			if (Connection.State != System.Data.ConnectionState.Open)
				Connection.Open();

			// SpatiaLite requires the SQLite extension mechanism to be enabled and the
			// mod_spatialite native library to be installed.  Both requirements may not
			// be met on every developer machine (e.g. macOS ships a system SQLite that
			// does not export sqlite3_enable_load_extension).  We therefore attempt the
			// load and gracefully fall back – tests that need spatial functions can check
			// SpatialiteAvailable and skip themselves accordingly.
			try {
				Connection.EnableExtensions();
				SpatialiteLoader.Load(Connection);
				SpatialiteAvailable = true;
			} catch (Exception) {
				// SpatiaLite is not available on this platform – non-spatial tests will
				// still run; only tests that rely on spatial SQL functions must be skipped.
				SpatialiteAvailable = false;
			}
		}

		public SqliteConnection Connection { get; }

		/// <summary>
		/// Indicates whether the SpatiaLite extension was successfully loaded for this
		/// connection.  When <c>false</c>, spatial SQL functions are unavailable and any
		/// test that exercises them should be skipped.
		/// </summary>
		public bool SpatialiteAvailable { get; }

		public void Dispose() {
			if (Connection.State != System.Data.ConnectionState.Closed)
				Connection?.Close();
            
			Connection?.Dispose();
		}
	}
}
