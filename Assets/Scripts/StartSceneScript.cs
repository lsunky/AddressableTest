using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;
public class StartSceneScript:MonoBehaviour
{
    public bool IsRef = true;
    public Transform _objectTrans;
    public GameObject _objectComplexRef;
    
    Button _instantiateComplexButton;
    Button _destroyComplexButton;
    Button _releaseComHandleButton;
    AsyncOperationHandle<GameObject> complexHandle;
    List<object> _updateKeys = new List<object>();
    public AssetReference assetReference;
    public GameObject _objectComplex;
    AsyncOperationHandle<GameObject> _loadAssetHandle;
    AsyncOperationHandle<GameObject> _instantiateHandle;
    void Start()
    {
        _instantiateComplexButton = _objectTrans.FindChild("Button1").gameObject.GetComponent<Button>();
        _destroyComplexButton = _objectTrans.FindChild("Button2").gameObject.GetComponent<Button>();
        _releaseComHandleButton = _objectTrans.FindChild("Button3").gameObject.GetComponent<Button>();

        //assetReference
        if (IsRef){
            _instantiateComplexButton.onClick.AddListener(InstantiateComplexRef);
            _destroyComplexButton.onClick.AddListener(DestroyComplexRef);
            _releaseComHandleButton.onClick.AddListener(RealeseComplexRef);
        }else{
            //Addressables
            _instantiateComplexButton.onClick.AddListener(InstantiateComplex);
            _destroyComplexButton.onClick.AddListener(DestroyComplex);
            _releaseComHandleButton.onClick.AddListener(RealeseComplex);
        }
        
    }
    void InstantiateComplexRef()
    {
        complexHandle = assetReference.LoadAssetAsync<GameObject>();
        complexHandle.Completed += (handle) =>
        {
            if (handle.Status == AsyncOperationStatus.Succeeded && handle.IsValid())
            {
                _objectComplexRef = assetReference.InstantiateAsync(Vector3.zero,Quaternion.identity,parent:null).Result;
            }
        };
        // var cubeHandle = assetReference.LoadSceneAsync();
        // cubeHandle.Completed += (handle) =>
        // {
        //     if (handle.Status == AsyncOperationStatus.Succeeded && handle.IsValid())
        //     {
        //         Debug.Log("success");
        //     }
        // };
    }
    void DestroyComplexRef()
    {
        if (_objectComplexRef != null)
        {
            Destroy(_objectComplexRef);
        }
        Addressables.Release(_objectComplexRef);
    }
    void RealeseComplexRef()
    {
        assetReference.ReleaseAsset();
    }
    void InstantiateComplex()
    {
         StartCoroutine(AddressablesLoad());
    } 
    void Update()
    {
        if(_loadAssetHandle.IsValid()){
            Debug.Log(_loadAssetHandle.PercentComplete);
            if (_loadAssetHandle.PercentComplete > 0.4f)
            {
                //StopCoroutine(AddressablesLoad());
                Addressables.Release(_loadAssetHandle);
            }
        }
    }
    public IEnumerator AddressablesLoad()
    {
        AsyncOperationHandle<GameObject> loadAssetHandle = Addressables.LoadAssetAsync<GameObject>("Assets/Prefabs/Complex.prefab");
         _loadAssetHandle = loadAssetHandle;
        yield return loadAssetHandle;
        if (loadAssetHandle.IsValid())
        {
             if (loadAssetHandle.Status == AsyncOperationStatus.Succeeded )
            {
                _instantiateHandle = Addressables.InstantiateAsync("Assets/Prefabs/Complex.prefab");
                _objectComplex = _instantiateHandle.Result;
            }
        }
    }
     void DestroyComplex()
    {
        if (_objectComplex != null)
        {
            Destroy(_objectComplex);
        }
        //释放实例化后的对象
        StartCoroutine(ReleaseInstance(_instantiateHandle));
    }
    void RealeseComplex()
    {
        //这里并不是Destroy对象，只是释放资源的加载操作loadAssetHandle
        //资源被成功使用或者实例化之后，释放加载资源的操作 
        Addressables.Release(_loadAssetHandle);
        
    }
    private IEnumerator ReleaseInstance(AsyncOperationHandle<GameObject> asyncOperationHandle )
    {
        yield return new WaitForSeconds(1);
        Debug.Log("开始 ReleaseInstance");
        Addressables.ReleaseInstance(asyncOperationHandle);
    }
    
}