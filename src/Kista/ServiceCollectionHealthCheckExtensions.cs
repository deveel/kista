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

using System.Linq;
using Kista.HealthChecks.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace Kista;

/// <summary>
/// Extension methods for registering repositories for health check tracking.
/// </summary>
internal static class ServiceCollectionHealthCheckExtensions {
    /// <summary>
    /// Ensures the repository context registry is registered.
    /// </summary>
    public static void EnsureRegistryRegistered(this IServiceCollection services) {
        if (!services.Any(d => d.ServiceType == typeof(IRepositoryContextRegistry))) {
            var registry = new RepositoryContextRegistry();
            services.AddSingleton<IRepositoryContextRegistry>(registry);
            services.AddSingleton(registry);
        }
    }
    
    /// <summary>
    /// Registers a repository for health check tracking.
    /// </summary>
    public static void RegisterRepositoryForHealthCheck(
        this IServiceCollection services,
        Type repositoryType,
        Type entityType) {
        
        services.EnsureRegistryRegistered();
        
        var registry = services
            .First(d => d.ServiceType == typeof(IRepositoryContextRegistry))
            .ImplementationInstance as RepositoryContextRegistry;
        
        if (registry == null)
            return;
        
        // Extract key type from repository type
        Type? keyType = null;
        foreach (var iface in repositoryType.GetInterfaces().Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRepository<,>))) {
            var genericArgs = iface.GetGenericArguments();
            keyType = genericArgs[1];
            break;
        }
        
        if (keyType != null)
            registry.Register(repositoryType, entityType, keyType);
    }
}
