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

using MainThreadDispatcher;
using System;
using System.Collections;
using System.Threading;
using umi3d.common;
using umi3d.common.collaboration;
using UnityEngine;
using WebSocketSharp;
using WebSocketSharp.Server;


namespace umi3d.edk.collaboration
{

    public class UMI3DWebSocketConnection : WebSocketBehavior
    {
        public string _id = null;
        private static int _number = 0;
        private string _prefix;

        public UMI3DWebSocketConnection()
            : this(null)
        {
        }

        public UMI3DWebSocketConnection(string prefix)
        {
            _prefix = !prefix.IsNullOrEmpty() ? prefix : "connection_";
        }

        private string genId()
        {
            var id = Context.QueryString["id"];
            return !id.IsNullOrEmpty() ? id : _prefix + getNumber();
        }

        private static int getNumber()
        {
            return Interlocked.Increment(ref _number);
        }

        //on user quit
        protected override void OnClose(CloseEventArgs e)
        {
            Debug.Log($"onClose {_id}");
            UnityMainThreadDispatcher.Instance().Enqueue(UMI3DCollaborationServer.Collaboration.ConnectionClose(_id));
        }

        //on user send message
        protected override void OnMessage(MessageEventArgs e)
        {
            UnityMainThreadDispatcher.Instance().Enqueue(_OnMessage(e));
        }

        IEnumerator _OnMessage(MessageEventArgs e)
        {
            var res = UMI3DDto.FromBson(e.RawData);
            if (res is IdentityDto)
            {
                var req = res as IdentityDto;
                _id = req.userId;
                if (_id == null || _id == "") _id = genId();
                UMI3DCollaborationServer.Collaboration.CreateUser(req.login, this, onUserCreated);
            }
            if (_id != null)
            {
                if (res is StatusDto)
                {
                    var req = res as StatusDto;
                    Debug.Log(req.status);
                    UMI3DCollaborationServer.Collaboration.OnStatusUpdate(_id, req.status);
                }
                else if (res is RTCDto)
                {
                    UMI3DCollaborationServer.Instance.WebRtcMessage(_id, res as RTCDto);
                }
            }
            yield break;
        }


        void onUserCreated(UMI3DCollaborationUser user,bool reconnection)
        {
            _id = user.Id();
            SendData(user.ToStatusDto());
        }


        //on user connect
        protected override void OnOpen()
        {
            Debug.Log("open");
        }

        public void SendData(UMI3DDto obj,Action<bool> callback = null)
        {
            if (obj != null && this.Context.WebSocket.IsConnected)
            {
                var data = obj.ToBson();
                try
                {
                    if(callback == null) callback = (b) => { };
                    SendAsync(data, callback);
                }
                catch (InvalidOperationException exp)
                {
                    Debug.LogWarning(exp);
                    // todo UnityMainThreadDispatcher.Instance().Enqueue(UMI3D.UserManager.OnRealtimeConnectionClose(_id));
                    return;
                }
            }
        }

        public string GetId()
        {
            return _id;
        }

    }
}