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

using System.Threading.Channels;

namespace Kista.Events {
	/// <summary>
	/// Configuration options for <see cref="InMemoryEntityEventPublisher{TEntity}"/>.
	/// </summary>
	public class InMemoryEventPublisherOptions {
		/// <summary>
		/// The maximum number of events the internal channel can hold before
		/// applying backpressure. Defaults to 1024.
		/// </summary>
		/// <remarks>
		/// When the channel is full, publishers wait (backpressure) instead of
		/// growing memory without bound. Set to a value appropriate for your
		/// consumer throughput.
		/// </remarks>
		public int Capacity { get; set; } = 1024;

		/// <summary>
		/// The behavior when the channel is full and a publisher attempts to
		/// write. Defaults to <see cref="BoundedChannelFullMode.Wait"/>, which
		/// applies backpressure to the publisher.
		/// </summary>
		public BoundedChannelFullMode FullMode { get; set; } = BoundedChannelFullMode.Wait;
	}
}