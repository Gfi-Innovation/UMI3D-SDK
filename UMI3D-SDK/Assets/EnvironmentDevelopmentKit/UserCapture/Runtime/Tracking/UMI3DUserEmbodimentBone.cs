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

using umi3d.common.userCapture;
using UnityEngine;

namespace umi3d.edk.userCapture
{
    public class UMI3DUserEmbodimentBone
    {
        public struct SpatialPosition
        {
            public Vector3 localPosition;
            public Quaternion localRotation;
            public Vector3 localScale;
        }

        public string userId { get; protected set; }

        public string boneType { get; protected set; }

        public SpatialPosition spatialPosition;

        public bool isTracked;

        public UMI3DUserEmbodimentBone(string userId, string boneType)
        {
            this.userId = userId;
            this.boneType = boneType;
            spatialPosition = new SpatialPosition();
        }
    }

    public class Binding
    {
        public string boneType;
        public bool isBinded = true;
        public UMI3DNode node;
        public string rigName = "";
        public Vector3 offsetPosition = Vector3.zero;
        public Quaternion offsetRotation = Quaternion.identity;

        public BoneBindingDto ToDto(UMI3DUser user)
        {
            BoneBindingDto dto = new BoneBindingDto()
            {
                rigName = rigName,
                active = isBinded,
                boneType = boneType,
                position = offsetPosition,
                rotation = offsetRotation,
            };

            if (node != null)
                dto.objectId = node.Id();
            else
                dto.objectId = "";

            if (user != null)
                dto.bindingId = user.Id() + "binding" + rigName + dto.objectId;
            else
                dto.bindingId = "binding" + rigName + dto.objectId;

            return dto;
        }
    }
}
