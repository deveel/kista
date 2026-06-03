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
	/// The contract used to define an object that 
	/// has an owner.
	/// </summary>
	public interface IHaveOwner<TKey>
	{
		/// <summary>
		/// Gets the identifier of the owner of 
		/// the object.
		/// </summary>
		TKey Owner { get; }

		/// <summary>
		/// Sets the owner of the object, eventually
		/// overriding the current owner.
		/// </summary>
		/// <param name="owner">
		/// The identifier of the new owner.
		/// </param>
		void SetOwner(TKey owner);
	}
}
