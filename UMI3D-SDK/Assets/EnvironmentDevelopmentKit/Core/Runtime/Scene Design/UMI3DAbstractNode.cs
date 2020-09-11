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
using System.Linq;
using umi3d.common;
using UnityEngine;

namespace umi3d.edk
{
    /// <summary>
    /// UMI3D empty object.
    /// </summary>
    [DisallowMultipleComponent]
    [SelectionBase]
    public abstract class UMI3DAbstractNode : MonoBehaviour, UMI3DLoadableEntity
    {

        #region properties

        /// <summary>
        /// Indicates if InitDefinition has been called.
        /// </summary>
        protected bool inited = false;

        /// <summary>
        /// The objects's unique id. 
        /// </summary>
        protected string objectId;

        /// <summary>
        /// The public Getter for objectId.
        /// </summary>
        public string Id()
        {
            Register();
            return objectId;
        }

        /// <summary>
        /// The public Getter for object's parent Id.
        /// </summary>
        /// <returns></returns>
        UMI3DAbstractNode Parent
        {
            get
            {
                if (transform.parent != null)
                {
                    var p = transform.parent.gameObject.GetComponent<UMI3DAbstractNode>();
                    return p;
                }
                return null;
            }
        }
        public UMI3DAsyncProperty<UMI3DAbstractNode> objectParentId;

        /// <summary>
        /// False if the object is alowed to move during the application exectution.
        /// </summary>
        [SerializeField]
        protected bool isStatic = false;
        /// <summary>
        /// Indicates if the object is only vissible in full 3D media displayers (sush as Computer or Virtual reality headset)
        /// </summary>
        public UMI3DAsyncProperty<bool> objectIsStatic;


        /// <summary>
        /// False if the object is alowed to move during the application exectution.
        /// </summary>
        [SerializeField]
        protected bool active = true;
        /// <summary>
        /// Indicates if the object is only vissible in full 3D media displayers (sush as Computer or Virtual reality headset)
        /// </summary>
        public UMI3DAsyncProperty<bool> objectActive;


        public UMI3DAnchor UMI3DAnchor;
        public UMI3DAsyncProperty<UMI3DAnchorDto> objectAnchor;


        /// <summary>
        /// An editor field to modify default objectImmersiveOnly value
        /// </summary>
        public bool immersiveOnly = false;
        /// <summary>
        /// Indicates if the object is only vissible in full 3D media displayers (sush as Computer or Virtual reality headset)
        /// </summary>
        public UMI3DAsyncProperty<bool> objectImmersiveOnly;


        //public AsyncPropertiesHandler PropertiesHandler { protected set; get; }
        UMI3DAsyncPropertyEquality PropertyEquality;
        
        /// <summary>
        /// The object local position in the scene graph.
        /// </summary>
        public UMI3DAsyncProperty<Vector3> objectPosition;

        /// <summary>
        /// The object local orientation in the scene graph.
        /// </summary>
        public UMI3DAsyncProperty<Quaternion> objectRotation;

        /// <summary>
        /// The object local scale in the scene graph.
        /// </summary>
        public UMI3DAsyncProperty<Vector3> objectScale;
        
        #endregion


        #region initialization

        /// <summary>
        /// Check if the AbstractObject3D has been registered to to the UMI3DScene and do it if not
        /// </summary>
        /// <returns>Return a LoadEntity</returns>
        public virtual LoadEntity Register()
        {
            if (objectId == null && UMI3DEnvironment.Exists)
            {
                objectId = UMI3DEnvironment.Register(this);
                InitDefinition(objectId);
            }
            return GetLoadEntity();
        }

        protected virtual LoadEntity GetLoadEntity()
        {
            var operation = new LoadEntity()
            {
                entity = this,
                users = new HashSet<UMI3DUser>(UMI3DEnvironment.GetEntities<UMI3DUser>())
            };
            return operation;
        }

        /// <summary>
        /// Initialize object's properties.
        /// </summary>
        protected virtual void InitDefinition(string id)
        {
            objectId = id;

            objectIsStatic = new UMI3DAsyncProperty<bool>(objectId, UMI3DPropertyKeys.Static, this.isStatic);
            objectActive = new UMI3DAsyncProperty<bool>(objectId, UMI3DPropertyKeys.Active, this.active);
            objectIsStatic.OnValueChanged += (bool s) => isStatic = s;
            objectActive.OnValueChanged += (bool a) => active = a;

            objectParentId = new UMI3DAsyncProperty<UMI3DAbstractNode>(objectId, UMI3DPropertyKeys.ParentId, Parent,(UMI3DAbstractNode node,UMI3DUser user)=>node.Id());
            objectParentId.OnValueChanged += (UMI3DAbstractNode node) => { if (transform.parent != node?.transform) transform.SetParent(node?.transform); };

            PropertyEquality = new UMI3DAsyncPropertyEquality();
            PropertyEquality.epsilon = 0.000001f;

            objectPosition = new UMI3DAsyncProperty<Vector3>(objectId, UMI3DPropertyKeys.Position, transform.localPosition,ToUMI3DSerializable.ToSerializableVector3,  PropertyEquality.Vector3Equality);
            objectPosition.OnValueChanged += (Vector3 p) => transform.localPosition = p;

            objectRotation = new UMI3DAsyncProperty<Quaternion>(objectId, UMI3DPropertyKeys.Rotation, transform.localRotation, ToUMI3DSerializable.ToSerializableVector4, PropertyEquality.QuaternionEquality);

            objectRotation.OnValueChanged += (Quaternion r) => transform.localRotation = r;

            objectScale = new UMI3DAsyncProperty<Vector3>(objectId, UMI3DPropertyKeys.Scale, transform.localScale, ToUMI3DSerializable.ToSerializableVector3);
            objectScale.OnValueChanged += (Vector3 s) => transform.localScale = s;

            objectImmersiveOnly = new UMI3DAsyncProperty<bool>(objectId, UMI3DPropertyKeys.VROnly, this.immersiveOnly);
            objectImmersiveOnly.OnValueChanged += (bool b) => immersiveOnly = b;

            objectAnchor = new UMI3DAsyncProperty<UMI3DAnchorDto>(objectId, UMI3DPropertyKeys.Anchor, UMI3DAnchor?.ToDto());
            objectAnchor.OnValueChanged += (UMI3DAnchorDto a) => { UMI3DAnchor.PositionOffset = a.positionOffset; UMI3DAnchor.RotationOffset = a.rotationOffset; UMI3DAnchor.ScaleOffset = a.scaleOffset; };

            inited = true;
        }


        #endregion

        protected virtual void WriteProperties(UMI3DAbstractNodeDto dto, UMI3DUser user)
        {
            dto.id = Id();
            dto.pid = objectParentId.GetValue(user)?.Id();
            dto.active = objectActive.GetValue(user);
            dto.isStatic = objectIsStatic.GetValue(user);
            dto.immersiveOnly = objectImmersiveOnly.GetValue(user);
            dto.anchorDto = objectAnchor.GetValue(user);
        }


        #region sub objects

        /// <summary>
        /// Get sub objects for a given user.
        /// </summary>
        /// <param name="user">User to give children to</param>
        /// <returns></returns>
        public List<UMI3DAbstractNode> GetChildren(UMI3DUser user)
        {
            var res = new List<UMI3DAbstractNode>();
            foreach (Transform ct in transform)
            {
                var child = ct.gameObject.GetComponent<UMI3DAbstractNode>();
                if (child != null)
                {
                    if (child is UMI3DScene)
                        res.Add(child);
                    else if (child is UMI3DNode) { 
                        UMI3DNode obj = child as UMI3DNode;
                        if(obj.VisibleFor(user))
                            res.Add(child);
                    }
                }
            }
            return res;
        }

        /// <summary>
        /// Get all sub objects for a given user and scene.
        /// </summary>
        /// <param name="user">User to give children to</param>
        /// <returns></returns>
        protected List<UMI3DNode> GetAllChildrenInThisScene(UMI3DUser user)
        {
            var res = new List<UMI3DNode>();
            foreach (Transform ct in transform)
            {
                var child = ct.gameObject.GetComponent<UMI3DAbstractNode>();
                if (child != null)
                {
                    if (child is UMI3DNode)
                    {
                        UMI3DNode obj = child as UMI3DNode;
                        if (obj.VisibleFor(user))
                        {
                            res.Add(obj);
                            res.AddRange(child.GetAllChildrenInThisScene(user));
                        }
                    }
                }
            }
            return res;
        }

        public List<UMI3DLoadableEntity> GetAllLoadableEntityUnderThisNode(UMI3DUser user,Transform transform = null)
        {
            var res = new List<UMI3DLoadableEntity>();

            if (transform == null) transform = this.transform;
            else if (transform.gameObject.GetComponent<UMI3DAbstractNode>() != null) return res;
            
            var others = transform.gameObject.GetComponents<UMI3DLoadableEntity>()?.Where(i => !(i is UMI3DAbstractNode));
            if (others != null)
            {
                res.AddRange(others);
            }
            foreach (Transform ct in transform)
            {
                res.AddRange(GetAllLoadableEntityUnderThisNode(user, ct));
            }
            return res;
        }


        public abstract IEntity ToEntityDto(UMI3DUser user);

        #endregion

    }

}
