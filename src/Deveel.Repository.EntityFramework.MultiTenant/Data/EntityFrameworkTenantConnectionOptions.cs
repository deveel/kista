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

namespace Deveel.Data {
	/// <summary>
	/// Provides options for configuring the connection
	/// for a multi-tenant Entity Framework Core application.
	/// </summary>
	public class EntityFrameworkTenantConnectionOptions {
		/// <summary>
		/// Gets or sets the default connection string used to
		/// connect to the database, when no tenant-specific
		/// connection string is provided.
		/// </summary>
		public string? DefaultConnectionString { get; set; }
	}
}
