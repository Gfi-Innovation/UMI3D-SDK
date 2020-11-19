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
using umi3d.common;
using umi3d.common.collaboration;
using UnityEngine;

namespace umi3d.cdk.collaboration
{

    /// <summary>
    /// Singleton use to read AudioDto.
    /// </summary>
    public class AudioManager : Singleton<AudioManager>
    {
        Dictionary<string, IAudioReader> GlobalReader = new Dictionary<string, IAudioReader>();
        Dictionary<string, IAudioReader> SpacialReader = new Dictionary<string, IAudioReader>();

        private void Start()
        {
            UMI3DUser.OnNewUser.AddListener(OnAudioChanged);
            UMI3DUser.OnUserAudioUpdated.AddListener(OnAudioChanged);
        }

        /// <summary>
        /// Read an Audio Dto and dispatched it in the right audioSource.
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="channel"></param>
        public void Read(UMI3DUser user, byte[] sample, DataChannel channel)
        {
            if (user != null)
            {
                string id = user.id;
                if (UMI3DDto.FromBson(sample) is AudioDto dto)
                {
                    if (SpacialReader.ContainsKey(id))
                    {
                        SpacialReader[id].Read(dto);
                    }
                    else
                    {
                        if (!GlobalReader.ContainsKey(id))
                        {
                            var g = new GameObject();
                            g.name = id;
                            GlobalReader[id] = g.AddComponent<AudioReader>();
                        }
                        GlobalReader[id].Read(dto);
                    }
                }
            }
        }

        /// <summary>
        /// MAnage user update
        /// </summary>
        /// <param name="user"></param>
        void OnAudioChanged(UMI3DUser user)
        {
            var reader = user.audioplayer;
            if (reader != null)
            {
                SpacialReader[user.id] = reader;
            }
            else
            {
                if (SpacialReader.ContainsKey(user.id))
                    SpacialReader.Remove(user.id);
            }
        }
    }
}