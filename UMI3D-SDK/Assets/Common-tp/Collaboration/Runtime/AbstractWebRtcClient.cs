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
using System.Collections.Generic;
using System.Linq;
using Unity.WebRTC;
using UnityEngine;
using UnityEngine.Events;

namespace umi3d.common.collaboration
{
    public abstract class AbstractWebRtcClient
    {
        MonoBehaviour behaviour;
        Coroutine WebrtcCoroutine;

        public Dictionary<string, WebRTCconnection> peers;
        public Dictionary<string, List<DataChannel>> peersDataChanelToAdd;

        static bool webrtcRunning = false;
        static bool webrtcUpdating = false;

        public class RtcMessageListener : UnityEvent<string, byte[], DataChannel> { }

        public AbstractWebRtcClient(MonoBehaviour behaviour)
        {
            this.behaviour = behaviour;
            peers = new Dictionary<string, WebRTCconnection>();
            if (!webrtcRunning)
            {
                WebRTC.Initialize();
                webrtcRunning = true;
            }
            if (!webrtcUpdating)
            {
                WebrtcCoroutine = behaviour.StartCoroutine(WebRTC.Update());
                webrtcUpdating = true;
            }
        }

        /// <summary>
        /// Add a dataChannel to all peers
        /// </summary>
        /// <param name="dataBase">DataChannel use to setup new Datachannel.
        /// This dataChannel will never be use by a connection.
        /// </param>
        /// <returns></returns>
        public virtual List<DataChannel> Add(DataChannel dataBase)
        {
            List<DataChannel> channels = new List<DataChannel>();
            int i = 0;
            foreach (var p in peers)
            {
                i++;
                var dc = new DataChannel(dataBase);
                dc.OnCreated += () => OnDataChannelCreated(dc, p.Key);
                channels.Add(dc);
                p.Value.AddDataChannel(dc);
            }
            return channels;
        }

        public virtual DataChannel Add(string uid, DataChannel dataBase)
        {
            var dc = new DataChannel(dataBase);
            dc.OnCreated += () => OnDataChannelCreated(dc, uid);
            peers[uid].AddDataChannel(dc);
            return dc;
        }

        /// <summary>
        /// Remove a DataChannel
        /// </summary>
        public virtual void Remove(DataChannel dataChannel)
        {
        }

        /// <summary>
        /// CallBack invoke when a new DataChannel is created.
        /// </summary>
        /// <param name="dataChannel"></param>
        /// <param name="uid"></param>
        protected virtual void OnDataChannelCreated(DataChannel dataChannel, string uid)
        {
            //Debug.Log($"datachannel created {dataChannel.Label} {dataChannel.reliable} {dataChannel.type}");
            WebSocketSend(
                new RTCDataChannelDto()
                {
                    targetUser = uid,
                    sourceUser = GetUID(),
                    Label = dataChannel.Label,
                    reliable = dataChannel.reliable,
                    type = dataChannel.type,
                },
                uid
            );
        }

        /// <summary>
        /// Handles RtcDto received by the websocket.
        /// </summary>
        /// <param name="dto"></param>
        public virtual void HandleMessage(RTCDto dto)
        {
            if (dto is OfferDto)
            {
                var offer = dto as OfferDto;
                OnRtcOffer(offer);
            }
            else if (dto is AnswerDto)
            {
                var answer = dto as AnswerDto;
                OnRtcAnswer(answer);
            }
            else if (dto is CandidateDto)
            {
                var candidate = dto as CandidateDto;
                OnRtcIceCandidate(candidate);
            }
            else if (dto is LeaveDto)
            {
                if (peers.ContainsKey(dto.sourceUser))
                    peers.Remove(dto.sourceUser);
            }
            else if (dto is RTCConnectionDTO)
            {
                if (!peers.ContainsKey(dto.sourceUser))
                {
                    WebRTCconnection rtc = CreateWebRtcConnection(dto.sourceUser, true);
                    peers.Add(dto.sourceUser, rtc);
                    rtc.Offer();
                }
            }
            else if (dto is RTCCloseConnectionDto)
            {
                if (peers.ContainsKey(dto.sourceUser))
                {
                    peers[dto.sourceUser].Close();
                    peers.Remove(dto.sourceUser);
                }
            }
            else if (dto is RTCDataChannelDto)
            {
                var dcDto = dto as RTCDataChannelDto;
                var otherId = dto.sourceUser == GetUID() ? dto.targetUser : dto.sourceUser;
                if (peers.ContainsKey(otherId))
                {
                    var dc = peers[otherId].channels.Find((dc2) => dcDto.Label == dc2.Label);
                    if (dc != null)
                    {
                        dc.type = dcDto.type;
                        dc.reliable = dcDto.reliable;
                    }
                    else
                    {
                        peers[otherId].AddDataChannel(CreateDataChannel(new DataChannel(dcDto.Label, dcDto.reliable, dcDto.type), otherId));
                    }
                }
                else
                {
                    if (peersDataChanelToAdd == null) peersDataChanelToAdd = new Dictionary<string, List<DataChannel>>();
                    if (!peersDataChanelToAdd.ContainsKey(otherId)) { peersDataChanelToAdd[otherId] = new List<DataChannel>(); }
                    peersDataChanelToAdd[otherId].Add(CreateDataChannel(new DataChannel(dcDto.Label, dcDto.reliable, dcDto.type), otherId));
                }
            }
            else
            {
                Debug.LogError("other :" + dto);
            }
        }

        /// <summary>
        /// Handles Offer and create Answer
        /// </summary>
        /// <param name="offer"></param>
        protected virtual void OnRtcOffer(OfferDto offer)
        {
            if (!peers.ContainsKey(offer.sourceUser))
                peers.Add(offer.sourceUser, CreateWebRtcConnection(offer.sourceUser));

            RTCSessionDescription description = new RTCSessionDescription();
            description.sdp = offer.sdp;
            description.type = RTCSdpType.Offer;
            UnityMainThreadDispatcher.Instance().Enqueue(peers[offer.sourceUser].CreateAnswer(description));
        }

        /// <summary>
        /// Handles Answers and set remote session
        /// </summary>
        /// <param name="answer"></param>
        protected virtual void OnRtcAnswer(AnswerDto answer)
        {
            if (peers.ContainsKey(answer.sourceUser))
                peers[answer.sourceUser].SetRemoteSession(answer.sdp);
            else
                throw new ArgumentException("Received answer from unknown peer.");
        }

        /// <summary>
        /// Handles Candidate
        /// </summary>
        /// <param name="c"></param>
        protected virtual void OnRtcIceCandidate(CandidateDto c)
        {
            if (!peers.ContainsKey(c.sourceUser))
                peers.Add(c.sourceUser, CreateWebRtcConnection(c.sourceUser));
            RTCIceCandidate candidate = new RTCIceCandidate();
            candidate.candidate = c.candidate;
            candidate.sdpMid = c.sdpMid;
            candidate.sdpMLineIndex = c.sdpMLineIndex;
            peers[c.sourceUser].AddIceCandidate(candidate);
        }

        /// <summary>
        /// Used only for debug purpose.
        /// </summary>
        /// <returns></returns>
        protected abstract string GetLogPrefix();

        /// <summary>
        /// Create and setup WebrtcConnection
        /// </summary>
        /// <param name="uid"></param>
        /// <returns></returns>
        protected WebRTCconnection CreateWebRtcConnection(string uid, bool instanciateChannel = false)
        {
            WebRTCconnection connection = new WebRTCconnection();
            connection.onIceCandidate += arg => OnIceCandidate(arg, uid);
            connection.onAnswerCreated += arg => OnRtcAnswer(arg, uid);
            connection.onOfferCreated += arg => OnRtcOffer(arg, uid);
            connection.onMessage += (bytes, channel) => OnRtcMessage(uid, bytes, channel);
            connection.onDataChannelOpen += OnRtcDataChannelOpen;
            connection.onDataChannelClose += OnRtcDataChannelClose;
            connection.logPrefix = GetLogPrefix();
            connection.channels = new List<DataChannel>();
            ChannelsToAddCreation(uid, connection);
            connection.Init(uid, instanciateChannel);
            return connection;
        }

        /// <summary>
        /// Add default datachannel to a connection
        /// </summary>
        /// <param name="uid">peer id</param>
        /// <param name="connection"></param>
        protected virtual void ChannelsToAddCreation(string uid, WebRTCconnection connection)
        {
            foreach (var channel in WebRtcChannels.defaultPeerToPeerChannels)
                if (!connection.channels.Any(c => c.Label == channel.Label))
                    connection.channels.Add(CreateDataChannel(channel, uid));
            List<DataChannel> otherChannels = null;
            if (peersDataChanelToAdd != null && peersDataChanelToAdd.ContainsKey(uid))
            {
                otherChannels = peersDataChanelToAdd[uid];
                peersDataChanelToAdd.Remove(uid);
            }
            if (otherChannels != null)
                foreach (var channel in otherChannels)
                    if (!connection.channels.Any(c => c.Label == channel.Label))
                        connection.channels.Add(CreateDataChannel(channel, uid));

        }

        /// <summary>
        /// Create a dataChannel based on an other dataChannel
        /// </summary>
        /// <param name="Base"></param>
        /// <returns></returns>
        protected virtual DataChannel CreateDataChannel(DataChannel Base, string uid)
        {
            DataChannel dc = new DataChannel(Base);
            dc.OnCreated += () => OnDataChannelCreated(dc, uid);
            dc.OnOpen += () => UnityMainThreadDispatcher.Instance().Enqueue(SendStack(dc));
            return dc;
        }

        System.Collections.IEnumerator SendStack(DataChannel dataChannel)
        {
            yield return new WaitForFixedUpdate();
            dataChannel.SendStack();
        }

        /// <summary>
        /// Should return self UID
        /// </summary>
        /// <returns></returns>
        protected abstract string GetUID();

        /// <summary>
        /// Handle a new received Message
        /// </summary>
        /// <param name="id">peer uid</param>
        /// <param name="bytes">bytes message</param>
        /// <param name="channel">DataChannel used by this message</param>
        protected abstract void OnRtcMessage(string id, byte[] bytes, DataChannel channel);

        void OnIceCandidate(RTCIceCandidate c, string uid)
        {
            CandidateDto msg = new CandidateDto()
            {
                candidate = c.candidate,
                sdpMid = c.sdpMid,
                sdpMLineIndex = c.sdpMLineIndex,
                targetUser = uid,
                sourceUser = GetUID(),
            };
            WebSocketSend(msg, uid);
        }

        void OnRtcAnswer(string sdp, string uid)
        {
            AnswerDto msg = new AnswerDto()
            {
                sdp = sdp,
                targetUser = uid,
                sourceUser = GetUID(),
            };
            WebSocketSend(msg, uid);
        }

        void OnRtcOffer(string sdp, string uid)
        {
            OfferDto msg = new OfferDto()
            {
                sdp = sdp,
                targetUser = uid,
                sourceUser = GetUID(),
            };
            WebSocketSend(msg, uid);
        }

        protected abstract void OnRtcDataChannelOpen(DataChannel channel);

        protected abstract void OnRtcDataChannelClose(DataChannel channel);

        protected abstract void WebSocketSend(RTCDto dto, string targetId);

        /// <summary>
        /// Send a Message to all peers
        /// </summary>
        /// <param name="dto"></param>
        /// <param name="reliable"></param>
        public virtual void Send(UMI3DDto dto, bool reliable, string peerId = null)
        {
            if (peerId == null)
                foreach (var connection in peers.Values)
                    connection.Send(dto.ToBson(), reliable);
            else if (peers.ContainsKey(peerId))
            {
                peers[peerId].Send(dto.ToBson(), reliable);
            }
            else
                throw new Exception($"peer not found {peerId}");
        }

        /// <summary>
        /// Send a Message to all peers using a dataChannel
        /// </summary>
        /// <param name="dto"></param>
        /// <param name="reliable"></param>
        public virtual void Send(UMI3DDto dto, bool reliable, DataType dataType, string peerId = null)
        {
            if (peerId == null)
                foreach (var connection in peers.Values)
                    connection.Send(dto.ToBson(), reliable, dataType);
            else if (peers.ContainsKey(peerId))
                peers[peerId].Send(dto.ToBson(), reliable, dataType);
        }

        /// <summary>
        /// Send a Message to all peers
        /// </summary>
        /// <param name="message"></param>
        /// <param name="reliable"></param>
        public virtual void Send(string message, bool reliable, string peerId = null)
        {
            if (peerId == null)
                foreach (var connection in peers.Values)
                    connection.Send(message, reliable);
            else if (peers.ContainsKey(peerId))
                peers[peerId].Send(message, reliable);
        }

        /// <summary>
        /// Send Message to a list of dataChannel
        /// </summary>
        /// <param name="message"></param>
        /// <param name="dataChannels"></param>
        public virtual void Send(string message, List<DataChannel> dataChannels)
        {
            foreach (var dataChannel in dataChannels)
                dataChannel.dataChannel.Send(message);
        }

        /// <summary>
        /// Send Message to a list of dataChannel
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="dataChannels"></param>
        public virtual void Send(byte[] bytes, List<DataChannel> dataChannels)
        {
            foreach (var dataChannel in dataChannels)
                dataChannel.dataChannel.Send(bytes);
        }

        /// <summary>
        /// Stop The webrtc client.
        /// </summary>
        public void Stop()
        {
            foreach (var peer in peers)
                peer.Value.Close();
            if (webrtcUpdating)
            {
                if (WebrtcCoroutine != null)
                    behaviour.StopCoroutine(WebrtcCoroutine);
                webrtcUpdating = false;
            }


        }

        /// <summary>
        /// Use only on application quit. Restarting webrtc after that will crash.
        /// </summary>
        public void Clear()
        {
            Stop();
            if (webrtcRunning)
            {
                WebRTC.Dispose();
                webrtcRunning = false;
            }
        }
    }
}