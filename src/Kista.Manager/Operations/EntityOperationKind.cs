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
	/// Identifies the kind of write operation being performed on an
	/// entity through the <see cref="EntityManager{TEntity, TKey}"/>
	/// operation pipeline.
	/// </summary>
	public enum EntityOperationKind {
		/// <summary>
		/// The entity is being created (added) in the repository.
		/// </summary>
		Create,

		/// <summary>
		/// The entity is being updated in the repository.
		/// </summary>
		Update,

		/// <summary>
		/// The entity is being removed (soft-deleted or hard-deleted)
		/// from the repository.
		/// </summary>
		Remove,

		/// <summary>
		/// The entity is being restored after a previous soft-delete.
		/// </summary>
		Restore,

		/// <summary>
		/// The entity is being permanently removed (hard-deleted) from
		/// the repository, bypassing any soft-delete behavior.
		/// </summary>
		HardDelete
	}
}