﻿/*
Copyright 2019 Gfi Informatique

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
*/

namespace umi3d.common.interaction
{
    /// <summary>
    /// Requests a browser to project a tool.
    /// </summary>
    public class ProjectToolDto : AbstractOperationDto
    {
        /// <summary>
        /// Id of the tool to project.
        /// </summary>
        public string toolId;

        /// <summary>
        /// Can the client choose to release the tool.
        /// if false, the only way of releasing it is through a ReleaseToolDto.
        /// </summary>
        public bool releasable;
    }
}