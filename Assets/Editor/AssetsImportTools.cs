using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UnityEngine.AddressableAssets;
 public class AssetsImportTools : AssetPostprocessor
{
    //--导入声音前
    public void OnPreprocessAudio()
    {
        //Debug.Log("导入声音前 OnPreprocessAudio");
        AudioImporter audioImporter = (AudioImporter)assetImporter;
        //Debug.Log(string.Format("路径 ：{0}", audioImporter.assetPath));
    }
    //--导入声音后
    public void OnPostprocessAudio(AudioClip clip)
    {
        //Debug.Log("导入声音后 OnPostprocessAudio");
    }
 
    //--导入模型前
    public void OnPreprocessModel()
    {
        //Debug.Log("导入模型前 OnPreprocessModel");
        ModelImporter modelImporter = (ModelImporter)assetImporter;
    }
 
    //--导入模型后
    public void OnPostprocessModel(GameObject gameObject)
    {
        ModelImporter modelImporter = (ModelImporter)assetImporter;
        var assetPath = modelImporter.assetPath;
        var guid = AssetDatabase.AssetPathToGUID(assetPath);
        var setting = AddressableAssetSettingsDefaultObject.Settings;
        var groupName = "Import";
        AddressableAssetGroup group = setting.FindGroup(groupName);
        if (group == null)
        {
            group = setting.CreateGroup(groupName, false, false, false,
                new List<AddressableAssetGroupSchema> { setting.DefaultGroup.Schemas[0], setting.DefaultGroup.Schemas[1] });
        }
        var entry = setting.CreateOrMoveEntry(guid, group);
        //Debug.Log("导入模型后 OnPostprocessModel");
    }
 
    //--导入贴图前
    public void OnPreprocessTexture()
    {
        //Debug.Log("导入贴图前 OnPreprocessTexture");
        TextureImporter textureImporter = (TextureImporter)assetImporter;
    }
 
    //--导入贴图后
    public void OnPostprecessTexture(Texture2D texture2D)
    {
        TextureImporter textureImporter = (TextureImporter)assetImporter;
        var assetPath = textureImporter.assetPath;
        var guid = AssetDatabase.AssetPathToGUID(assetPath);
        var setting = AddressableAssetSettingsDefaultObject.Settings;
        var groupName = "Atlas";
        AddressableAssetGroup group = setting.FindGroup(groupName);
        if (group == null)
        {
            group = setting.CreateGroup(groupName, false, false, false,
                new List<AddressableAssetGroupSchema> { setting.DefaultGroup.Schemas[0], setting.DefaultGroup.Schemas[1] });
        }
        var entry = setting.CreateOrMoveEntry(guid, group);
        //Debug.Log("导入贴图后 OnPostprecessTexture");
    }
 
    //--导入动画前
    public void OnPreprocessAnimation()
    {
        Debug.Log("导入动画前 OnPreprocessAnimation");
        ModelImporter modelImporter = (ModelImporter)assetImporter;
    }
    //--导入动画后
    public void OnPostprocessAnimation(GameObject gameObject, AnimationClip animationClip)
    {
        Debug.Log("导入动画后 OnPostprocessAnimation");
    }
 
    //--导入材质后
    public void OnPostprocessMaterial(Material material)
    {
        Debug.Log("导入材质后 OnPostprocessMaterial");
    }
 
 
    //--导入精灵后
    public void OnPostprocessSprites(Texture2D texture, Sprite[] sprites)
    {
         TextureImporter textureImporter = (TextureImporter)assetImporter;
        var assetPath = textureImporter.assetPath;
        var guid = AssetDatabase.AssetPathToGUID(assetPath);
        var setting = AddressableAssetSettingsDefaultObject.Settings;
        var groupName = "Atlas";
        AddressableAssetGroup group = setting.FindGroup(groupName);
        if (group == null)
        {
            group = setting.CreateGroup(groupName, false, false, false,
                new List<AddressableAssetGroupSchema> { setting.DefaultGroup.Schemas[0], setting.DefaultGroup.Schemas[1] });
        }
        var entry = setting.CreateOrMoveEntry(guid, group);
        Debug.Log("导入精灵后 OnPostprocessSprites");
    }

    
 
    public static void OnPostprocessAllAssets(string[]importedAsset,string[] deletedAssets,string[] movedAssets,string[]movedFromAssetPaths)
    {
        // Debug.Log ("OnPostprocessAllAssets");
        // foreach (string str in importedAsset) {
        //     Debug.Log("importedAsset = "+str);
        // }
        // foreach (string str in deletedAssets) {
        //     Debug.Log("deletedAssets = "+str);
        // }
        // foreach (string str in movedAssets) {
        //     Debug.Log("movedAssets = "+str);
        // }
        // foreach (string str in movedFromAssetPaths) {
        //     Debug.Log("movedFromAssetPaths = "+str);
        // }
    }

}