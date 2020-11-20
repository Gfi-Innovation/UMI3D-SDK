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
using System.Collections.Generic;
using umi3d.common.interaction;
using UnityEngine;

namespace umi3d.cdk.interaction
{
    /// <summary>
    /// Abstract class for UMI3D Player.
    /// </summary>
    public abstract class AbstractInteractionMapper : MonoBehaviour
    {
        /// <summary>
        /// Singleton instance.
        /// </summary>
        public static AbstractInteractionMapper Instance;

        /// <summary>
        /// The Interaction Controllers.
        /// Should be input devices (or groups of input devices) connectors.
        /// </summary>
        [SerializeField]
        protected List<AbstractController> Controllers = new List<AbstractController>();


        protected virtual void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(this);
            }
        }


        /// <summary>
        /// Reset the InteractionMapper module.
        /// </summary>
        public abstract void ResetModule();


        /// <summary>
        /// Check if a toolbox with the given id exists.
        /// </summary>
        public abstract bool ToolboxExists(string id);

        /// <summary>
        /// Get the toolbox with the given id (if any).
        /// </summary>
        public abstract Toolbox GetToolbox(string id);

        /// <summary>
        /// Return the toolboxes matching a given condition.
        /// </summary>
        public abstract IEnumerable<Toolbox> GetToolboxes(Predicate<Toolbox> condition);

        /// <summary>
        /// Return all known toolboxes.
        /// </summary>
        /// <returns></returns>
        public virtual IEnumerable<Toolbox> GetToolboxes() { return GetToolboxes(t => true); }


        /// <summary>
        /// Check if a tool with the given id exists.
        /// </summary>
        public abstract bool ToolExists(string id);

        /// <summary>
        /// Return true if the tool is currently projected on a controller.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public abstract bool IsToolSelected(string id);

        /// <summary>
        /// Get the tool with the given id (if any).
        /// </summary>
        public abstract AbstractTool GetTool(string id);

        /// <summary>
        /// Return the tools matching a given condition.
        /// </summary>
        public abstract IEnumerable<AbstractTool> GetTools(Predicate<AbstractTool> condition);

        /// <summary>
        /// Return all known tools.
        /// </summary>
        /// <returns></returns>
        public virtual IEnumerable<AbstractTool> GetTools() { return GetTools(t => true); }


        /// <summary>
        /// Check if an interaction with the given id exists.
        /// </summary>
        public abstract bool InteractionExists(string id);

        /// <summary>
        /// Get the interaction with the given id (if any).
        /// </summary>
        public abstract AbstractInteractionDto GetInteraction(string id);

        /// <summary>
        /// Return the interactions matching a given condition.
        /// </summary>
        public abstract IEnumerable<AbstractInteractionDto> GetInteractions(Predicate<AbstractInteractionDto> condition);

        /// <summary>
        /// Return all known interactions.
        /// </summary>
        /// <returns></returns>
        public virtual IEnumerable<AbstractInteractionDto> GetInteractions() { return GetInteractions(t => true); }

        /// <summary>
        /// Get the controller onto a given tool has been projected.
        /// </summary>
        /// <param name="projectedToolId">Tool's id</param>
        /// <returns></returns>
        public abstract AbstractController GetController(string projectedToolId);


        public abstract void CreateToolbox(Toolbox toolbox);

        public abstract void CreateTool(Tool tool);

        /// <summary>
        /// Request the selection of a Tool.
        /// Be careful,this method could be called before the tool is added for async loading reasons.
        /// Returns true if the tool has been successfuly selected, false otherwise.
        /// </summary>
        /// <param name="dto">The tool to be selected</param>
        public abstract bool SelectTool(string toolId, bool releasable, string hoveredObjectId, InteractionMappingReason reason = null);

        /// <summary>
        /// Request a Tool to be released.
        /// </summary>
        /// <param name="dto">The tool to be released</param>
        public abstract void ReleaseTool(string toolId, InteractionMappingReason reason = null);

        /// <summary>
        /// Request a Tool to be replaced by another one.
        /// </summary>
        /// <param name="selected">The tool to be selected</param>
        /// <param name="released">The tool to be released</param>
        public abstract bool SwitchTools(string selected, string released, bool releasable, string hoveredObjectId, InteractionMappingReason reason = null);

    }
}