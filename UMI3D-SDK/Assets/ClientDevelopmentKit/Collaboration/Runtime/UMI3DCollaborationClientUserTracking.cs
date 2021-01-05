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

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using umi3d.cdk.userCapture;
using umi3d.common.collaboration;
using UnityEngine;

namespace umi3d.cdk.collaboration
{
    public class UMI3DCollaborationClientUserTracking : UMI3DClientUserTracking
    {
        ///<inheritdoc/>
        protected override IEnumerator DispatchTracking()
        {
            while (sendTracking)
            {
                if (targetTrackingFPS > 0)
                {
                    BonesIterator();

                    DataChannel dc = UMI3DCollaborationClientServer.dataChannels.FirstOrDefault(d => d.reliable == false && d.type == DataChannelTypes.Tracking);
                    if (dc != null)
                        dc.Send(LastFrameDto.ToBson());

                    yield return new WaitForSeconds(1f / targetTrackingFPS);
                }
                else
                    yield return new WaitUntil(() => targetTrackingFPS > 0 || !sendTracking);
            }
        }

        ///<inheritdoc/>
        protected override IEnumerator DispatchCamera()
        {
            DataChannel dc;
            while ( !(UMI3DClientServer.Exists && UMI3DCollaborationClientServer.Exists) || UMI3DClientServer.Instance.GetId() == null || (dc = UMI3DCollaborationClientServer.dataChannels.FirstOrDefault(d => d.reliable == false && d.type == DataChannelTypes.Tracking)) == default)
            {
                yield return null;
            }
            dc.Send(CameraPropertiesDto.ToBson());
        }
    }
}
