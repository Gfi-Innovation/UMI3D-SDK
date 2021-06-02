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

namespace umi3d.common.interaction
{
    [System.Serializable]
    public class ToolReleasedDto : AbstractBrowserRequestDto
    {

        public ulong toolId;

        public uint boneType;

        protected override uint GetOperationId() { return UMI3DOperationKeys.ToolReleased; }

        public override (int, Func<byte[], int, int>) ToByteArray(params object[] parameters)
        {
            var fb = base.ToByteArray(parameters);

            int size = UMI3DNetworkingHelper.GetSize(toolId) + UMI3DNetworkingHelper.GetSize(boneType) + fb.Item1;
            Func<byte[], int, int> func = (b, i) =>
            {
                i += fb.Item2(b, i);
                i += UMI3DNetworkingHelper.Write(toolId, b, i);
                i += UMI3DNetworkingHelper.Write(boneType, b, i);
                return size;
            };
            return (size, func);
        }
    }
}
