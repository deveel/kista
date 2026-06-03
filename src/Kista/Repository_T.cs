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

namespace Kista {
	/// <summary>
	/// An abstract repository base class that hides the underlying
	/// <see cref="IQueryable{T}"/> data access hatch from consumer code, with
	/// the key type defaulted to <see cref="object"/>.
	/// </summary>
	/// <typeparam name="TEntity">
	/// The type of the entity managed by the repository.
	/// </typeparam>
	/// <seealso cref="Repository{TEntity,TKey}"/>
	public abstract class Repository<TEntity> : Repository<TEntity, object>
		where TEntity : class {
	}
}
