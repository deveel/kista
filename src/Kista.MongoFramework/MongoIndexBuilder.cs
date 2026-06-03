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
using MongoFramework.Infrastructure;
using MongoFramework.Infrastructure.Mapping;

namespace Kista {
	/// <summary>
	/// A shared utility class for building MongoDB index models from entity definitions.
	/// </summary>
	/// <typeparam name="TEntity">The entity type for which indexes are built.</typeparam>
	internal static class MongoIndexBuilder<TEntity> where TEntity : class {
		/// <summary>
		/// Builds a list of <see cref="CreateIndexModel{TEntity}"/> from the given index definition.
		/// </summary>
		/// <param name="indexDef">The index definition to build models from.</param>
		/// <returns>A list of index models ready to be created.</returns>
		internal static List<CreateIndexModel<TEntity>> BuildIndexModels(IndexDefinition indexDef) {
			var keysBuilder = new IndexKeysDefinitionBuilder<TEntity>();
			var indices = new List<CreateIndexModel<TEntity>>();

			foreach (var path in indexDef.IndexPaths) {
				var keysDef = BuildIndexKeys(keysBuilder, path);
				if (keysDef == null)
					continue;

				var options = new CreateIndexOptions {
					Unique = indexDef.IsUnique,
					Name = indexDef.IndexName
				};
				indices.Add(new CreateIndexModel<TEntity>(keysDef, options));
			}

			return indices;
		}

		/// <summary>
		/// Builds an <see cref="IndexKeysDefinition{TEntity}"/> for the given path definition.
		/// </summary>
		/// <param name="builder">The index keys definition builder.</param>
		/// <param name="path">The path definition to build keys for.</param>
		/// <returns>The keys definition, or <c>null</c> if the path type is not supported.</returns>
		internal static IndexKeysDefinition<TEntity>? BuildIndexKeys(IndexKeysDefinitionBuilder<TEntity> builder, IndexPathDefinition path) {
			var fieldDef = new StringFieldDefinition<TEntity>(path.Path);
			return path.IndexType switch {
				IndexType.Standard => path.SortOrder == IndexSortOrder.Descending
					? builder.Descending(fieldDef)
					: builder.Ascending(fieldDef),
				IndexType.Geo2dSphere => builder.Geo2DSphere(fieldDef),
				IndexType.Text => builder.Text(fieldDef),
				_ => null
			};
		}
	}
}