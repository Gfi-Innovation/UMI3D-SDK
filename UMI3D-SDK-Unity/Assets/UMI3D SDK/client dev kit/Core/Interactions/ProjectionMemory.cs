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
using System.IO;
using umi3d.common;
using UnityEngine;


namespace umi3d.cdk
{
    public class ProjectionMemory : MonoBehaviour
    {
        protected string id_ = "";
        public string id 
        { 
            get 
            {
                if (id_.Equals(""))
                    id_ = (this.gameObject.GetInstanceID() + Random.Range(0, 1000)).ToString();
                return id_;
            } 
        }

        /// <summary>
        /// Projection memory.
        /// </summary>
        protected ProjectionTreeNode memoryRoot;

        protected virtual void Awake()
        {
            memoryRoot = new ProjectionTreeNode(id) { id = "root" };   
        }


        /// <summary>
        /// Get Inputs of a controller for a list of interactions.
        /// </summary>
        /// <param name="controller">The controller on which the input should be</param>
        /// <param name="interactions">the array of interaction for which an input is seeked</param>
        /// <param name="unused"></param>
        /// <returns></returns>
        public AbstractUMI3DInput[] GetInputs(AbstractController controller, AbstractInteractionDto[] interactions, bool unused = true)
        {
            ProjectionTreeNode currentMemoryTreeState = memoryRoot;

            System.Func<ProjectionTreeNode> deepProjectionCreation;
            System.Predicate<ProjectionTreeNode> adequation;
            System.Action<ProjectionTreeNode> chooseProjection;

            List<AbstractUMI3DInput> selectedInputs = new List<AbstractUMI3DInput>();

            for (int depth = 0; depth < interactions.Length; depth++)
            {
                AbstractInteractionDto interaction = interactions[depth];
                if (interaction is ManipulationDto)
                {
                    DofGroupOptionDto[] options = (interaction as ManipulationDto).dofSeparationOptions.ToArray();
                    DofGroupOptionDto bestDofGoupOption = controller.FindBest(options);

                    foreach (DofGroupDto sep in bestDofGoupOption.separations)
                    {
                        adequation = node =>
                        {
                            return (node is ManipulationNode) && ((node as ManipulationNode).manipulationDofGroupDto.dofs == sep.dofs);
                        };

                        deepProjectionCreation = () =>
                        {
                            AbstractUMI3DInput projection = controller.FindInput(interaction as ManipulationDto, sep, unused);

                            if (projection == null)
                                throw new NoInputFoundException();

                            return new ManipulationNode(id)
                            {
                                id = (interaction as ManipulationDto).Id,
                                manipulation = interaction as ManipulationDto,
                                manipulationDofGroupDto = sep,
                                projectedInput = projection
                            };
                        };

                        chooseProjection = node =>
                        {
                            selectedInputs.Add(node.projectedInput);
                        };

                        currentMemoryTreeState = Project(currentMemoryTreeState, adequation, deepProjectionCreation, chooseProjection);

                    }
                }
                else if (interaction is EventDto)
                {
                    adequation = node =>
                    {
                        return (node is EventNode) && (node as EventNode).evt.Name.Equals(interaction.Name);
                    };

                    deepProjectionCreation = () =>
                    {
                        AbstractUMI3DInput projection = controller.FindInput(interaction as EventDto, unused);

                        if (projection == null)
                            throw new NoInputFoundException();
                        return new EventNode(id)
                        {
                            id = (interaction as EventDto).Id,
                            evt = interaction as EventDto,
                            projectedInput = projection
                        };
                    };

                    chooseProjection = node =>
                    {
                        if (node == null)
                            throw new System.Exception("Internal error");
                        if (node.projectedInput == null)
                        {
                            throw new NoInputFoundException();
                        }

                        selectedInputs.Add(node.projectedInput);
                    };

                    currentMemoryTreeState = Project(currentMemoryTreeState, adequation, deepProjectionCreation, chooseProjection);
                }
                else if (interaction is AbstractParameterDto)
                {
                    adequation = node =>
                    {
                        return (node is ParameterNode)
                            && (node as ParameterNode).parameter.Dtype.Equals((interaction as AbstractParameterDto).Dtype);
                    };

                    deepProjectionCreation = () =>
                    {
                        AbstractUMI3DInput projection = controller.FindInput(interaction as AbstractParameterDto, unused);

                        if (projection == null)
                            throw new NoInputFoundException();

                        ParameterNode param = new ParameterNode(id)
                        {
                            id = (interaction as AbstractParameterDto).Id,
                            parameter = interaction as AbstractParameterDto,
                            projectedInput = projection
                        };

                        return param;
                    };

                    chooseProjection = node =>
                    {
                        selectedInputs.Add(node.projectedInput);
                    };

                    currentMemoryTreeState = Project(currentMemoryTreeState, adequation, deepProjectionCreation, chooseProjection);
                }
                else
                {
                    throw new System.Exception("Unknown interaction type, can't project !");
                }
            }


            return selectedInputs.ToArray();
        }

        /// <summary>
        /// Project a manipulation dof on a given controller and return associated input.
        /// </summary>
        /// <param name="controller">Controller to project on</param>
        /// <param name="manip">Manipulation to project</param>
        /// <param name="dof">Dof to project</param>
        /// <param name="unusedInputsOnly">Project on unused inputs only</param>
        public AbstractUMI3DInput PartialProject(AbstractController controller, ManipulationDto manip, DofGroupDto dof, bool unusedInputsOnly)
        {
            ProjectionTreeNode currentMemoryTreeState = memoryRoot;

            DofGroupOptionDto[] options = manip.dofSeparationOptions.ToArray();
            DofGroupOptionDto bestDofGoupOption = controller.FindBest(options);

            System.Predicate<ProjectionTreeNode> adequation = node =>
            {
                return (node is ManipulationNode) && ((node as ManipulationNode).manipulationDofGroupDto.dofs == dof.dofs);
            };

            System.Func<ProjectionTreeNode> deepProjectionCreation = () =>
            {
                AbstractUMI3DInput projection = controller.FindInput(manip, dof, unusedInputsOnly);

                if (projection == null)
                    throw new NoInputFoundException();

                return new ManipulationNode(id)
                {
                    id = manip.Id,
                    manipulation = manip,
                    manipulationDofGroupDto = dof,
                    projectedInput = projection
                };
            };

            System.Action<ProjectionTreeNode> chooseProjection = node =>
            {
                if (!node.projectedInput.IsAvailable())
                {
                    if (!unusedInputsOnly)
                        node.projectedInput.Dissociate();
                    else
                        throw new System.Exception("Internal error");
                }
                node.projectedInput.Associate(manip, dof.dofs);
            };

            currentMemoryTreeState = Project(currentMemoryTreeState, adequation, deepProjectionCreation, chooseProjection, unusedInputsOnly);
            return currentMemoryTreeState.projectedInput;
        }

        /// <summary>
        /// Project an event dto on a controller and return associated input.
        /// </summary>
        /// <param name="controller">Controller to project on</param>
        /// <param name="evt">Event dto to project</param>
        /// <param name="unusedInputsOnly">Project on unused inputs only</param>
        public AbstractUMI3DInput PartialProject(AbstractController controller, EventDto evt, bool unusedInputsOnly = false)
        {
            System.Func<ProjectionTreeNode> deepProjectionCreation = () =>
            {
                AbstractUMI3DInput projection = controller.FindInput(evt, true);
                if ((projection == null) && !unusedInputsOnly)
                    projection = controller.FindInput(evt, false);

                if (projection == null)
                    throw new NoInputFoundException();

                return new EventNode(id)
                {
                    id = evt.Id,
                    evt = evt,
                    projectedInput = projection
                };
            };

            System.Predicate<ProjectionTreeNode> adequation = node =>
            {
                return (node is EventNode) && (node as EventNode).evt.Name.Equals(evt.Name);
            };

            System.Action<ProjectionTreeNode> chooseProjection = node =>
            {
                if (!node.projectedInput.IsAvailable())
                {
                    if (!unusedInputsOnly)
                        node.projectedInput.Dissociate();
                    else
                        throw new System.Exception("Internal error");
                }
                node.projectedInput.Associate(evt);
            };

            return Project(memoryRoot, adequation, deepProjectionCreation, chooseProjection, unusedInputsOnly).projectedInput;
        }

        /// <summary>
        /// Project on a given controller a set of interactions and return associated inputs.
        /// </summary>
        /// <param name="controller">Controller to project interactions on</param>
        /// <param name="interactions">Interactions to project</param>
        public AbstractUMI3DInput[] Project(AbstractController controller, AbstractInteractionDto[] interactions)
        {
            ProjectionTreeNode currentMemoryTreeState = memoryRoot;

            System.Func<ProjectionTreeNode> deepProjectionCreation;
            System.Predicate<ProjectionTreeNode> adequation;
            System.Action<ProjectionTreeNode> chooseProjection;

            List<AbstractUMI3DInput> selectedInputs = new List<AbstractUMI3DInput>();

            for (int depth = 0; depth < interactions.Length; depth++)
            {
                AbstractInteractionDto interaction = interactions[depth];
                if (interaction is ManipulationDto)
                {
                    DofGroupOptionDto[] options = (interaction as ManipulationDto).dofSeparationOptions.ToArray();
                    DofGroupOptionDto bestDofGoupOption = controller.FindBest(options);

                    foreach (DofGroupDto sep in bestDofGoupOption.separations)
                    {
                        adequation = node =>
                        {
                            return (node is ManipulationNode) && ((node as ManipulationNode).manipulationDofGroupDto.dofs == sep.dofs);
                        };

                        deepProjectionCreation = () =>
                        {
                            AbstractUMI3DInput projection = controller.FindInput(interaction as ManipulationDto, sep, true);

                            if (projection == null)
                                throw new NoInputFoundException();

                            return new ManipulationNode(id)
                            {
                                id = (interaction as ManipulationDto).Id,
                                manipulation = interaction as ManipulationDto,
                                manipulationDofGroupDto = sep,
                                projectedInput = projection
                            };
                        };

                        chooseProjection = node =>
                        {
                            node.projectedInput.Associate(interaction as ManipulationDto, sep.dofs);
                            selectedInputs.Add(node.projectedInput);
                        };

                        currentMemoryTreeState = Project(currentMemoryTreeState, adequation, deepProjectionCreation, chooseProjection);

                    }
                }
                else if (interaction is EventDto)
                {
                    adequation = node =>
                    {
                        return (node is EventNode) && (node as EventNode).evt.Name.Equals(interaction.Name);
                    };

                    deepProjectionCreation = () =>
                    {
                        AbstractUMI3DInput projection = controller.FindInput(interaction as EventDto, true);

                        if (projection == null)
                            throw new NoInputFoundException();
                        return new EventNode(id)
                        {
                            id = (interaction as EventDto).Id,
                            evt = interaction as EventDto,
                            projectedInput = projection
                        };
                    };

                    chooseProjection = node =>
                    {
                        if (node == null)
                            throw new System.Exception("Internal error");
                        if (node.projectedInput == null)
                        {
                            throw new System.Exception("No input found");
                        }

                        node.projectedInput.Associate(interaction);
                        selectedInputs.Add(node.projectedInput);
                    };

                    currentMemoryTreeState = Project(currentMemoryTreeState, adequation, deepProjectionCreation, chooseProjection);
                }
                else if (interaction is AbstractParameterDto)
                {
                    adequation = node =>
                    {
                        return (node is ParameterNode)
                            && (node as ParameterNode).parameter.Dtype.Equals((interaction as AbstractParameterDto).Dtype);
                    };

                    deepProjectionCreation = () =>
                    {
                        AbstractUMI3DInput projection = controller.FindInput(interaction as AbstractParameterDto, true);

                        if (projection == null)
                            throw new NoInputFoundException();

                        ParameterNode param = new ParameterNode(id)
                        {
                            id = (interaction as AbstractParameterDto).Id,
                            parameter = interaction as AbstractParameterDto,
                            projectedInput = projection
                        };

                        return param;
                    };

                    chooseProjection = node =>
                    {
                        node.projectedInput.Associate(interaction);
                        selectedInputs.Add(node.projectedInput);
                    };

                    currentMemoryTreeState = Project(currentMemoryTreeState, adequation, deepProjectionCreation, chooseProjection);
                }
                else
                {
                    throw new System.Exception("Unknown interaction type : " + interaction);
                }
            }


            return selectedInputs.ToArray();
        }


        /// <summary>
        /// Navigates through tree and project an interaction. Updates the tree if necessary.
        /// </summary>
        /// <param name="nodeAdequationTest">Decides if the given projection node is adequate for the interaction to project</param>
        /// <param name="deepProjectionCreation">Create a new deep projection node, should throw an <see cref="NoInputFoundException"/> if no input is available</param>
        /// <param name="chooseProjection">Project the interaction to the given node's input</param>
        /// <param name="currentTreeNode">Current node in tree projection</param>
        /// <param name="unusedInputsOnly">Project on unused inputs only</param>
        /// <exception cref="NoInputFoundException"></exception>
        private ProjectionTreeNode Project(ProjectionTreeNode currentTreeNode,
            System.Predicate<ProjectionTreeNode> nodeAdequationTest,
            System.Func<ProjectionTreeNode> deepProjectionCreation,
            System.Action<ProjectionTreeNode> chooseProjection,
            bool unusedInputsOnly = true,
            bool updateMemory = true)
        {
            if (!unusedInputsOnly)
            {
                try
                {
                    ProjectionTreeNode p = Project(currentTreeNode, nodeAdequationTest, deepProjectionCreation, chooseProjection, true, updateMemory);
                    return p;
                }
                catch (NoInputFoundException) { }
            }

            ProjectionTreeNode deepProjection = currentTreeNode.children.Find(nodeAdequationTest);
            if (deepProjection != null)
            {
                if (unusedInputsOnly && !deepProjection.projectedInput.IsAvailable())
                {
                    ProjectionTreeNode alternativeProjection = deepProjectionCreation();
                    chooseProjection(alternativeProjection);
                    return alternativeProjection;
                }
                else
                {
                    chooseProjection(deepProjection);
                    return deepProjection;
                }
            }
            else
            {
                ProjectionTreeNode rootProjection = memoryRoot.children.Find(nodeAdequationTest);
                if ((rootProjection == null) || (unusedInputsOnly && !rootProjection.projectedInput.IsAvailable()))
                {
                    deepProjection = deepProjectionCreation();
                    chooseProjection(deepProjection);
                    if (updateMemory)
                        currentTreeNode.AddChild(deepProjection);
                    return deepProjection;
                }
                else
                {
                    chooseProjection(rootProjection);
                    if (updateMemory)
                        currentTreeNode.AddChild(rootProjection);
                    return rootProjection;
                }
            }

        }

        /// <summary>
        /// Save current state of the memory.
        /// </summary>
        /// <param name="path">path to the file</param>
        public void SaveToFile(string path)
        {
            memoryRoot.SaveToFile(path);
        }

        /// <summary>
        /// Load a state of memory.
        /// </summary>
        /// <param name="path">path to the file</param>
        public void LoadFromFile(string path)
        {
            memoryRoot.LoadFromFile(path);
        }

        public class NoInputFoundException : System.Exception
        {
            public NoInputFoundException() { }
            public NoInputFoundException(string message) : base(message) { }
            public NoInputFoundException(string message, System.Exception inner) : base(message, inner) { }
        }
    }




    [System.Serializable]
    public class ProjectionTreeNode
    {
        /// <summary>
        /// Nodes collection by tree id.
        /// </summary>
        [SerializeField]
        protected static Dictionary<string, Dictionary<string, ProjectionTreeNode>> nodesByTree = new Dictionary<string, Dictionary<string, ProjectionTreeNode>>();

        /// <summary>
        /// Node's children. Please avoid calling this field too often as it is slow to compute.
        /// </summary>
        public List<ProjectionTreeNode> children
        {
            get
            {
                List<ProjectionTreeNode> buffer = new List<ProjectionTreeNode>();
                foreach (string id in childrensId)
                {
                    if (nodesByTree.TryGetValue(treeId, out Dictionary<string, ProjectionTreeNode> nodes))
                        if (nodes.TryGetValue(id, out ProjectionTreeNode child))
                            buffer.Add(child);
                }
                return buffer;
            }
        }


        [SerializeField]
        protected List<string> childrensId = new List<string>();

        /// <summary>
        /// Node id.
        /// </summary>
        [SerializeField]
        public string id;

        public string treeId { get; protected set; }

        /// <summary>
        /// UMI3D Input projected to this node. 
        /// </summary>
        [SerializeField]
        public AbstractUMI3DInput projectedInput;


        public ProjectionTreeNode (string treeId)
        {
            this.treeId = treeId;
        }

        /// <summary>
        /// Add a child to this node.
        /// </summary>
        /// <param name="child">Node to add</param>
        public void AddChild(ProjectionTreeNode child)
        {
            if (childrensId.Contains(child.id))
                return;
            childrensId.Add(child.id);
            if (nodesByTree.TryGetValue(treeId, out Dictionary<string, ProjectionTreeNode> nodes))
            {
                if (!nodes.ContainsKey(child.id))
                {
                    nodes.Add(child.id, child);
                }
            }
        }

        /// <summary>
        /// save a node state to a file.
        /// </summary>
        /// <param name="path">path to the file</param>
        public void SaveToFile(string path)
        {
            if (nodesByTree.TryGetValue(treeId, out Dictionary<string, ProjectionTreeNode> nodes))
            {

                string objectJson = JsonUtility.ToJson(this, true);
                string staticRefIds = JsonUtility.ToJson(nodes.Keys, true);
                string storage = objectJson;// + "@\n" + static_json;

                StreamWriter writer = new StreamWriter(path);
                writer.Write(storage);
                writer.Close();
            }
        }

        /// <summary>
        /// Load A node state from a file
        /// </summary>
        /// <param name="path">path to the file</param>
        public void LoadFromFile(string path)
        {
            if (nodesByTree.TryGetValue(treeId, out Dictionary<string, ProjectionTreeNode> nodes))
            {
                StreamReader reader = new StreamReader(path);
                string storage = reader.ReadToEnd();
                string[] buffer = storage.Split('@');

                string object_json = buffer[0];
                string static_json = buffer[1];

                JsonUtility.FromJsonOverwrite(static_json, nodes);
                JsonUtility.FromJsonOverwrite(object_json, this);
            }
        }

    }

    [System.Serializable]
    public class EventNode : ProjectionTreeNode
    {
        [SerializeField]
        public EventDto evt;

        public EventNode (string treeId) : base(treeId) { }
    }

    [System.Serializable]
    public class ManipulationNode : ProjectionTreeNode
    {
        [SerializeField]
        public ManipulationDto manipulation;

        [SerializeField]
        public DofGroupDto manipulationDofGroupDto;

        public ManipulationNode(string treeId) : base(treeId) { }
    }

    [System.Serializable]
    public class ParameterNode : ProjectionTreeNode
    {
        [SerializeField]
        public AbstractParameterDto parameter;

        public ParameterNode(string treeId) : base(treeId) { }
    }
}