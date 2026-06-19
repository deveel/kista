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
/// Marker interface to track which drivers have health checks enabled.
/// </summary>
public interface IHealthCheckMarker {
    /// <summary>
    /// Gets the driver type.
    /// </summary>
    string DriverType { get; }
}

/// <summary>
/// Marker for Entity Framework health checks.
/// </summary>
public sealed class EntityFrameworkHealthCheckMarker : IHealthCheckMarker {
    public string DriverType => "EntityFramework";
}

/// <summary>
/// Marker for MongoDB health checks.
/// </summary>
public sealed class MongoHealthCheckMarker : IHealthCheckMarker {
    public string DriverType => "MongoDB";
}

/// <summary>
/// Marker for In-Memory health checks.
/// </summary>
public sealed class InMemoryHealthCheckMarker : IHealthCheckMarker {
    public string DriverType => "InMemory";
}

/// <summary>
/// Marker to prevent double-registration of health checks.
/// </summary>
public interface IKistaHealthChecksRegistered { }

public sealed class KistaHealthChecksRegisteredMarker : IKistaHealthChecksRegistered { }
