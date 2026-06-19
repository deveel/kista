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

namespace Kista.HealthChecks.Internal;

/// <summary>
/// Registry to track repositories registered via AddRepositoryContext().
/// </summary>
public interface IRepositoryContextRegistry {
    /// <summary>
    /// Gets the list of registered repositories.
    /// </summary>
    IReadOnlyList<RepositoryRegistration> RegisteredRepositories { get; }
}

/// <summary>
/// Information about a registered repository.
/// </summary>
public sealed class RepositoryRegistration {
    public Type RepositoryType { get; init; } = null!;
    public Type EntityType { get; init; } = null!;
    public Type KeyType { get; init; } = null!;
}

/// <summary>
/// Implementation of the repository context registry.
/// </summary>
public sealed class RepositoryContextRegistry : IRepositoryContextRegistry {
    private readonly List<RepositoryRegistration> _registrations = new();
    
    public IReadOnlyList<RepositoryRegistration> RegisteredRepositories => _registrations.AsReadOnly();
    
    /// <summary>
    /// Registers a repository type.
    /// </summary>
    public void Register(Type repositoryType, Type entityType, Type keyType) {
        _registrations.Add(new RepositoryRegistration {
            RepositoryType = repositoryType,
            EntityType = entityType,
            KeyType = keyType
        });
    }
}
