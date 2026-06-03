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

using Microsoft.AspNetCore.Http;

namespace Kista {
	/// <summary>
	/// An implementation of <see cref="IOperationCancellationSource"/> that
	/// uses the <see cref="HttpContext.RequestAborted"/> token.
	/// </summary>
	/// <remarks>
	/// This implementation requires the <see cref="IHttpContextAccessor"/> to
	/// be registered in the context of the application.
	/// </remarks>
	public sealed class HttpRequestCancellationSource : IOperationCancellationSource {
		private readonly IHttpContextAccessor httpContextAccessor;

		/// <summary>
		/// Constructs the cancellation source using the given <see cref="IHttpContextAccessor"/>.
		/// </summary>
		/// <param name="httpContextAccessor">
		/// The accessor to the current HTTP context.
		/// </param>
		public HttpRequestCancellationSource(IHttpContextAccessor httpContextAccessor) {
			this.httpContextAccessor = httpContextAccessor;
		}

		/// <summary>
		/// Gets the cancellation token from the current HTTP context.
		/// </summary>
		public CancellationToken Token => httpContextAccessor.HttpContext?.RequestAborted ?? CancellationToken.None;
	}
}
