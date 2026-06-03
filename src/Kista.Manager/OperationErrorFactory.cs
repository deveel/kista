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

using System.ComponentModel.DataAnnotations;
using Deveel;

namespace Kista {
	/// <summary>
	/// A default implementation of <see cref="IOperationErrorFactory"/>
	/// providing methods to create instances of <see cref="IOperationError"/>.
	/// </summary>
	public class OperationErrorFactory : IOperationErrorFactory {
		/// <summary>
		/// Normalizes the given error code to a standard format.
		/// </summary>
		/// <param name="errorCode">
		/// The error code to normalize.
		/// </param>
		/// <param name="domain">
		/// The domain where the error belongs to.
		/// </param>
		/// <remarks>
		/// The aim of this method is to provide a way to map
		/// a given error code to a normalized format, so that
		/// multiple factories can be registered for the same
		/// application.
		/// </remarks>
		/// <returns>
		/// Returns an error code normalized to a standard format.
		/// </returns>
		protected virtual string ResolveErrorCode(string errorCode, string domain) {
			return errorCode;
		}

		/// <summary>
		/// Gets the error message for the given error code.
		/// </summary>
		/// <param name="errorCode">
		/// The error code for which to get the message.
		/// </param>
		/// <returns>
		/// Returns a string containing the error message for the given
		/// code or <c>null</c> if no message is available.
		/// </returns>
		protected virtual string? GetErrorMessage(string errorCode) {
			return null;
		}

		/// <inheritdoc/>
		public virtual IOperationError CreateError(string errorCode, string domain, string? message = null)
			=> new OperationError(ResolveErrorCode(errorCode, domain), domain, message == null ? GetErrorMessage(ResolveErrorCode(errorCode, domain)) : message);

		/// <inheritdoc/>
		public virtual IOperationError CreateError(Exception exception) {
			string? errorCode = null, errorDomain = null, errorMessage = null;
			if (exception is OperationException opError) {
				errorCode = opError.ErrorCode;
				errorDomain = opError.ErrorDomain;
				errorMessage = opError.Message;
			} else {
				errorDomain = EntityErrorCodes.UnknownDomain;
				errorCode = EntityErrorCodes.UnknownError;
				errorMessage = exception.Message;
			}

			return new OperationError(errorCode, errorDomain, errorMessage);
		}

		/// <inheritdoc/>
		public virtual IValidationError CreateValidationError(string errorCode, string domain, IReadOnlyList<ValidationResult> validationResults) {
			return new OperationValidationError(ResolveErrorCode(errorCode, domain), domain, validationResults);
		}
	}
}
