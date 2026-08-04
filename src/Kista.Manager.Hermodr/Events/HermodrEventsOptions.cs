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
	/// Options controlling how <see cref="HermodrEventPublisher{TEntity}"/>
	/// maps <see cref="EntityEventData{TEntity}"/> payloads to canonical
	/// CNCF CloudEvents.
	/// </summary>
	public class HermodrEventsOptions {
		/// <summary>
		/// Gets or sets the URI scheme used to build the CloudEvent
		/// <c>source</c> attribute. The default is <c>kista</c>, producing
		/// sources of the form <c>kista://&lt;entity-type-name&gt;</c>.
		/// </summary>
		public string SourceUriScheme { get; set; } = "kista";

		/// <summary>
		/// Gets or sets an optional base URI for the CloudEvent
		/// <c>dataschema</c> attribute. When set, the schema is built as
		/// <c>&lt;DataSchemaBaseUri&gt;/&lt;event-type-name&gt;</c>.
		/// </summary>
		public Uri? DataSchemaBaseUri { get; set; }
	}
}