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
#if UNITY_EDITOR
using System;
using umi3d.common;
using umi3d.edk;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace umi3d.edk.editor
{
    [CustomEditor(typeof(UMI3DScene))]
    [CanEditMultipleObjects]
    public class UMI3DSceneEditor : Editor
    {

        SerializedProperty Name;
        SerializedProperty Type;
        SerializedProperty Navigation;
        SerializedProperty AmbientType;
        SerializedProperty AmbientColor;
        SerializedProperty SkyColor;
        SerializedProperty HorizonColor;
        SerializedProperty GroundColor;
        SerializedProperty AmbientIntensity;
        SerializedProperty Icon;
        SerializedProperty Skybox;


        // Start is called before the first frame update
        public void OnEnable()
        {
            Name = serializedObject.FindProperty("_name");
            Type = serializedObject.FindProperty("_type");
            Navigation = serializedObject.FindProperty("_navigation");
            AmbientType = serializedObject.FindProperty("AmbientType");
            AmbientColor = serializedObject.FindProperty("AmbientColor");
            SkyColor = serializedObject.FindProperty("SkyColor");
            HorizonColor = serializedObject.FindProperty("HorizonColor");
            GroundColor = serializedObject.FindProperty("GroundColor");
            AmbientIntensity = serializedObject.FindProperty("AmbientIntensity");
            Icon = serializedObject.FindProperty("icon");
            Skybox = serializedObject.FindProperty("skybox");
        }

        // Update is called once per frame
        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(Name);
            EditorGUILayout.PropertyField(Type);
            EditorGUILayout.PropertyField(Navigation);
            EditorGUILayout.PropertyField(AmbientType);

            int vType = AmbientType.enumValueIndex;
            if(vType == (int)common.AmbientType.Skybox)
            {
                EditorGUILayout.PropertyField(AmbientIntensity);
            }
            else if (vType == (int)common.AmbientType.Flat)
            {
                EditorGUILayout.PropertyField(AmbientColor);
            }
            else if (vType == (int)common.AmbientType.Gradient)
            {
                EditorGUILayout.PropertyField(SkyColor);
                EditorGUILayout.PropertyField(HorizonColor);
                EditorGUILayout.PropertyField(GroundColor);
            }

            EditorGUILayout.PropertyField(Icon);
            EditorGUILayout.PropertyField(Skybox);

            if (EditorGUI.EndChangeCheck())
            {
                //UpdateLight();
                serializedObject.ApplyModifiedProperties();
            }
        }
    }
}
#endif