// Copyright 2023-2025 Antonello Provenzano
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

using System;

namespace Deveel.Data {
/// <summary>
	/// Defines the event IDs used by the Entity Framework repository logging system.
	/// </summary>
    static class LogEventIds {
        /// <summary>
        /// Event ID for creating an entity.
        /// </summary>
        public const int CreatingEntity = 10000;
        /// <summary>
        /// Event ID for updating an entity.
        /// </summary>
        public const int UpdatingEntity = 10001;
        /// <summary>
        /// Event ID for deleting an entity.
        /// </summary>
        public const int DeletingEntity = 10002;
        /// <summary>
        /// Event ID for finding an entity by ID.
        /// </summary>
        public const int FindingById = 10010;

        /// <summary>
        /// Event ID for a successfully created entity.
        /// </summary>
        public const int EntityCreated = 10020;
        /// <summary>
        /// Event ID for a successfully updated entity.
        /// </summary>
        public const int EntityUpdated = 10021;
        /// <summary>
        /// Event ID for a successfully deleted entity.
        /// </summary>
        public const int EntityDeleted = 10022;
        /// <summary>
        /// Event ID for an entity found by ID.
        /// </summary>
        public const int EntityFoundById = 10030;
        /// <summary>
        /// Event ID for an entity not found by ID.
        /// </summary>
        public const int EntityNotFoundById = 10031;

        /// <summary>
        /// Event ID for an entity not found warning.
        /// </summary>
        public const int EntityNotFound = -1001;
        /// <summary>
        /// Event ID for an entity not deleted warning.
        /// </summary>
        public const int EntityNotDeleted = -1002;
        /// <summary>
        /// Event ID for an entity not updated warning.
        /// </summary>
        public const int EntityNotUpdated = -1003;
        /// <summary>
        /// Event ID for user context not set warning.
        /// </summary>
        public const int UserNotSet = -1014;
        /// <summary>
        /// Event ID for entity owner not matching user warning.
        /// </summary>
        public const int EntityOwnerNotMatching = -1015;
        /// <summary>
        /// Event ID for an unknown error.
        /// </summary>
        public const int UnknownError = -1000;
    }
}
