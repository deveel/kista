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
	/// A helper class that provides common disposable patterns to reduce code duplication.
	/// </summary>
	public static class DisposableHelper {
		/// <summary>
		/// Throws an <see cref="ObjectDisposedException"/> if the object has been disposed.
		/// </summary>
		/// <param name="disposed">The disposed flag.</param>
		/// <param name="typeName">The name of the type to include in the exception.</param>
		/// <exception cref="ObjectDisposedException">
		/// Thrown when <paramref name="disposed"/> is <c>true</c>.
		/// </exception>
		public static void ThrowIfDisposed(bool disposed, string typeName) {
			if (disposed)
				throw new ObjectDisposedException(typeName);
		}
	}
}