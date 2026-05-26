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

namespace Kista {
using Deveel;
	/// <summary>
	/// A service that creates instances of <see cref="IOperationError"/>
	/// that can be used to report errors in an operation
	/// for a specific entity.
	/// </summary>
	/// <typeparam name="TEntity">
	/// The type of the entity for which errors are created.
	/// </typeparam>
	public interface IOperationErrorFactory<TEntity> : IOperationErrorFactory where TEntity : class {
	}
}
