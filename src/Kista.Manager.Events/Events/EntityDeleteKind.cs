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

namespace Kista.Events {
	/// <summary>
	/// Identifies whether a delete operation was a soft delete (the
	/// entity was flagged as deleted) or a hard delete (the entity was
	/// physically removed from the repository).
	/// </summary>
	/// <remarks>
	/// This discriminator is carried by <see cref="EntityDeletedData{TEntity}"/>
	/// so subscribers can distinguish between the two delete semantics
	/// without inspecting the entity's <see cref="ISoftDeletable"/> interface.
	/// </remarks>
	public enum EntityDeleteKind {
		/// <summary>
		/// The entity was logically deleted (flagged as deleted via
		/// <see cref="ISoftDeletable"/>) and remains in the repository.
		/// </summary>
		Soft,

		/// <summary>
		/// The entity was physically removed from the repository.
		/// </summary>
		Hard
	}
}