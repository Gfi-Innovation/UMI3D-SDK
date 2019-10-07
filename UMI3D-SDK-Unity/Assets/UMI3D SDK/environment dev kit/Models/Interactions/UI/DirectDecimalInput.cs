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
using umi3d.common;
using UnityEngine;
using UnityEngine.Events;

namespace umi3d.edk
{
    /// <summary>
    /// Decimal UI Input.
    /// </summary>
    public class DirectDecimalInput : DirectInput
    {
        /// <summary>
        /// Default value.
        /// </summary>
        public float m_default = 0f;

        /// <summary>
        /// Is this input a range.
        /// </summary>
        public bool is_range = false;

        /// <summary>
        /// Maximum available value.
        /// </summary>
        public float max = 1f;

        /// <summary>
        /// Minimum available value.
        /// </summary>
        public float min = 0f;

        public class OnChangeListener : UnityEvent<UMI3DUser, float> { }

        /// <summary>
        /// Event raised on value change.
        /// </summary>
        public OnChangeListener onChange = new OnChangeListener();

        /// <summary>
        /// automatically check if the object has been updated in the editor
        /// </summary>
        protected override void checkForUpdates()
        {
            base.checkForUpdates();
            bool inputUpdated = input.DefaultValue == null ||
                m_default != float.Parse(input.DefaultValue);
            if (is_range)
            {
                inputUpdated = inputUpdated ||
                input.Max == null ||
                max != float.Parse(input.Max) ||
                input.Min == null ||
                min != float.Parse(input.Min);
            }
            else
            {
                inputUpdated = inputUpdated ||
                input.Max != null ||
                input.Min != null;
            }
            if(inputUpdated)
                PropertiesHandler.NotifyUpdate();
        }

        /// <summary>
        /// Update input properties.
        /// </summary>
        protected override void syncProperties()
        {
            base.syncProperties();
            input.inputType = InputType.Decimal;
            input.DefaultValue = m_default.ToString();
            if (is_range)
            {
                input.Max = max.ToString();
                input.Min = min.ToString();
            }
            else
            {
                input.Max = null;
                input.Min = null;
            }
        }


        /// <summary>
        /// Called by a user on interaction.
        /// </summary>
        /// <param name="user">User interacting</param>
        /// <param name="evt">Interaction data</param>
        public override void OnUserInteraction(UMI3DUser user, JSONObject evt)
        {
            if (evt.IsNumber)
            {
                var value = evt.f;
                if (is_range)
                    value = Mathf.Clamp(value, min, max);
                m_default = value;
                onChange.Invoke(user, m_default);
            }
        }

    }
}
