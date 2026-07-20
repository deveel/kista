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

using System.Diagnostics.CodeAnalysis;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Kista {
	/// <summary>
	/// Extensions for the <see cref="IServiceCollection"/> to register
	/// repositories and providers.
	/// </summary>
	public static partial class ServiceCollectionExtensions {
		/// <summary>
		/// Registers a singleton <see cref="ISystemTime"/> service of the
		/// given <typeparamref name="TTime"/> type.
		/// </summary>
		/// <typeparam name="TTime">
		/// The type of the <see cref="ISystemTime"/> implementation.
		/// </typeparam>
		/// <param name="services">
		/// The <see cref="IServiceCollection"/> to add the service to.
		/// </param>
		/// <returns>
		/// Returns the <see cref="IServiceCollection"/> so that additional calls can be chained.
		/// </returns>
		public static IServiceCollection AddSystemTime<TTime>(this IServiceCollection services)
			where TTime : class, ISystemTime {
			services.TryAddSingleton<ISystemTime, TTime>();
			services.AddSingleton<TTime>();
			return services;
		}

		/// <summary>
		/// Registers a singleton instance of <see cref="ISystemTime"/> of the
		/// given <typeparamref name="TTime"/> type.
		/// </summary>
		/// <typeparam name="TTime">
		/// The type of the <see cref="ISystemTime"/> implementation.
		/// </typeparam>
		/// <param name="services">
		/// The <see cref="IServiceCollection"/> to add the service to.
		/// </param>
		/// <param name="time">
		/// The instance of <typeparamref name="TTime"/> to register.
		/// </param>
		/// <returns>
		/// Returns the <see cref="IServiceCollection"/> so that additional calls
		/// can be chained.
		/// </returns>
		public static IServiceCollection AddSystemTime<TTime>(this IServiceCollection services, TTime time)
			where TTime : class, ISystemTime {
			services.TryAddSingleton<ISystemTime>(time);
			services.AddSingleton(time);
			return services;
		}

		/// <summary>
		/// Registers the default <see cref="ISystemTime"/> service implementation
		/// </summary>
		/// <param name="services">
		/// The <see cref="IServiceCollection"/> to add the service to.
		/// </param>
		/// <returns>
		/// Returns the <see cref="IServiceCollection"/> so that additional calls can be chained.
		/// </returns>
		public static IServiceCollection AddSystemTime(this IServiceCollection services)
			=> services.AddSystemTime<SystemTime>();
    }
}