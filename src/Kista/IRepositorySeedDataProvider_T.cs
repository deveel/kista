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

namespace Kista
{
    /// <summary>
    /// Provides strongly-typed seed data for a specific entity type.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity to seed.</typeparam>
    public interface IRepositorySeedDataProvider<out TEntity> : IRepositorySeedDataProvider
        where TEntity : class {
        /// <summary>
        /// Retrieves the seed data as a collection of <typeparamref name="TEntity"/> instances.
        /// </summary>
        /// <returns>An enumerable of seed data entities.</returns>
        new IEnumerable<TEntity> GetSeedData();
    }
}