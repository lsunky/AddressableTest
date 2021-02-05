using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
public class IOHelper
{
    public static bool IsExistMeta(string strPath) {
        return File.Exists(strPath + ".meta");
    }

    public static int GetFileSize(string strFilePath) {

        if (string.IsNullOrEmpty(strFilePath) || !File.Exists(strFilePath))
            throw new ApplicationException("GetFile Size Failed");

        return (int)new FileInfo(strFilePath).Length;
    }

    public static void SerachFiles(string sourceDir, string searchPattern, ref List<string> arFile)
    {

        if (!Directory.Exists(sourceDir))
            return;

        foreach (var file in Directory.GetFiles(sourceDir, searchPattern))
        {
            string strPath = file.Replace('\\', '/');
            arFile.Add(strPath);
        }

        foreach (var directory in Directory.GetDirectories(sourceDir))
        {
            SerachFiles(directory
                , searchPattern
                , ref arFile);
        }
    }

    public static void CopyDirectory(string sourceDir, string targetDir, string searchPattern = "*", string newExt = null, bool keepHierachy = true)
    {
        if (!Directory.Exists(sourceDir))
            return;

        if (!Directory.Exists(targetDir))
            Directory.CreateDirectory(targetDir);

        foreach (var file in Directory.GetFiles(sourceDir, searchPattern))
        {
            bool isHidden = ((File.GetAttributes(file) & FileAttributes.Hidden) == FileAttributes.Hidden);
            if (isHidden)
                continue;

            string fileName = newExt == null ? Path.GetFileName(file) : Path.ChangeExtension(Path.GetFileName(file), newExt);

            File.Copy(file, Path.Combine(targetDir, fileName), true);
        }

        foreach (var directory in Directory.GetDirectories(sourceDir))
        {
            DirectoryInfo info = new DirectoryInfo(directory);
            bool isHidden = ((info.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden);
            if (isHidden)
                continue;

            string targetPath = null;
            if (keepHierachy)
            {
                targetPath = Path.Combine(targetDir, Path.GetFileName(directory));
            }
            else
            {
                targetPath = targetDir;
            }
            CopyDirectory(directory, targetPath, searchPattern, newExt, keepHierachy);
        }
    }

    public static bool CopyDirectory(string sPath, string dPath, string[] copyFiles)
    {
        DirectoryInfo destDir = new DirectoryInfo(dPath);
        dPath = destDir.FullName;
        bool isSuccess = false;
        try
        {
            if (!Directory.Exists(dPath))
                Directory.CreateDirectory(dPath);
            DirectoryInfo srcDir = new DirectoryInfo(sPath);
            sPath = srcDir.FullName;
            DirectoryInfo[] srcDirs = srcDir.GetDirectories();
            CopyFile(srcDir, dPath, copyFiles);
            if (srcDirs.Length > 0)
            {
                foreach (DirectoryInfo temDirectoryInfo in srcDirs)
                {
                    string sourceDirectoryFullName = temDirectoryInfo.FullName;
                    string destDirectoryFullName = sourceDirectoryFullName.Replace(sPath, dPath);
                    if (!Directory.Exists(destDirectoryFullName))
                    {
                        Directory.CreateDirectory(destDirectoryFullName);
                    }
                    CopyDirectory(sourceDirectoryFullName, destDirectoryFullName, copyFiles);
                }
            }
            isSuccess = true;
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError(ex.ToString());
        }
        return isSuccess;
    }

    public static void CheckDirectory(string fullPath)
    {
        if (!Directory.Exists(fullPath))
        {
            Directory.CreateDirectory(fullPath);
        }
    }

    public static void CopyFile(DirectoryInfo path, string desPath, string[] copyFiles)
    {
        string sourcePath = path.FullName;
        FileInfo[] files = path.GetFiles();
        foreach (FileInfo file in files)
        {
            string sourceFileFullName = file.FullName;
            string destFileFullName = sourceFileFullName.Replace(sourcePath, desPath);
            if (copyFiles != null)
            {
                foreach (var s in copyFiles)
                {
                    if (s == file.Name)
                    {
                        file.CopyTo(destFileFullName, true);
                    }
                }
            }
            else
            {
                file.CopyTo(destFileFullName, true);
            }
        }
    }

    public static void ReplaceFile(string strSource)
    {
        string strFileName = Path.GetFileName(strSource);

        string[] arAsset = AssetDatabase.FindAssets(Path.GetFileNameWithoutExtension(strSource));
        
        List<string> _TmpAsset = new List<string>(arAsset).Distinct().ToList();
        List<string> _Asset    = new List<string>();
        
        //精确匹配
        foreach (var iGuid in _TmpAsset)
        {
            string strPath    = AssetDatabase.GUIDToAssetPath(iGuid);
            
            string strFileEx  = Path.GetFileName(strPath);

            if (strFileEx == Path.GetFileName(strSource))
            {
                _Asset.Add(strPath);
            }
        }
            
        if (_Asset.Count > 1 || _Asset.Count <= 0)
        {
            Debug.LogError("Asset Replace Failed File:"+strFileName + " Length:"+_Asset.Count);
            return;
        }
        
        string strDirectory = Path.Combine(Application.dataPath,_Asset[0].Remove(0,7));
        
        File.Copy(strSource
        , strDirectory
        , true);

        //Debug.Log(string.Format("Replace File Source:{0} Target:{1}",strSource,strDirectory));
    }

    public static void GetAssetFiles(String rootPath)
    {
        // string parentPath = Directory.GetParent(rootPath).FullName;//上级目录
        // string topPath = Directory.GetParent(parentPath).FullName;//上上级目录
        StreamWriter sw = null;
        try
        {
            //创建输出流，将得到文件名子目录名保存到txt中
            sw = new StreamWriter(new FileStream("fileList.txt", FileMode.Append));
            sw.WriteLine("根目录：" + rootPath);
            getDirectory(sw, rootPath, 2);
        }
        catch (IOException e)
        {
            Console.WriteLine(e.Message);
        }
        finally
        {
            if (sw != null)
            {
                sw.Close();
                Console.WriteLine("完成");
            }
        }

    }

    /// <summary>
    /// 获得指定路径下所有文件名
    /// </summary>
    /// <param name="sw">文件写入流</param>
    /// <param name="path">文件写入流</param>
    /// <param name="indent">输出时的缩进量</param>
    public static void getFileName(StreamWriter sw, string path, int indent)
    {
        DirectoryInfo root = new DirectoryInfo(path);
        foreach (FileInfo f in root.GetFiles())
        {
            if (f.Extension == ".meta"){
                continue;
            }
            for (int i = 0; i < indent; i++)
            {
                sw.Write("  ");
            }
            sw.WriteLine(f.Name);
        }
    }

    /// <summary>
    /// 获得指定路径下所有子目录名
    /// </summary>
    /// <param name="sw">文件写入流</param>
    /// <param name="path">文件夹路径</param>
    /// <param name="indent">输出时的缩进量</param>
    public static void getDirectory(StreamWriter sw, string path, int indent)
    {
        getFileName(sw, path, indent);
        DirectoryInfo root = new DirectoryInfo(path);
        foreach (DirectoryInfo d in root.GetDirectories())
        {
            if (d.Name == "AddressableAssetsData"){
                continue;
            }
            if (d.Name == "Editor"){
                continue;
            }
            if (d.Name == "Resources"){
                continue;
            }
            if (d.Name == "Scripts"){
                continue;
            }
            for (int i = 0; i < indent; i++)
            {
                sw.Write("  ");
            }
            sw.WriteLine("文件夹：" + d.Name);
            getDirectory(sw, d.FullName, indent + 2);
            sw.WriteLine();
        }
    }
}
