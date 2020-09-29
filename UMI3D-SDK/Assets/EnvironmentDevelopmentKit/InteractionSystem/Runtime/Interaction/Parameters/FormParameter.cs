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
using System.Linq;
using umi3d.common.interaction;
using UnityEngine;
using UnityEngine.Events;

namespace umi3d.edk.interaction
{
    public class FormParameter : AbstractInteraction
    {
        public List<AbstractParameter> Fields = new List<AbstractParameter>();

        [System.Serializable]
        public class FormListener : UnityEvent<UMI3DUser, FormDto> { }


        /// <summary>
        /// Event raised on value change.
        /// </summary>
        public FormListener onFormCompleted = new FormListener();

        protected override AbstractInteractionDto CreateDto()
        {
            return new FormDto();
        }

        protected override void WriteProperties(AbstractInteractionDto dto_, UMI3DUser user)
        {
            base.WriteProperties(dto_, user);
            var dto = (dto_ as FormDto);
            if (dto == null)
                return;
            dto.Fields = Fields.Select(f => f.ToDto(user) as AbstractParameterDto).Where(f => f != null).ToList();
        }

        public override void OnUserInteraction(UMI3DUser user, InteractionRequestDto interactionRequest)
        {
            switch (interactionRequest)
            {
                case FormAnswer formAnswer:
                    onFormCompleted.Invoke(user, formAnswer.form);
                    break;
                default:
                    throw new System.Exception("User interaction not supported (ParameterSettingRequestDto) ");
            }
        }
    }
}