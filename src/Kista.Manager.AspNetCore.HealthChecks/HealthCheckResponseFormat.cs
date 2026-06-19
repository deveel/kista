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

namespace Kista.HealthChecks;

/// <summary>
/// Response format for health check endpoints.
/// </summary>
public enum HealthCheckResponseFormat {
    /// <summary>
    /// Plain text response.
    /// </summary>
    Text,
    
    /// <summary>
    /// JSON response.
    /// </summary>
    Json
}
