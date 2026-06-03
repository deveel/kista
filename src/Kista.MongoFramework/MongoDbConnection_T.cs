// Copyright 2023-2026 Antonello Provenzano
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using MongoDB.Driver;

using MongoFramework;
using MongoFramework.Infrastructure.Diagnostics;

namespace Kista {
	/// <summary>
	/// An implementation of <see cref="IMongoDbConnection{TContext}"/> that
	/// is used to create a connection to a MongoDB database.
	/// </summary>
	/// <typeparam name="TContext">
	/// The type of the context that is used to create the connection.
	/// </typeparam>
	/// <remarks>
	/// This object wraps a <see cref="IMongoDbConnection"/> and it's used
	/// to strongly type the connection to a specific context.
	/// </remarks>
	public class MongoDbConnection<TContext> : IMongoDbConnection<TContext> 
		where TContext : class, IMongoDbContext {

		private bool disposed;
		private IMongoClient? client;

		/// <summary>
		/// Creates a new instance of <see cref="MongoDbConnection{TContext}"/>
		/// that connects to a MongoDB database using the specified connection string.
		/// </summary>
		/// <param name="connectionString">The connection string used to establish 
		/// a connection to the MongoDB database.</param>
		public MongoDbConnection(string connectionString)
		{
			var settings = MongoClientSettings.FromConnectionString(connectionString);
			// Set the Linq provider to V2 to ensure compatibility with MongoFramework
			settings.LinqProvider = MongoDB.Driver.Linq.LinqProvider.V2;
			Url = MongoUrl.Create(connectionString);
			client = new MongoClient(settings);
		}

		private void ThrowIfDisposed() {
			if (disposed)
				throw new ObjectDisposedException(nameof(MongoDbConnection<TContext>));
		}

		/// <summary>
		/// Gets the MongoDB connection URL used by this connection.
		/// </summary>
		public MongoUrl Url { get; }

		/// <summary>
		/// Gets the MongoDB client instance used for database operations.
		/// </summary>
		public IMongoClient Client
		{
			get
			{
				ThrowIfDisposed();
				return client ?? throw new InvalidOperationException("MongoDB client is not initialized.");
			}
		}

		/// <inheritdoc/>
		public IDiagnosticListener DiagnosticListener { get; set; } = new NoOpDiagnosticListener();

		/// <inheritdoc/>
		public IMongoDatabase GetDatabase()
		{
			ThrowIfDisposed();

			if (client == null)
				throw new InvalidOperationException("MongoDB client is not initialized.");

			return client.GetDatabase(Url.DatabaseName);
		}

		/// <inheritdoc/>
		public void Dispose() {
			if (!disposed) {
				client = null;
				disposed = true;
			}
			GC.SuppressFinalize(this);
		}
	}
}
