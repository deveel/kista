// Copyright 2023-2025 Antonello Provenzano
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

using System.Reflection;

using MongoDB.Driver;

using MongoFramework;

namespace Kista {
	/// <summary>
	/// Extends the <see cref="IMongoDbConnection"/> interface with
	/// helper methods to build a connection for a specific context.
	/// </summary>
	public static class MongoDbConnectionExtensions {
		/// <summary>
		/// Attempts to get the <see cref="MongoUrl"/> instance
		/// for the given connection.
		/// </summary>
		/// <param name="connection">
		/// The connection to get the URL from.
		/// </param>
		/// <remarks>
		/// This method uses reflection to get the <see cref="MongoUrl"/>
		/// instance from a default <c>Url</c> property eventually
		/// defined in the connection type.
		/// </remarks>
		/// <returns>
		/// Returns the <see cref="MongoUrl"/> instance if available,
		/// otherwise <c>null</c>.
		/// </returns>
		public static MongoUrl? GetUrl(this IMongoDbConnection connection) {
			var connectionType = connection.GetType();
			var urlProperty = connectionType.GetProperty("Url", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

			return urlProperty == null ? null : (MongoUrl?)urlProperty.GetValue(connection);
		}

		///// <summary>
		///// Constructs a new <see cref="IMongoDbConnection{TContext}"/> instance
		///// for a given context from the given connection.
		///// </summary>
		///// <typeparam name="TContext">
		///// The type of the context to build the connection for.
		///// </typeparam>
		///// <param name="connection">
		///// The connection to build the context for.
		///// </param>
		}
}
