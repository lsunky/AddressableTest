using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using UnityEngine.Networking;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.Util;
using UnityEngine.Serialization;

namespace UnityEngine.ResourceManagement.ResourceProviders
{
    /// <summary>
    /// Wrapper for asset bundles.
    /// </summary>
    public interface IAssetBundleResource
    {
        /// <summary>
        /// Retrieves the asset bundle.
        /// </summary>
        /// <returns>Returns the asset bundle.</returns>
        AssetBundle GetAssetBundle();
    }

    /// <summary>
    /// Contains cache information to be used by the AssetBundleProvider
    /// </summary>
    [Serializable]
    public class AssetBundleRequestOptions : ILocationSizeData
    {
        [FormerlySerializedAs("m_hash")]
        [SerializeField]
        string m_Hash = "";
        /// <summary>
        /// Hash value of the asset bundle.
        /// </summary>
        public string Hash { get { return m_Hash; } set { m_Hash = value; } }
        [FormerlySerializedAs("m_crc")]
        [SerializeField]
        uint m_Crc;
        /// <summary>
        /// CRC value of the bundle.
        /// </summary>
        public uint Crc { get { return m_Crc; } set { m_Crc = value; } }
        [FormerlySerializedAs("m_timeout")]
        [SerializeField]
        int m_Timeout;
        /// <summary>
        /// Sets UnityWebRequest to attempt to abort after the number of seconds in timeout have passed.
        /// </summary>
        public int Timeout { get { return m_Timeout; } set { m_Timeout = value; } }
        [FormerlySerializedAs("m_chunkedTransfer")]
        [SerializeField]
        bool m_ChunkedTransfer;
        /// <summary>
        /// Indicates whether the UnityWebRequest system should employ the HTTP/1.1 chunked-transfer encoding method.
        /// </summary>
        public bool ChunkedTransfer { get { return m_ChunkedTransfer; } set { m_ChunkedTransfer = value; } }
        [FormerlySerializedAs("m_redirectLimit")]
        [SerializeField]
        int m_RedirectLimit = -1;
        /// <summary>
        /// Indicates the number of redirects which this UnityWebRequest will follow before halting with a “Redirect Limit Exceeded” system error.
        /// </summary>
        public int RedirectLimit { get { return m_RedirectLimit; } set { m_RedirectLimit = value; } }
        [FormerlySerializedAs("m_retryCount")]
        [SerializeField]
        int m_RetryCount;
        /// <summary>
        /// Indicates the number of times the request will be retried.
        /// </summary>
        public int RetryCount { get { return m_RetryCount; } set { m_RetryCount = value; } }

        [SerializeField]
        string m_BundleName = null;
        /// <summary>
        /// The name of the original bundle.  This does not contain the appended hash.
        /// </summary>
        public string BundleName { get { return m_BundleName; } set { m_BundleName = value; } }

        [SerializeField]
        long m_BundleSize;
        /// <summary>
        /// The size of the bundle, in bytes.
        /// </summary>
        public long BundleSize { get { return m_BundleSize; } set { m_BundleSize = value; } }

        [SerializeField]
        bool m_UseCrcForCachedBundles;
        /// <summary>
        /// If false, the CRC will not be used when loading bundles from the cache.
        /// </summary>
        public bool UseCrcForCachedBundle { get { return m_UseCrcForCachedBundles; } set { m_UseCrcForCachedBundles = value; } }

        [SerializeField]
        bool m_ClearOtherCachedVersionsWhenLoaded;
        /// <summary>
        /// If false, the CRC will not be used when loading bundles from the cache.
        /// </summary>
        public bool ClearOtherCachedVersionsWhenLoaded { get { return m_ClearOtherCachedVersionsWhenLoaded; } set { m_ClearOtherCachedVersionsWhenLoaded = value; } }

        /// <summary>
        /// Computes the amount of data needed to be downloaded for this bundle.
        /// </summary>
        /// <param name="location">The location of the bundle.</param>
        /// <param name="resourceManager">The object that contains all the resource locations.</param>
        /// <returns>The size in bytes of the bundle that is needed to be downloaded.  If the local cache contains the bundle or it is a local bundle, 0 will be returned.</returns>
        public virtual long ComputeSize(IResourceLocation location, ResourceManager resourceManager)
        {
            var id = resourceManager == null ? location.InternalId : resourceManager.TransformInternalId(location);
            if (!ResourceManagerConfig.IsPathRemote(id))
                return 0;
            var locHash = Hash128.Parse(Hash);
#if ENABLE_CACHING
            if (locHash.isValid) //If we have a hash, ensure that our desired version is cached.
            {
                if (Caching.IsVersionCached(new CachedAssetBundle(BundleName, locHash)))
                    return 0;
                return BundleSize;
            }
            else //If we don't have a hash, any cached version will do.
            {
                List<Hash128> versions = new List<Hash128>();
                Caching.GetCachedVersions(BundleName, versions);
                if (versions.Count > 0)
                    return 0;
            }
#endif //ENABLE_CACHING
            return BundleSize;
        }
    }

    class AssetBundleResource : IAssetBundleResource
    {
        AssetBundle m_AssetBundle;
        DownloadHandler m_downloadHandler;
        AsyncOperation m_RequestOperation;
        WebRequestQueueOperation m_WebRequestQueueOperation;
        internal ProvideHandle m_ProvideHandle;
        internal AssetBundleRequestOptions m_Options;
        IDataConverter m_dataProc;
        int m_Retries;
        long m_BytesToDownload;

        String m_InternalId;
        public static String GetEncryptedCachePath() {
            return Path.Combine(Application.persistentDataPath, "eb");
        }
        private String GetEncryptedAssetLocalPath(String internalId) {
            return Path.Combine(GetEncryptedCachePath(), internalId.GetHashCode().ToString() + "." + m_Options == null ? "000" : m_Options.Hash);
        }

        internal UnityWebRequest CreateWebRequest(IResourceLocation loc)
        {
            var url = m_ProvideHandle.ResourceManager.TransformInternalId(loc);
            UnityWebRequest webRequest = null;

            if (m_dataProc == null) {
                if (m_Options == null)
                    return UnityWebRequestAssetBundle.GetAssetBundle(url);
                
                if (!string.IsNullOrEmpty(m_Options.Hash))
                {
                    CachedAssetBundle cachedBundle = new CachedAssetBundle(m_Options.BundleName, Hash128.Parse(m_Options.Hash));
#if ENABLE_CACHING
                    if (m_Options.UseCrcForCachedBundle || !Caching.IsVersionCached(cachedBundle))
                        webRequest = UnityWebRequestAssetBundle.GetAssetBundle(url, cachedBundle, m_Options.Crc);
                    else
                        webRequest = UnityWebRequestAssetBundle.GetAssetBundle(url, cachedBundle);
#else
                    webRequest = UnityWebRequestAssetBundle.GetAssetBundle(url, cachedBundle, m_Options.Crc);
#endif
                }
                else
                    webRequest = UnityWebRequestAssetBundle.GetAssetBundle(url, m_Options.Crc);
            } else {
                webRequest = new UnityWebRequest(loc.InternalId);
                DownloadHandlerBuffer dH = new DownloadHandlerBuffer();
                webRequest.downloadHandler = dH;
            }

            if (webRequest == null)
                return webRequest;

            if (m_Options != null) {
                if (m_Options.Timeout > 0)
                    webRequest.timeout = m_Options.Timeout;
                if (m_Options.RedirectLimit > 0)
                    webRequest.redirectLimit = m_Options.RedirectLimit;
#if !UNITY_2019_3_OR_NEWER
                webRequest.chunkedTransfer = m_Options.ChunkedTransfer;
#endif
            }
            if (m_ProvideHandle.ResourceManager.CertificateHandlerInstance != null)
            {
                webRequest.certificateHandler = m_ProvideHandle.ResourceManager.CertificateHandlerInstance;
                webRequest.disposeCertificateHandlerOnDispose = false;
            }
            return webRequest;
        }

        float PercentComplete() { return m_RequestOperation != null ? m_RequestOperation.progress : 0.0f; }

        DownloadStatus GetDownloadStatus()
        {
            if (m_Options == null)
                return default;
            var status = new DownloadStatus() { TotalBytes = m_BytesToDownload, IsDone = PercentComplete() >= 1f };
            if (m_BytesToDownload > 0)
            {
                if (m_WebRequestQueueOperation != null)
                    status.DownloadedBytes = (long)(m_WebRequestQueueOperation.m_WebRequest.downloadedBytes);
                else if (PercentComplete() >= 1.0f)
                    status.DownloadedBytes = status.TotalBytes;
            }
            return status;
        }

        /// <summary>
        /// Get the asset bundle object managed by this resource.  This call may force the bundle to load if not already loaded.
        /// </summary>
        /// <returns>The asset bundle.</returns>
        public AssetBundle GetAssetBundle()
        {
            if (m_AssetBundle == null && m_downloadHandler != null)
            {
                if (m_dataProc == null)
                {
                    m_AssetBundle = (m_downloadHandler as DownloadHandlerAssetBundle).assetBundle;
                }
                else
                {
                    var crc = m_Options == null ? 0 : m_Options.Crc;
                    var inputStream = new MemoryStream(m_downloadHandler.data, false);
                    String filePath = GetEncryptedAssetLocalPath(m_InternalId);
                    saveDownloadBundle(inputStream, filePath);
                    inputStream.Seek(0, SeekOrigin.Begin);
                    var dataStream = m_dataProc.CreateReadStream(inputStream, m_InternalId);
                    if (dataStream.CanSeek)
                    {
                        m_AssetBundle = AssetBundle.LoadFromStream(dataStream, crc);
                    }
                    else
                    {
                        //Slow path needed if stream is not seekable
                        var memStream = new MemoryStream();
                        dataStream.CopyTo(memStream);
                        dataStream.Flush();
                        dataStream.Dispose();
                        inputStream.Dispose();
                        inputStream.Position = 0;
                        m_AssetBundle = AssetBundle.LoadFromStream(memStream, crc);
                    }
                }
                m_downloadHandler.Dispose();
                m_downloadHandler = null;
            }
            return m_AssetBundle;
        }

        void CopyStream(Stream input, Stream output)
        {
            byte[] buffer = new byte[8 * 1024];
            int len;
            while ( (len = input.Read(buffer, 0, buffer.Length)) > 0)
            {
                output.Write(buffer, 0, len);
            }    
        }

        void saveDownloadBundle(Stream stream, string path)
        {
            //Create the Directory if it does not exist
            if (!Directory.Exists(Path.GetDirectoryName(path)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
            }

            try
            {
                using (Stream file = File.Create(path))
                {
                    CopyStream(stream, file);
                    file.Flush();
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("Failed To Save Data to: " + path.Replace("/", "\\"));
                Debug.LogWarning("Error: " + e.Message);
            }
        }

        internal void Start(ProvideHandle provideHandle, IDataConverter dataProc)
        {
            m_dataProc = dataProc;
            m_Retries = 0;
            m_AssetBundle = null;
            m_downloadHandler = null;
            m_RequestOperation = null;
            m_ProvideHandle = provideHandle;
            m_Options = m_ProvideHandle.Location.Data as AssetBundleRequestOptions;
            if (m_Options != null)
                m_BytesToDownload = m_Options.ComputeSize(m_ProvideHandle.Location, m_ProvideHandle.ResourceManager);
            provideHandle.SetProgressCallback(PercentComplete);
            provideHandle.SetDownloadProgressCallbacks(GetDownloadStatus);
            BeginOperation();
        }

        private void LoadWithDataProc(String path, uint crc) {
            var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read);
            var dataStream = m_dataProc.CreateReadStream(fileStream, m_ProvideHandle.Location.InternalId);
            if (dataStream.CanSeek)
            {
                m_RequestOperation = AssetBundle.LoadFromStreamAsync(dataStream, crc);
            }
            else
            {
                //Slow path needed if stream is not seekable
                var memStream = new MemoryStream();
                dataStream.CopyTo(memStream);
                dataStream.Flush();
                dataStream.Dispose();
                fileStream.Dispose();

                memStream.Position = 0;
                m_RequestOperation = AssetBundle.LoadFromStreamAsync(memStream, crc);
            }
        }

        private void BeginOperation()
        {
            string path = m_ProvideHandle.Location.InternalId;
            if (File.Exists(path) || (Application.platform == RuntimePlatform.Android && path.StartsWith("jar:")))
            {
                var crc = m_Options == null ? 0 : m_Options.Crc;
                if (m_dataProc == null)
                {
                    m_RequestOperation = AssetBundle.LoadFromFileAsync(path, crc);
                }
                else
                {
                    LoadWithDataProc(path, crc);
                }
                m_RequestOperation.completed += LocalRequestOperationCompleted;
            }
            // encrypted and has cache
            else if (m_dataProc != null && File.Exists(GetEncryptedAssetLocalPath(path)))
            {
                var crc = m_Options == null ? 0 : m_Options.Crc;
                LoadWithDataProc(GetEncryptedAssetLocalPath(path), crc);
                m_RequestOperation.completed += LocalRequestOperationCompleted;
            }
            else if (ResourceManagerConfig.ShouldPathUseWebRequest(path))
            {
                var req = CreateWebRequest(m_ProvideHandle.Location);
                req.disposeDownloadHandlerOnDispose = false;
                m_WebRequestQueueOperation = WebRequestQueue.QueueRequest(req);
                if (m_WebRequestQueueOperation.IsDone)
                {
                    m_RequestOperation = m_WebRequestQueueOperation.Result;
                    m_RequestOperation.completed += WebRequestOperationCompleted;
                }
                else
                {
                    m_WebRequestQueueOperation.OnComplete += asyncOp =>
                    {
                        m_RequestOperation = asyncOp;
                        m_RequestOperation.completed += WebRequestOperationCompleted;
                    };
                }
            }
            else
            {
                m_RequestOperation = null;
                m_ProvideHandle.Complete<AssetBundleResource>(null, false, new Exception(string.Format("Invalid path in AssetBundleProvider: '{0}'.", path)));
            }
        }

        private void LocalRequestOperationCompleted(AsyncOperation op)
        {
            m_AssetBundle = (op as AssetBundleCreateRequest).assetBundle;
            m_ProvideHandle.Complete(this, m_AssetBundle != null, null);
        }

        private void WebRequestOperationCompleted(AsyncOperation op)
        {
            UnityWebRequestAsyncOperation remoteReq = op as UnityWebRequestAsyncOperation;
            var webReq = remoteReq.webRequest;
            if (string.IsNullOrEmpty(webReq.error))
            {
                m_downloadHandler = webReq.downloadHandler;

                if (m_dataProc != null) {
                    // encrypt bundle, need to cache InternalId before complete
                    m_InternalId = m_ProvideHandle.Location.InternalId;
                }
#if ENABLE_CACHING
                if (!string.IsNullOrEmpty(m_Options.Hash) && m_Options.ClearOtherCachedVersionsWhenLoaded)
                    Caching.ClearOtherCachedVersions(m_Options.BundleName, Hash128.Parse(m_Options.Hash));
#endif
                m_ProvideHandle.Complete(this, true, null);
            }
            else
            {
                m_downloadHandler = webReq.downloadHandler;
                m_downloadHandler.Dispose();
                m_downloadHandler = null;
                bool forcedRetry = false;
                string message = string.Format("Web request {0} failed with error '{1}', retrying ({2}/{3})...", webReq.url, webReq.error, m_Retries, m_Options.RetryCount);
#if ENABLE_CACHING
                if (!string.IsNullOrEmpty(m_Options.Hash))
                {
                    CachedAssetBundle cab = new CachedAssetBundle(m_Options.BundleName, Hash128.Parse(m_Options.Hash));
                    if (Caching.IsVersionCached(cab))
                    {
                        message = string.Format("Web request {0} failed to load from cache with error '{1}'. The cached AssetBundle will be cleared from the cache and re-downloaded. Retrying...", webReq.url, webReq.error);
                        Caching.ClearCachedVersion(cab.name, cab.hash);
                        if (m_Options.RetryCount == 0 && m_Retries == 0)
                        {
                            Debug.LogFormat(message);
                            BeginOperation();
                            m_Retries++; //Will prevent us from entering an infinite loop of retrying if retry count is 0
                            forcedRetry = true;
                        }
                    }
                }
#endif
                if (!forcedRetry)
                {
                    if (m_Retries < m_Options.RetryCount)
                    {
                        Debug.LogFormat(message);
                        BeginOperation();
                        m_Retries++;
                    }
                    else
                    {
                        var exception = new Exception(string.Format(
                            "RemoteAssetBundleProvider unable to load from url {0}, result='{1}'.", webReq.url,
                            webReq.error));
                        m_ProvideHandle.Complete<AssetBundleResource>(null, false, exception);
                    }
                }
            }
            webReq.Dispose();
        }

        /// <summary>
        /// Unloads all resources associated with this asset bundle.
        /// </summary>
        public void Unload()
        {
            if (m_AssetBundle != null)
            {
                m_AssetBundle.Unload(true);
                m_AssetBundle = null;
            }
            if (m_downloadHandler != null)
            {
                m_downloadHandler.Dispose();
                m_downloadHandler = null;
            }
            m_RequestOperation = null;
        }
    }

    /// <summary>
    /// IResourceProvider for asset bundles.  Loads bundles via UnityWebRequestAssetBundle API if the internalId starts with "http".  If not, it will load the bundle via AssetBundle.LoadFromFileAsync.
    /// </summary>
    [DisplayName("AssetBundle Provider")]
    public class AssetBundleProvider : ResourceProviderBase
    {
        public IDataConverter DataStreamProcessor {get;set;}
        /// <inheritdoc/>
        public override void Provide(ProvideHandle providerInterface)
        {
            new AssetBundleResource().Start(providerInterface, DataStreamProcessor);
        }

        /// <inheritdoc/>
        public override Type GetDefaultType(IResourceLocation location)
        {
            return typeof(IAssetBundleResource);
        }

        public override bool Initialize(string id, string data)
        {
            if (!base.Initialize(id, data))
                return false;

            if (!string.IsNullOrEmpty(data))
            {
                var dsType = JsonUtility.FromJson<SerializedType>(data);
                if (dsType.Value != null)
                    DataStreamProcessor = Activator.CreateInstance(dsType.Value) as IDataConverter;
            }

            return true;
        }

        /// <summary>
        /// Releases the asset bundle via AssetBundle.Unload(true).
        /// </summary>
        /// <param name="location">The location of the asset to release</param>
        /// <param name="asset">The asset in question</param>
        public override void Release(IResourceLocation location, object asset)
        {
            if (location == null)
                throw new ArgumentNullException("location");
            if (asset == null)
            {
                Debug.LogWarningFormat("Releasing null asset bundle from location {0}.  This is an indication that the bundle failed to load.", location);
                return;
            }
            var bundle = asset as AssetBundleResource;
            if (bundle != null)
            {
                bundle.Unload();
                return;
            }
        }
    }
}
