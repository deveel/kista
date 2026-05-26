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

using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;

namespace Kista {
	/// <summary>
	/// An implementation of <see cref="ITenantInfo"/> that
	/// provides the connection string for an Entity Framework Core tenant.
	/// </summary>
	public class EntityFrameworkTenantInfo : ITenantInfo {
		/// <summary>
		/// Gets or sets the connection string for the Entity Framework Core tenant.
		/// </summary>
		public string? ConnectionString { get; set; }

#if NET8_0 || NET9_0
		string? ITenantInfo.Id {
			get => Id ?? "";
			set => Id = value ?? "";
		}

		string? ITenantInfo.Identifier {
			get => Identifier ?? "";
			set => Identifier = value ?? "";
		}
#endif
		/// <summary>
		/// Gets or sets the unique identifier of the tenant.
		/// </summary>
		public string Id { get; set; }

		/// <summary>
		/// Gets or sets the unique identifier used to identify the tenant.
		/// </summary>
		public string Identifier { get; set; }

		/// <summary>
		/// Gets or sets the display name of the tenant.
		/// </summary>
		public string? Name { get; set; }
	}
}
