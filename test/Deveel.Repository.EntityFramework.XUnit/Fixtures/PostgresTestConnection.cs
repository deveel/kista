using Testcontainers.PostgreSql;

namespace Deveel.Data {
	public class PostgresTestConnection : IAsyncLifetime, IDisposable {
		private readonly PostgreSqlContainer container;
		private bool disposedValue;

		public PostgresTestConnection() {
			container = new PostgreSqlBuilder()
				.WithImage("postgis/postgis:15-3.3")
				.WithUsername("test")
				.WithPassword("test")
				.WithDatabase("testdb")
				.Build();
		}

		public string ConnectionString => container.GetConnectionString();

		public async ValueTask InitializeAsync() {
			await container.StartAsync();
		}

		public async ValueTask DisposeAsync() {
			if (!disposedValue) {
				await container.DisposeAsync();
				disposedValue = true;
			}
		}

		public void Dispose() {
			DisposeAsync().GetAwaiter().GetResult();
		}
	}
}
