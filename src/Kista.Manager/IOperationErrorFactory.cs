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

using System.ComponentModel.DataAnnotations;
using Deveel;

namespace Kista {
	/// <summary>
	/// A service that creates instances of <see cref="IOperationError"/>
	/// that can be used to report errors in an operation.
	/// </summary>
	public interface IOperationErrorFactory {
        /// <summary>
        /// Creates an instance of <see cref="IOperationError"/>
        /// with the given error code and message.
        /// </summary>
        /// <param name="errorCode">
        /// The code that identifies the class of the error.
        /// </param>
        /// <param name="domain">
        /// The application domain that the error belongs to.
        /// </param>
        /// <param name="message">
        /// A message that describes the error.
        /// </param>
        /// <returns>
        /// Returns an instance of <see cref="IOperationError"/>
        /// with the given error code and message.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when the given <paramref name="errorCode"/> is <c>null</c>
        /// or empty.
        /// </exception>
        IOperationError CreateError(string errorCode, string domain, string? message = null);

		/// <summary>
		/// Creates an instance of <see cref="IValidationError"/>
		/// with the given error code and validation results.
		/// </summary>
		/// <param name="errorCode">
		/// The code that identifies the class of the error.
		/// </param>
		/// <param name="domain">
		/// The application domain that the error belongs to.
		/// </param>
		/// <param name="validationResults">
		/// The list of validation results that describe the error.
		/// </param>
		/// <returns>
		/// Returns an instance of <see cref="IValidationError"/>
		/// that represents a failed validation of an entity.
		/// </returns>
		IValidationError CreateValidationError(string errorCode, string domain, IReadOnlyList<ValidationResult> validationResults);

		/// <summary>
		/// Creates an instance of <see cref="IOperationError"/>
		/// from the given <paramref name="exception"/>.
		/// </summary>
		/// <param name="exception">
		/// The exception that caused the error.
		/// </param>
		/// <returns>
		/// Returns an instance of <see cref="IOperationError"/>
		/// with the given <paramref name="exception"/>.
		/// </returns>
		IOperationError CreateError(Exception exception);
	}
}
