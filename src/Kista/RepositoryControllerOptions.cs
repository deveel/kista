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

﻿using System.Diagnostics.CodeAnalysis;

namespace Kista {
	/// <summary>
	/// Provides configuration options for the obsolete <see cref="IRepositoryController"/>
	/// lifecycle model. Use <see cref="RepositoryLifecycleOptions"/> instead.
	/// </summary>
	[Obsolete("Use RepositoryLifecycleOptions instead")]
	[ExcludeFromCodeCoverage]
	public class RepositoryControllerOptions {
		/// <summary>
		/// Gets or sets whether a repository should be deleted if it already exists.
		/// Defaults to <c>true</c>.
		/// </summary>
		public bool DeleteIfExists { get; set; } = true;

		/// <summary>
		/// Gets or sets whether non-controllable repositories should be ignored
		/// without throwing. Defaults to <c>true</c>.
		/// </summary>
		public bool IgnoreNotControllable { get; set; } = true;

		/// <summary>
		/// Gets or sets whether creation should be skipped when the repository already exists.
		/// Defaults to <c>true</c>.
		/// </summary>
		public bool DontCreateExisting { get; set; } = true;
	}
}
