using DotNet.Testcontainers.Containers;

using MongoDB.Driver;

using Testcontainers.MongoDb;

namespace Kista {
	public class MongoSingleDatabase : IAsyncLifetime, IDisposable {
		private readonly MongoDbContainer container;
		private bool disposedValue;

		~MongoSingleDatabase() {
			Dispose(disposing: false);
		}

		public MongoSingleDatabase() {
			container = new MongoDbBuilder()
				.WithUsername("")
				.WithPassword("")
				.Build();
		}

		public const string DatabaseName = "test_db";

		public string ConnectionString =>
				SetDatabase(container.GetConnectionString(), DatabaseName);

		private static string SetDatabase(string connectionString, string database) {
			var urlBuilder = new MongoUrlBuilder(connectionString);
			urlBuilder.DatabaseName = database;
			return urlBuilder.ToString();
		}

		public async ValueTask DisposeAsync() {
			if (!disposedValue) {
				await container.StopAsync();
				while (container.State != TestcontainersStates.Exited) {
					await Task.Delay(100);
				}

				await container.DisposeAsync();
				disposedValue = true;
			}
		}

		public async ValueTask InitializeAsync() {
			await container.StartAsync();
		}

		public void Dispose() {
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}

		private void Dispose(bool disposing) {
			if (!disposedValue) {
				if (disposing) {
					DisposeAsync().GetAwaiter().GetResult();
				}
				disposedValue = true;
			}
		}
	}
}
