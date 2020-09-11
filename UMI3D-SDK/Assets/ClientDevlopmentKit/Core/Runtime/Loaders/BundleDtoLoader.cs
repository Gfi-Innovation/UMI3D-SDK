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
using umi3d.common;
using UnityEngine;
using UnityEngine.Networking;

namespace umi3d.cdk
{
    /// <summary>
    /// Resource Loader for a bundle
    /// </summary>
    public class BundleDtoLoader : IResourcesLoader
    {

        public List<string> supportedFileExtentions;
        public List<string> ignoredFileExtentions;

        /// <summary>
        /// Constructor.
        /// </summary>
        public BundleDtoLoader()
        {
            supportedFileExtentions = new List<string>() { ".bundle" };
            ignoredFileExtentions = new List<string>();
        }

        /// <see cref="IResourcesLoader.IsSuitableFor"/>
        public bool IsSuitableFor(string extension)
        {
            return supportedFileExtentions.Contains(extension);
        }

        /// <see cref="IResourcesLoader.IsToBeIgnored"/>
        public bool IsToBeIgnored(string extension)
        {
            return ignoredFileExtentions.Contains(extension);
        }

        /// <see cref="IResourcesLoader.UrlToObject"/>
        public virtual void UrlToObject(string url, string extension, string authorization, Action<object> callback, Action<string> failCallback, string pathIfObjectInBundle = "")
        {

            // add bundle in the cache
            UnityWebRequest www = UnityWebRequestAssetBundle.GetAssetBundle(url);

            SetCertificate(www, authorization);
            UMI3DResourcesManager.DownloadObject(www,
                () =>
                {
                    AssetBundle bundle = ((DownloadHandlerAssetBundle)www.downloadHandler).assetBundle;

                    callback.Invoke(bundle);
                },
                s => failCallback.Invoke(s)
            );
        }

        /// <see cref="IResourcesLoader.ObjectFromCache"/>
        public virtual void ObjectFromCache(object o, Action<object> callback, string pathIfObjectInBundle)
        {
            if (pathIfObjectInBundle != null && pathIfObjectInBundle != "")
            {

                if (Array.Exists(((AssetBundle)o).GetAllAssetNames(), element => { return element == pathIfObjectInBundle; }))
                {
                    var objectInBundle = ((AssetBundle)o).LoadAsset(pathIfObjectInBundle);
                    callback.Invoke(objectInBundle);
                }
                else
                {
                    if (Array.Exists(((AssetBundle)o).GetAllScenePaths(), element => { return element == pathIfObjectInBundle; }))
                    {
                        callback.Invoke(pathIfObjectInBundle);
                    }

                    else
                    {
                        Debug.LogWarning("Scene path not found : " + pathIfObjectInBundle);
                        callback.Invoke(o);
                    }

                }
            }
            else
            {

                callback.Invoke(o);
            }
        }

        /// <summary>
        /// set Certificate for webRequest.
        /// </summary>
        /// <param name="www">web request.</param>
        /// <param name="fileAuthorization">Authorization</param>
        public virtual void SetCertificate(UnityWebRequest www, string fileAuthorization)
        {
            if (fileAuthorization != null && fileAuthorization != "")
            {
                string authorization = fileAuthorization;
                authorization = Convert.ToBase64String(System.Text.Encoding.GetEncoding("ISO-8859-1").GetBytes(authorization));
                authorization = "Basic " + authorization;

                www.SetRequestHeader(UMI3DNetworkingKeys.Authorization, authorization);
            }
        }

        /// <see cref="IResourcesLoader.DeleteObject"/>
        public void DeleteObject(object objectLoaded, string reason)
        {
            if (objectLoaded != null) ((AssetBundle)objectLoaded).Unload(true);
        }

    }
}