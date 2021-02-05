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
public class AddressableEditor
{
    static Dictionary<string,string> assetDic = new Dictionary<string, string>();
    
    // [MenuItem("AddressableEditor/将选择的Group设置为可全量更新")]
    // public static void SetNonStaticContentGroup()
    // {
    //     foreach (AddressableAssetGroup groupAsset in Selection.objects)
    //     {
    //         for (int i = 0; i < groupAsset.Schemas.Count; i++)
    //         {
    //             var schema = groupAsset.Schemas[i];
    //             if (schema is UnityEditor.AddressableAssets.Settings.GroupSchemas.ContentUpdateGroupSchema)
    //             {
    //                 (schema as UnityEditor.AddressableAssets.Settings.GroupSchemas.ContentUpdateGroupSchema)
    //                     .StaticContent = false;
    //             }
    //             else if (schema is UnityEditor.AddressableAssets.Settings.GroupSchemas
    //                 .BundledAssetGroupSchema)
    //             {
    //                 var bundledAssetGroupSchema =
    //                     (schema as UnityEditor.AddressableAssets.Settings.GroupSchemas.BundledAssetGroupSchema);
    //                 bundledAssetGroupSchema.BuildPath.SetVariableByName(groupAsset.Settings,
    //                     AddressableAssetSettings.kLocalBuildPath);
    //                 bundledAssetGroupSchema.LoadPath.SetVariableByName(groupAsset.Settings,
    //                     AddressableAssetSettings.kLocalLoadPath);
    //             }
    //         }
    //     }
    //     AssetDatabase.SaveAssets();
    //     AssetDatabase.Refresh();
    // }
    [MenuItem("AddressableEditor/1.清除本地旧的打包数据")]
    public static void CleanContent()
    {
        AddressableAssetSettings.CleanPlayerContent();
    }
    // [MenuItem("AddressableEditor/暴力删除所有现有Group")]
    // public static void DeleteCurGroup()
    // {
    //     var settings = AddressableAssetSettingsDefaultObject.Settings;
    //     foreach (AddressableAssetGroup groupAsset in Selection.objects)
    //     {
    //         settings.RemoveGroup(groupAsset);
    //     }
    //     AssetDatabase.Refresh();
    // }

    [MenuItem("AddressableEditor/2.按照规则自动分组打包")]
    public static void AutoGroupBuild()
    {
        AddressableAssetSettings.CleanPlayerContent();
        //DeleteCurGroup();
        string sourcePath = Application.dataPath;
        DirectoryInfo srcDir = new DirectoryInfo(sourcePath);
        //IOHelper.GetAssetFiles(srcDir.FullName);
        assetDic.Clear();
        CreatGroupAuto(srcDir.FullName);
         var setting = AddressableAssetSettingsDefaultObject.Settings;
        foreach (KeyValuePair<string, string> kvp in assetDic)
        {
            //Debug.Log(kvp.Key+"  /  "+kvp.Value);
            var guid = AssetDatabase.AssetPathToGUID(kvp.Value);
            string str = kvp.Value;
            var groupName = str;
            int i1 = str.IndexOf('\\');
            if(i1 > 0)
            {
                groupName = str.Remove(0, i1+1);
                int i2 = groupName.IndexOf('\\');
                if(i2 > 0)
                {
                    groupName = groupName.Substring(0, i2);
                }
            }
            Debug.Log(groupName);
            AddressableAssetGroup group = setting.FindGroup(groupName);
            if (group == null)
            {
                if (groupName == "Prefabs")
                {
                    group = setting.CreateGroup(groupName, false, false, false,
                    new List<AddressableAssetGroupSchema> { setting.DefaultGroup.Schemas[0], setting.DefaultGroup.Schemas[1] });
                }else
                {
                    group = setting.CreateGroup(groupName, false, false, false,
                    new List<AddressableAssetGroupSchema> { setting.DefaultGroup.Schemas[0], setting.DefaultGroup.Schemas[1] });
                }
            }
            //var assetRef = setting.CreateAssetReference(guid);
            var entry = setting.CreateOrMoveEntry(guid, group);
            //entry.SetAddress(kvp.Key);
            //entry.SetLabel("LD",true);
        }
        AssetDatabase.Refresh();
        //AddressableAssetSettings.BuildPlayerContent();
    }
    [MenuItem("AddressableEditor/3.设置Group")]
    public static void SetStaticContentGroup()
    {
        foreach (AddressableAssetGroup groupAsset in Selection.objects)
        {
            for (int i = 0; i < groupAsset.Schemas.Count; i++)
            {
                var schema = groupAsset.Schemas[i];
                Debug.Log(schema.name);
                
                if (schema is UnityEditor.AddressableAssets.Settings.GroupSchemas.ContentUpdateGroupSchema)
                {
                    var bundledAssetGroupSchema =
                        (schema as UnityEditor.AddressableAssets.Settings.GroupSchemas.ContentUpdateGroupSchema);
                    bundledAssetGroupSchema.StaticContent = true;
                }
                else if (schema is UnityEditor.AddressableAssets.Settings.GroupSchemas
                    .BundledAssetGroupSchema)
                {
                    var bundledAssetGroupSchema =
                        (schema as UnityEditor.AddressableAssets.Settings.GroupSchemas.BundledAssetGroupSchema);
                    if(schema.name == "Prefabs_BundledAssetGroupSchema"){
                        bundledAssetGroupSchema.BundleMode = UnityEditor.AddressableAssets.Settings.GroupSchemas.BundledAssetGroupSchema.BundlePackingMode.PackSeparately;
                        bundledAssetGroupSchema.BuildPath.SetVariableByName(groupAsset.Settings,
                        AddressableAssetSettings.kRemoteBuildPath);
                        bundledAssetGroupSchema.LoadPath.SetVariableByName(groupAsset.Settings,
                        AddressableAssetSettings.kRemoteLoadPath);
                    }else{
                        bundledAssetGroupSchema.BuildPath.SetVariableByName(groupAsset.Settings,
                        AddressableAssetSettings.kLocalBuildPath);
                        bundledAssetGroupSchema.LoadPath.SetVariableByName(groupAsset.Settings,
                        AddressableAssetSettings.kLocalLoadPath);
                    }
                }
            }
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
    public static void CreatGroupAuto(string path)
    {
        foreach (AddressableAssetGroup groupAsset in Selection.objects)
        {
            for (int i = 0; i < groupAsset.Schemas.Count; i++)
            {
                var schema = groupAsset.Schemas[i];
                if (schema is UnityEditor.AddressableAssets.Settings.GroupSchemas.ContentUpdateGroupSchema)
                {
                    (schema as UnityEditor.AddressableAssets.Settings.GroupSchemas.ContentUpdateGroupSchema)
                        .StaticContent = false;
                }
                else if (schema is UnityEditor.AddressableAssets.Settings.GroupSchemas
                    .BundledAssetGroupSchema)
                {
                    var bundledAssetGroupSchema =
                        (schema as UnityEditor.AddressableAssets.Settings.GroupSchemas.BundledAssetGroupSchema);
                    bundledAssetGroupSchema.BuildPath.SetVariableByName(groupAsset.Settings,
                        AddressableAssetSettings.kLocalBuildPath);
                    bundledAssetGroupSchema.LoadPath.SetVariableByName(groupAsset.Settings,
                        AddressableAssetSettings.kLocalLoadPath);
                }
            }
        }
        DirectoryInfo root = new DirectoryInfo(path);
        var groupName = root.Name;
        AutoAddFileAsset(root.FullName);
        foreach (DirectoryInfo d in root.GetDirectories())
        {
            if ((d.Name == "AddressableAssetsData") || (d.Name == "Editor")
                ||  (d.Name == "Resources")||  (d.Name == "Scripts"))
                {
                 continue;
                }
            CreatGroupAuto(d.FullName);
        }
    }
    public static void AutoAddFileAsset(string filePath)
    {
        DirectoryInfo root = new DirectoryInfo(filePath);
        foreach (FileInfo f in root.GetFiles())
        {
            if (f.Extension == ".meta"){
                continue;
            }
            int assetIndex = filePath.IndexOf("Assets");
            string guidPath = filePath.Remove(0, assetIndex);
            //Debug.Log(guidPath);
            assetDic.Add(f.Name,guidPath);
        }
    }
    // [MenuItem("AddressableEditor/GetSchemas")]
    // public static void GetSchemas()
    // {
    //     var m_Settings = AddressableAssetSettingsDefaultObject.Settings;
    //     string guidPath = m_Settings.GroupFolder;
    //     //Debug.Log(guidPath);
    //     int assetIndex = guidPath.IndexOf("\\");
    //     //Debug.Log(assetIndex);
    //     //Debug.Log(guidPath.Remove(0, assetIndex));
    //     var schemaPath = Application.dataPath+guidPath.Remove(0, assetIndex)+"/Schemas";
    //     var filePath = schemaPath.Replace('/', '\\');
    //     Debug.Log(filePath);
    //     DirectoryInfo root = new DirectoryInfo(filePath);
    //     foreach (FileInfo f in root.GetFiles())
    //     {
    //         if (f.Extension == ".meta"){
    //             continue;
    //         }
    //         Debug.Log(f.Name);
    //     }
    // }
    [MenuItem("AddressableEditor/4.打初始包")]
    public static void BuildContent()
    {
        AddressableAssetSettings.BuildPlayerContent();
    }

     [MenuItem("AddressableEditor/5.热更资源汇总")]
    public static void CheckForUpdateContent()
    {
        //与上次打包做资源对比
        string buildPath = ContentUpdateScript.GetContentStateDataPath(false);
        var m_Settings = AddressableAssetSettingsDefaultObject.Settings;
        List<AddressableAssetEntry> entrys =
            ContentUpdateScript.GatherModifiedEntries(
                m_Settings, buildPath);
        if (entrys.Count == 0) return;
        StringBuilder sbuider = new StringBuilder();
        sbuider.AppendLine("Need Update Assets:");
        foreach (var _ in entrys)
        {
            sbuider.AppendLine(_.address);
        }
        Debug.Log(sbuider.ToString());
        
        //将被修改过的资源单独分组
        //var groupName = string.Format("UpdateGroup_{0}",DateTime.Now.ToString("yyyyMMdd"));
        //ContentUpdateScript.CreateContentUpdateGroup(m_Settings, entrys, groupName);
    }

    [MenuItem("AddressableEditor/6.Build热更新内容")]
    public static void BuildUpdate()
    {
        var path = ContentUpdateScript.GetContentStateDataPath(false);
        var m_Settings = AddressableAssetSettingsDefaultObject.Settings;
        AddressablesPlayerBuildResult result = ContentUpdateScript.BuildContentUpdate(AddressableAssetSettingsDefaultObject.Settings, path);
        Debug.Log("BuildFinish path = " + m_Settings.RemoteCatalogBuildPath.GetValue(m_Settings));
    }

}
