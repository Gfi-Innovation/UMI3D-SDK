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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using umi3d.common;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;

namespace umi3d.cdk
{
    /// <summary>
    /// 
    /// </summary>
    public class UMI3DEnvironmentLoader : Singleton<UMI3DEnvironmentLoader>
    {

        /// <summary>
        /// Index of any 3D object loaded.
        /// </summary>
        Dictionary<string, UMI3DEntityInstance> entities = new Dictionary<string, UMI3DEntityInstance>();

        /// <summary>
        /// Return a list of all registered entities.
        /// </summary>
        /// <returns></returns>
        public static List<UMI3DEntityInstance> Entities() { return Exists ? Instance.entities.Values.ToList() : null; }

        /// <summary>
        /// Get an entity with an id.
        /// </summary>
        /// <param name="id">unique id of the entity.</param>
        /// <returns></returns>
        public static UMI3DEntityInstance GetEntity(string id) { return id != null && Exists && Instance.entities.ContainsKey(id) ? Instance.entities[id] : null; }

        /// <summary>
        /// Get a node with an id.
        /// </summary>
        /// <param name="id">unique id of the entity.</param>
        /// <returns></returns>
        public static UMI3DNodeInstance GetNode(string id) { return id != null && Exists && Instance.entities.ContainsKey(id) ? Instance.entities[id] as UMI3DNodeInstance : null; }

        /// <summary>
        /// Get a node id with a collider.
        /// </summary>
        /// <param name="collider">collider.</param>
        /// <returns></returns>
        public static string GetNodeID(Collider collider) { return Exists ? Instance.entities.Where(k => k.Value is UMI3DNodeInstance).FirstOrDefault(k=>(k.Value as UMI3DNodeInstance).colliders.Any(c=>c==collider)).Key : null; }

        /// <summary>
        /// Register a node instance.
        /// </summary>
        /// <param name="id">unique id of the node.</param>
        /// <param name="dto">dto of the node.</param>
        /// <param name="instance">gameobject of the node.</param>
        /// <param name="issubObject">id this node a sub object of an other node.</param>
        /// <returns></returns>
        public static UMI3DNodeInstance RegisterNodeInstance(string id, UMI3DDto dto, GameObject instance, bool issubObject = false)
        {
            if (!Exists || instance == null)
                return null;
            else if (Instance.entities.ContainsKey(id))
            {
                UMI3DNodeInstance node = Instance.entities[id] as UMI3DNodeInstance;
                if (node == null)
                    throw new Exception($"id:{id} found but the value was of type {Instance.entities[id].GetType()}");
                if (node.gameObject != instance)
                    Destroy(instance);
                return node;
            }
            else
            {
                UMI3DNodeInstance node = new UMI3DNodeInstance() { gameObject = instance, dto = dto };
                Instance.entities.Add(id, node);
                return node;
            }
        }

        /// <summary>
        /// Register an entity without a gameobject.
        /// </summary>
        /// <param name="id">unique id of the node.</param>
        /// <param name="dto">dto of the node.</param>
        /// <returns></returns>
        public static UMI3DEntityInstance RegisterEntityInstance(string id, UMI3DDto dto, object Object)
        {
            if (!Exists)
                return null;
            else if (Instance.entities.ContainsKey(id))
            {
                return Instance.entities[id];
            }
            else
            {
                UMI3DEntityInstance node = new UMI3DEntityInstance() { dto = dto, Object = Object };
                Instance.entities.Add(id, node);
                return node;
            }
        }

        /// <summary>
        /// Index of any 3D object loaded.
        /// </summary>
        GlTFEnvironmentDto environment;

        /// <summary>
        /// Number of UMI3D nodes.
        /// = Number of scenes + Number of glTF nodes
        /// </summary>
        int nodesToInstantiate = 0;

        /// <summary>
        /// Number of UMI3D nodes.
        /// = Number of scenes + Number of glTF nodes
        /// </summary>
        int instantiatedNodes = 0;

        /// <summary>
        /// Number of UMI3D nodes.
        /// = Number of scenes + Number of glTF nodes
        /// </summary>
        int resourcesToLoad = 0;

        /// <summary>
        /// Number of loaded resources.
        /// </summary>
        int loadedResources = 0;

        public UMI3DSceneLoader sceneLoader { get; private set; }
        public GlTFNodeLoader nodeLoader { get; private set; }
        public UMI3DPbrMaterialLoader materialLoader { get; private set; }


        protected override void Awake()
        {
            base.Awake();
            sceneLoader = new UMI3DSceneLoader(this);
            nodeLoader = new GlTFNodeLoader();
            materialLoader = new UMI3DPbrMaterialLoader();
        }

        #region workflow

        /// <summary>
        /// Indicates if a UMI3D environment has been loaded
        /// </summary>
        public bool started { get; private set; } = false;

        /// <summary>
        /// Indicates if the UMI3D environment's resources has been loaded
        /// </summary>
        public bool downloaded { get; private set; } = false;

        /// <summary>
        /// Indicates if the UMI3D environment has been fully loaded
        /// </summary>
        public bool loaded { get; private set; } = false;

        [System.Serializable]
        public class ProgressListener : UnityEvent<float> { }
        public ProgressListener onProgressChange = new ProgressListener();

        public UnityEvent onResourcesLoaded = new UnityEvent();
        public UnityEvent onEnvironmentLoaded = new UnityEvent();

        /// <summary>
        /// Load the Environment.
        /// </summary>
        /// <param name="dto">Dto of the environement.</param>
        /// <param name="onSuccess">Finished callback.</param>
        /// <param name="onError">Error callback.</param>
        /// <returns></returns>
        public IEnumerator Load(GlTFEnvironmentDto dto, Action onSuccess, Action<string> onError)
        {
            environment = dto;
            RegisterEntityInstance(UMI3DGlobalID.EnvironementId, dto, null);
            nodesToInstantiate = dto.scenes.Count;
            foreach (GlTFSceneDto sce in dto.scenes)
                nodesToInstantiate += sce.nodes.Count;

            //
            // Load resources
            //
            StartCoroutine(LoadResources(dto));
            while (!downloaded)
            {
                onProgressChange.Invoke(resourcesToLoad == 0 ? 1f : loadedResources / resourcesToLoad);
                yield return new WaitForEndOfFrame();
            }
            onProgressChange.Invoke(1f);
            onResourcesLoaded.Invoke();
            //
            // Instantiate nodes
            //

            ReadUMI3DExtension(dto, null);

            onProgressChange.Invoke(0f);
            InstantiateNodes();
            while (!loaded)
            {
                onProgressChange.Invoke(nodesToInstantiate == 0 ? 1f : instantiatedNodes / nodesToInstantiate);
                yield return new WaitForEndOfFrame();
            }
            onProgressChange.Invoke(1f);
            onEnvironmentLoaded.Invoke();
            yield return null;
            onSuccess.Invoke();
        }

        #endregion

        #region resources

        /// <summary>
        /// Load the environment's resources
        /// </summary>
        IEnumerator LoadResources(GlTFEnvironmentDto dto)
        {
            started = true;
            downloaded = false;
            List<string> ids = dto.extensions.umi3d.LibrariesId;
            foreach (var scene in dto.scenes)
                ids.AddRange(scene.extensions.umi3d.LibrariesId);
            yield return StartCoroutine(UMI3DResourcesManager.LoadLibraries(ids, (i) => { loadedResources = i; }, (i) => { resourcesToLoad = i; }));
            downloaded = true;
        }

        #endregion

        #region parameters

        public AbstractUMI3DLoadingParameters parameters;
        public static AbstractUMI3DLoadingParameters Parameters { get { return Exists ? Instance.parameters : null; } }

        #endregion

        #region instantiation

        /// <summary>
        /// Load the environment's resources
        /// </summary>
        void InstantiateNodes()
        {
            Action finished = () => { loaded = true; };
            StartCoroutine(_InstantiateNodes(environment.scenes, finished));
        }

        /// <summary>
        /// Load scenes 
        /// </summary>
        /// <param name="scenes">scenes to loads</param>
        /// <returns></returns>
        IEnumerator _InstantiateNodes(List<GlTFSceneDto> scenes, Action finished)
        {
            //Load scenes without hierarchy
            foreach (var scene in scenes)
            {
                bool isFinished = false;
                sceneLoader.LoadGlTFScene(scene, () => isFinished = true, (i) => instantiatedNodes = i); ;
                yield return new WaitUntil(() => isFinished == true);
            }

            int count = 0;
            //Organize scenes
            foreach (var scene in scenes)
            {
                count += 1;
                UMI3DNodeInstance node = entities[scene.extensions.umi3d.id] as UMI3DNodeInstance;
                UMI3DSceneNodeDto umi3dScene = scene.extensions.umi3d;
                sceneLoader.ReadUMI3DExtension(umi3dScene, node.gameObject,()=> { count -= 1; instantiatedNodes += 1; }, (s) => { count -= 1; Debug.LogWarning(s); });
                node.gameObject.SetActive(true);
            }
            yield return new WaitUntil(() => count <= 0);
            finished.Invoke();
            yield return null;
        }

        #endregion


        /// <summary>
        /// Load IEntity.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="performed"></param>
        static public void LoadEntity(IEntity entity, Action performed)
        {
            if(Exists)Instance._LoadEntity( entity, performed);
        }

        /// <summary>
        /// Load IEntity.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="performed"></param>
        void _LoadEntity(IEntity entity, Action performed)
        {
            switch (entity)
            {
                case GlTFSceneDto scene:
                    StartCoroutine(_InstantiateNodes(new List<GlTFSceneDto>() { scene }, performed));
                    break;
                case GlTFNodeDto node:
                    StartCoroutine(nodeLoader.LoadNodes(new List<GlTFNodeDto>() { node }, performed));
                    break;
                case AssetLibraryDto library:
                    UMI3DResourcesManager.DownloadLibrary(library,
                        UMI3DClientServer.Media.name,
                        () =>
                        {
                            UMI3DResourcesManager.LoadLibrary(library.id, performed);
                        });
                    break;
                case AbstractEntityDto dto:
                    Parameters.ReadUMI3DExtension(dto, null, performed, (s) => { Debug.Log(s); performed.Invoke(); });
                    break;
                case GlTFMaterialDto matDto:
                    materialLoader.LoadMaterialFromExtension(matDto, (m) =>
                    {
                        m.name = matDto.name;
                        //register the material
                        RegisterEntityInstance(matDto.extensions.umi3d.id, matDto, m);
                    });

                    break;
                default:
                    Debug.Log($"load entity fail missing case {entity.GetType()}");
                    break;

            }
        }

        /// <summary>
        /// Delete IEntity
        /// </summary>
        /// <param name="entityId"></param>
        /// <param name="performed"></param>
        static public void DeleteEntity(string entityId, Action performed)
        {
            if (Instance.entities.ContainsKey(entityId))
            {
                UMI3DEntityInstance entity = Instance.entities[entityId];
                if (entity is UMI3DNodeInstance)
                {
                    if(entity.dto is GlTFSceneDto)
                    {
                        var sceneDto = (entity.dto as GlTFSceneDto).extensions.umi3d;
                        foreach(var library in sceneDto.LibrariesId)
                            UMI3DResourcesManager.UnloadLibrary(library,sceneDto.id);
                    }

                    UMI3DNodeInstance node = entity as UMI3DNodeInstance;
                    Destroy(node.gameObject);
                }
                Instance.entities.Remove(entityId);
            }
            else if (UMI3DResourcesManager.isKnowedLibrary(entityId))
            {
                UMI3DResourcesManager.UnloadLibrary(entityId);
            }
            else
                Debug.LogError($"Entity [{entityId}] To Destroy Not Found");
            performed?.Invoke();
        }

        /// <summary>
        /// Clear an environement and make the client ready to load a new environment.
        /// </summary>
        static public void Clear()
        {
            foreach(var entity in Instance.entities.ToList().Select(p=> { return p.Key; }))
            {
                DeleteEntity(entity, null);
            }
            UMI3DResourcesManager.Instance.ClearCache();
        }

        /// <summary>
        /// Load environment.
        /// </summary>
        /// <param name="dto"></param>
        /// <param name="node"></param>
        public virtual void ReadUMI3DExtension(GlTFEnvironmentDto dto, GameObject node)
        {
            var extension = dto?.extensions?.umi3d;
            if (extension != null)
            {
                foreach (var scene in extension.preloadedScenes)
                    Parameters.ReadUMI3DExtension(scene,node,null,null);
                RenderSettings.ambientMode = (AmbientMode)extension.ambientType;
                RenderSettings.ambientSkyColor = extension.skyColor;
                RenderSettings.ambientEquatorColor = extension.horizontalColor;
                RenderSettings.ambientGroundColor = extension.groundColor;
                RenderSettings.ambientIntensity = extension.ambientIntensity;
                if (extension.skybox != null)
                {
                    Parameters.loadSkybox(extension.skybox);
                }
            }
        }

        /// <summary>
        /// Update a property.
        /// </summary>
        /// <param name="entity">Entity to update.</param>
        /// <param name="property">Property containing the new value.</param>
        /// <returns></returns>
        public static bool SetUMI3DPorperty(UMI3DEntityInstance entity, SetEntityPropertyDto property)
        {
            if (entity == null) return false;
            var dto = ((entity.dto as GlTFEnvironmentDto)?.extensions as GlTFEnvironmentExtensions)?.umi3d;
            if (dto == null) return false;
            switch (property.property)
            {
                case UMI3DPropertyKeys.PreloadedScenes:
                    return Parameters.SetUMI3DProperty(entity,property);
                default:
                    return false;
            }
        }


        /// <summary>
        /// Handle SetEntityPropertyDto operation.
        /// </summary>
        /// <param name="dto">set operation to handle.</param>
        /// <returns></returns>
        public static bool SetEntity(SetEntityPropertyDto dto)
        {
            if (!Exists) return false;
            var node = UMI3DEnvironmentLoader.GetEntity(dto.entityId);
            if (node == null) {
                Instance.StartCoroutine(Instance._SetEntity(dto));
                return false;
            }
            else
            {
                if (SetUMI3DPorperty(node, dto)) return true;
                if (UMI3DEnvironmentLoader.Exists && UMI3DEnvironmentLoader.Instance.sceneLoader.SetUMI3DProperty(node, dto)) return true;
                return Parameters.SetUMI3DProperty(node, dto);
            }
        }

        IEnumerator _SetEntity(SetEntityPropertyDto dto)
        {
            var wait = new WaitForFixedUpdate();
            UMI3DEntityInstance node = null;
            yield return wait;
            while((node = UMI3DEnvironmentLoader.GetEntity(dto.entityId)) == null)
            {
                yield return wait;
                Debug.Log($"{dto.entityId} not found, will try again next fixed frame");
            }
            if (SetUMI3DPorperty(node, dto)) yield break;
            if (UMI3DEnvironmentLoader.Exists && UMI3DEnvironmentLoader.Instance.sceneLoader.SetUMI3DProperty(node, dto)) yield break;
            Parameters.SetUMI3DProperty(node, dto);
        }

    }
}