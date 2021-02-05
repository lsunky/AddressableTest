using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;
public class StartUpdateScript:MonoBehaviour
{
    public Transform _objectTrans;
    List<object> _updateKeys = new List<object>();
    public GameObject _objectComplex;
    AsyncOperationHandle<GameObject> _instantiateHandle;
    void Start()
    {
        Button _updateCatalogButton = _objectTrans.FindChild("Button1").gameObject.GetComponent<Button>();
        _updateCatalogButton.onClick.AddListener(UpdateCatalog);
        Button _downCatalogButton = _objectTrans.FindChild("Button2").gameObject.GetComponent<Button>();
        _downCatalogButton.onClick.AddListener(DownLoadCatalog);

        _objectTrans.FindChild("Button11").gameObject.GetComponent<Button>().onClick
        .AddListener(InstCarPrefab);
        _objectTrans.FindChild("Button22").gameObject.GetComponent<Button>().onClick
        .AddListener(InstHousePrefab);
        _objectTrans.FindChild("Button33").gameObject.GetComponent<Button>().onClick
        .AddListener(InstComplexPrefab);
        _objectTrans.FindChild("Button44").gameObject.GetComponent<Button>().onClick
        .AddListener(InstPeoplePrefab);

        _objectTrans.FindChild("BtnDestroy11").gameObject.GetComponent<Button>().onClick
        .AddListener(DestroyComplex);
    }
    public void InstCarPrefab()
    {
        string assetName = "Assets/Prefabs/Car.prefab";
        StartCoroutine(AddressablesLoad(assetName));
    }

    public void InstHousePrefab()
    {
        string assetName = "Assets/Prefabs/House.prefab";
        StartCoroutine(AddressablesLoad(assetName));
    }
    public void InstComplexPrefab()
    {
        string assetName = "Assets/Prefabs/Complex.prefab";
        StartCoroutine(AddressablesLoad(assetName));
    }
    public void InstPeoplePrefab()
    {
        StartCoroutine(AddressablesLabelLoad());
    }
    public IEnumerator AddressablesLabelLoad()
    {
        if (_objectComplex != null)
        {
            DestroyComplex();
        }
        string key = "People.prefab";
        string label = "HD";
        AsyncOperationHandle<IList<GameObject>> loadAssetHandle = Addressables.LoadAssetsAsync<GameObject>(new List<object> { key, label }, 
        null, Addressables.MergeMode.Intersection);
        loadAssetHandle.Completed    += SwitchToHighDef;
        //加载到内存中
        yield return loadAssetHandle;
        
        if (loadAssetHandle.Status == AsyncOperationStatus.Succeeded 
        && loadAssetHandle.IsValid())
        {
            //_instantiateHandle = Addressables.InstantiateAsync(loadAssetHandle.Result[0]);
            //Addressables.UnloadSceneAsync
            //_objectComplex = _instantiateHandle.Result;
            //Addressables.Release(loadAssetHandle);
        }
    }
    public void SwitchToHighDef(AsyncOperationHandle<IList<GameObject>> loadAssetHandle)
    {
        _objectComplex = Instantiate(loadAssetHandle.Result[0]);
        //_instantiateHandle = Addressables.InstantiateAsync(loadAssetHandle.Result[0]);
        //Addressables.UnloadSceneAsync
        //_objectComplex = _instantiateHandle.Result;
    }

    public IEnumerator AddressablesLoad(string assetName)
    {
        if (_objectComplex != null)
        {
            DestroyComplex();
        }

        AsyncOperationHandle<GameObject> loadAssetHandle = Addressables.LoadAssetAsync<GameObject>(assetName);
        //加载到内存中
        yield return loadAssetHandle;
        
        if (loadAssetHandle.Status == AsyncOperationStatus.Succeeded 
        && loadAssetHandle.IsValid())
        {
            //实例化
            _instantiateHandle = Addressables.InstantiateAsync(assetName);
            //Addressables.UnloadSceneAsync
            _objectComplex = _instantiateHandle.Result;
            Addressables.Release(loadAssetHandle);
        }
    }
    void DestroyComplex()
    {
        if (_objectComplex != null)
        {
            Destroy(_objectComplex);
            Debug.Log("开始 ReleaseInstance");
            Addressables.ReleaseInstance(_instantiateHandle);
        }
        //释放实例化后的对象
        //StartCoroutine(ReleaseInstance(_instantiateHandle));
    }
    private IEnumerator ReleaseInstance(AsyncOperationHandle<GameObject> asyncOperationHandle )
    {
        yield return new WaitForSeconds(1);
        Debug.Log("开始 ReleaseInstance");
        Addressables.ReleaseInstance(asyncOperationHandle);
    }
     public  async void UpdateCatalog()
    {
        //开始连接服务器检查更新
        var handle = Addressables.CheckForCatalogUpdates(false);
        await handle.Task;
        Debug.Log("check catalog status " + handle.Status);
        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            List<string> catalogs = handle.Result;
            if (catalogs != null && catalogs.Count > 0)
            {
                foreach (var catalog in catalogs)
                {
                    Debug.Log("catalog  " + catalog);
                }
                Debug.Log("download catalog start ");
                var updateHandle = Addressables.UpdateCatalogs(catalogs, false);
                await updateHandle.Task;
                foreach (var item in updateHandle.Result)
                {
                    Debug.Log("catalog result " + item.LocatorId);
                    foreach (var key in item.Keys)
                    {
                        //Debug.Log("catalog key " + key);
                    }
                    _updateKeys.AddRange(item.Keys);
                }
                Debug.Log("download catalog finish " + updateHandle.Status);
                Addressables.Release(handle);
            }
            else
            {
                Debug.Log("dont need update catalogs");
            }
        }
        Addressables.Release(handle);
    }

    public void DownLoadCatalog()
    {
        StartCoroutine(DownAssetImpl());
    }
    public IEnumerator DownAssetImpl()
    {
        var downloadsize = Addressables.GetDownloadSizeAsync(_updateKeys);
        yield return downloadsize;
        Debug.Log("start download size :" + downloadsize.Result);
        if (downloadsize.Result > 0)
        {
            var download = Addressables.DownloadDependenciesAsync(_updateKeys, Addressables.MergeMode.Intersection);
            yield return download;
            //await download.Task;
            Debug.Log("download result type " + download.Result.GetType());
            foreach (var item in download.Result as List<UnityEngine.ResourceManagement.ResourceProviders.IAssetBundleResource>)
            {
                var ab = item.GetAssetBundle();
                Debug.Log("ab name " + ab.name);
                foreach (var name in ab.GetAllAssetNames())
                {
                    Debug.Log("asset name " + name);
                }
            }
            Addressables.Release(download);
        }
        Addressables.Release(downloadsize);
    }

    //AddressablesImpl.cs
//添加2个新方法
//初始化完之后调用GetRemoteBundleSizeAsync方法
// AsyncOperationHandle<long> GetRemoteBundleSizeWithChain(IList<string> bundles)
// {
//     return ResourceManager.CreateChainOperation(InitializationOperation, op => GetRemoteBundleSizeAsync(key));
// }
//通过ab包名得到ab包大小
// public AsyncOperationHandle<long> GetRemoteBundleSizeAsync(IList<string> bundles)
// {
//     //如果还没初始化完那么等待初始化完
//     if(!InitializationOperation.IsDone)
//         return GetRemoteBundleSizeWithChain(key);
//     IList<IResourceLocation> locations = new IList<IResourceLocation>();
//     for(var i = 0; i < bundles.Count, i++)
//     {
//         IList<IResourceLocation> tmpLocations;
//         var key = bundles[i];
//         //寻找传入的包名对应的ab包，如果没找到那么警告
//        if(!GetResourceLocations(key, typeof(object), out locations))
//         return ResourceManager.CreateCompletedOperation<Long>(0, new InvalidKeyException(key).Message);
//         locations.Add(tmpLocations[0]);
//     }
//     //总的包大小
//     long size = 0;
//     for(var i = 0; i < locations.Count; i++)
//     {
//         if(locations[i].Data != null)
//         {
//             var sizeData = locations[i].Data as ILocationSizeData;
//             if(sizeData != null)
//             {
//                 //计算包大小
//                 size += sizeData.ComputeSize(locations[i]);
//             }
//         }
//     }
//     //返回总的包大小
//     return ResourceManager.CreateCompletedOperation<Long>(size, string.Empty)
// }
//在对应的Addressables外观类里也添加GetRemoteBundleSizeAsync方法

//使用方法
//在addressable初始化完成后遍历所有地址，如果地址的结尾是.bundle，那么他对应了一个ab包，然后把它缓存到列表，再使用添加的接口来获得所有需要更新包的大小。
// Addressables.InitializeAsync().Completed += opHandle =>
// {
//     var map = opHandle.Result as ResourceLocationMap;
//     List<string> bundles = new List<string>();
//     foreach (object mapKey in map.keys)
//     {
//         string key = mapKey as string;
//         if(key != null && key.EndsWith(".bundle"))
//         {
//             bundles.Add(key);
//         }
//     }
//     Addressables.GetRemoteBundleSizeAsync(key).Completed += asyncOpHandle => print(asyncOpHandle.Result);
// };

}