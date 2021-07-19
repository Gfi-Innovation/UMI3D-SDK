﻿/*
Copyright 2019 - 2021 Inetum

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

using System;
using System.Collections.Generic;
using umi3d.cdk.userCapture;
using umi3d.common;
using umi3d.common.userCapture;
using UnityEngine;

namespace umi3d.cdk
{
    /// <summary>
    /// Loader for UMI3DHandPose
    /// </summary>
    public class UMI3DHandPoseLoader : UMI3DNodeLoader
    {
        /// <summary>
        /// Load a UMI3DHandPose
        /// </summary>
        /// <param name="dto"></param>
        public virtual void Load(UMI3DHandPoseDto dto)
        {
            UMI3DEnvironmentLoader.RegisterEntityInstance(dto.id, dto, null);
        }

        /// <summary>
        /// Update a property.
        /// </summary>
        /// <param name="entity">entity to be updated.</param>
        /// <param name="property">property containing the new value.</param>
        /// <returns></returns>
        public override bool SetUMI3DProperty(UMI3DEntityInstance entity, SetEntityPropertyDto property)
        {
            var dto = entity.dto as UMI3DHandPoseDto;
            if (dto == null) return false;
            switch (property.property)
            {
                case UMI3DPropertyKeys.ActiveHandPose:
                    dto.IsActive = (bool)property.value;
                    // activate hand pose algorithm
                    break;
                default:
                    return false;
            }
            return true;
        }
    }
}