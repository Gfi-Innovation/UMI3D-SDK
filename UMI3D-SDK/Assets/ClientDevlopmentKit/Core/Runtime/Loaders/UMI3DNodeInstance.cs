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

using System.Collections.Generic;
using UnityEngine;

namespace umi3d.cdk
{
    /// <summary>
    /// Object containing a dto for entity with gameobject
    /// </summary>
    public class UMI3DNodeInstance : UMI3DEntityInstance
    {
        public GameObject gameObject;
        public Transform transform { get { return gameObject.transform; } }

        private List<Collider> _colliders;
        public List<Collider> colliders 
        {  
            get { 
                if (_colliders == null) 
                    _colliders = new List<Collider>();
                return _colliders;
            }
            set { _colliders = value; }
        }
    }
}