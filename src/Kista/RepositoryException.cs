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

using System.Runtime.Serialization;

namespace Kista {
	/// <summary>
	/// An exception that is thrown during the execution
	/// of an operation on the repository
	/// </summary>
	public class RepositoryException : Exception {
		/// <summary>
		/// Constructs an empty instance of the <see cref="RepositoryException"/> class.
		/// </summary>
		public RepositoryException() {
		}

		/// <summary>
		/// Constructs an instance of the <see cref="RepositoryException"/> class
		/// </summary>
		/// <param name="message">
		/// The message that describes the error.
		/// </param>
		public RepositoryException(string? message) : base(message) {
		}

		/// <summary>
		/// Constructs an instance of the <see cref="RepositoryException"/> class
		/// </summary>
		/// <param name="message">
		/// The message that describes the error.
		/// </param>
		/// <param name="innerException">
		/// An exception that is the cause of the current exception.
		/// </param>
		public RepositoryException(string? message, Exception? innerException) : base(message, innerException) {
		}
	}
}
