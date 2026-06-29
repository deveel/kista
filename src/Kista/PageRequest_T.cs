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

namespace Kista
{
    /// <summary>
    /// Represents a request for a page of items from a repository,
    /// identifying the page number and the maximum number of items to return.
    /// </summary>
    public class PageRequest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PageRequest"/> class
        /// with the given page number and page size.
        /// </summary>
        /// <param name="page">The one-based number of the page to return.</param>
        /// <param name="size">The maximum number of items to return in the page.</param>
        public PageRequest(int page, int size) {
            ArgumentOutOfRangeException.ThrowIfLessThan(page, 1);
            ArgumentOutOfRangeException.ThrowIfLessThan(size, 1);

            Page = page;
            Size = size;
        }
        /// <summary>
        /// Gets the number of the page to return
        /// </summary>
        public int Page { get; }

        /// <summary>
        /// Gets the maximum number of items to be returned.
        /// </summary>
        public int Size { get; }

        /// <summary>
        /// Gets the starting offet in the repository where to start
        /// collecting the items to return
        /// </summary>
        public int Offset => (Page - 1) * Size;
    }
}