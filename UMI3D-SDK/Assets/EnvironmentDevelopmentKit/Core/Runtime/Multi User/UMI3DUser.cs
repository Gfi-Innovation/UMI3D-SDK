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

using System;
using umi3d.common;
namespace umi3d.edk
{
    public class UMI3DUser : UMI3DEntity
    {

        #region identity

        /// <summary>
        /// The unique user identifier.
        /// </summary>
        private string userId;

        /// <summary>
        /// The public Getter for objectId.
        /// </summary>
        public string Id()
        {
            if (userId == null && UMI3DEnvironment.Exists)
                userId = UMI3DEnvironment.Register(this);
            return userId;
        }


        #endregion

        #region session
        public bool hasImmersiveDevice;

        public StatusType status { get; protected set; } = StatusType.CREATED;

        public virtual void OnJoin(bool hasImmersiveDevice/* TBD camera properties,  TBD First 6D pose*/)
        {
            this.hasImmersiveDevice = hasImmersiveDevice;
            SetStatus(StatusType.READY);
        }

        public virtual void SetStatus(StatusType status)
        {
            this.status = status;
        }

        public IEntity ToEntityDto(UMI3DUser user)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}