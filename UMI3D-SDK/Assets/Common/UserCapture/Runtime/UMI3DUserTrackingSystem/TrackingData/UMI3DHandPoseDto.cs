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

using System.Collections.Generic;

namespace umi3d.common
{
    public class UMI3DHandPoseDto : AbstractEntityDto, IEntity
    {
        public string PoseId;

        public bool IsActive;

        public bool IsRight;

        public string objectId;

        public SerializableVector3 HandPosition;
        public SerializableVector3 HandEulerRotation;

        public Dictionary<string, SerializableVector3> PhalanxRotations = new Dictionary<string, SerializableVector3>();
    }
}