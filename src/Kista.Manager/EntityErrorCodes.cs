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
	/// Provides a set of error codes that can be used to identify
	/// the type of error that occurred during an operation.
	/// </summary>
	public static class EntityErrorCodes {
		/// <summary>
		/// The application domain is unknown
		/// </summary>
		public const string UnknownDomain = "Unknown";

		/// <summary>
		/// The entity to be added or updated is not valid.
		/// </summary>
		public const string NotValid = "NOT_VALID";

		/// <summary>
		/// The entity was not found in the repository.
		/// </summary>
		public const string NotFound = "NOT_FOUND";

		/// <summary>
		/// The operation is not supported by the repository.
		/// </summary>
		public const string NotSupported = "NOT_SUPPORTED";

		/// <summary>
		/// An unknown error occurred during the operation.
		/// </summary>
		public const string UnknownError = "UNKNOWN_ERROR";
	}
}
