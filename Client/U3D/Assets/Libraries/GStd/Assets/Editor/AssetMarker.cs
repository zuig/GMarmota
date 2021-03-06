﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;

/*
功能
·资源导入时，根据所在目录是否受AssetMarker约束而自动设置
·手动设置所有受AssetMarker约束的资源

参数
·bundleName：AssetBundleName命名规则，例：uis/views/{firstdir}
    可选参数：
    ·fulldir：与AssetMarker配置文件的相对路径
    ·firstdir：与AssetMarker配置文件的相对路径的第一个目录命名。
    ·seconddir：与AssetMarker配置文件的相对路径的第二个目录命名。
    ·filename：使用asset的名字命名
·include/exclude：资源筛选规则

TODO：
·支持嵌套
·异常检测和处理
    ·重复设置
    ·不合理的命名自动设置为__foundation
·检测资源是否有冗余
 */

namespace GStd.Editor.Asset{
	 [CreateAssetMenu(
            fileName = "AssetBundleMarker",
            menuName = "GStd/Asset/Bundle marker")]
	public class AssetMarker : ScriptableObject {
		[SerializeField]
		private string bundleName;

		[SerializeField]
		private string exclude;
		
		[SerializeField]
		private string include;

        private string MarkSeconddir(string dir, string path)
        {
            var indexOfSplit = dir.IndexOf("/");
            if (indexOfSplit != -1)
            {
                var secondIndexOfSplit = dir.IndexOf("/", indexOfSplit+1);
                if (secondIndexOfSplit != -1)
                    return dir.Substring(indexOfSplit+1, secondIndexOfSplit-indexOfSplit-1);
                else
                    return dir.Substring(indexOfSplit+1);
            }
            else
                return dir;
        }

		private string MarkFirstdir(string dir, string path)
        {
            var indexOfSplit = dir.IndexOf("/");
            if (indexOfSplit != -1)
                return dir.Remove(indexOfSplit);
            else
                return dir;
        }

        private string MarkFilename(string path)
        {
            return Path.GetFileNameWithoutExtension(path);
        }

		private const string strMarkFirstdir = "{firstdir}";
        private const string strMarkSeconddir = "{seconddir}";
        private const string strMarkFulldir = "{fulldir}";
        private const string strMarkFilename = "{filename}";

        public string GetMarkBundleName(string dir, string path)
        {
            if (this.bundleName == null)
                this.bundleName = "";

            var ret = this.bundleName;
            if (this.bundleName.Contains(strMarkFirstdir))
                ret = ret.Replace(strMarkFirstdir, this.MarkFirstdir(dir, path));
            if (this.bundleName.Contains(strMarkSeconddir))
                ret = ret.Replace(strMarkSeconddir, this.MarkSeconddir(dir, path));
            if (this.bundleName.Contains(strMarkFulldir))
                ret = ret.Replace(strMarkFulldir, dir);
            if (this.bundleName.Contains(strMarkFilename))
                ret = ret.Replace(strMarkFilename, this.MarkFilename(path));

            if (ret.EndsWith("/"))
                ret += "__foundation";

            // if (!ret.EndsWith(".ab"))
            //     ret += ".ab";
            return ret.ToLower();
        }

		public string GetDirFromPath(string path)
        {
            var markerPath = AssetDatabase.GetAssetPath(this);
            var rootDirectory = Path.GetDirectoryName(markerPath);
            var dir = Path.GetDirectoryName(path);
            if (dir.Length > rootDirectory.Length)
                dir = dir.Remove(0, rootDirectory.Length + 1);
            else
                dir = "";
            return dir;
        }

		public bool IsNeedMark(string assetPath)
		{
			// exclude directory
			if (Directory.Exists(assetPath))
				return false;

            if (!string.IsNullOrEmpty(include) && !Regex.IsMatch(assetPath, include))
                return false;
            if (!string.IsNullOrEmpty(exclude) && Regex.IsMatch(assetPath, exclude))
                return false;

			return true;
		}

        public Dictionary<string, string[]> FindAssets()
        {
            Dictionary<string, string[]> ret = new Dictionary<string, string[]>();
            var selfPath = AssetDatabase.GetAssetPath(this);
            // 创建的时候可能为空
            if (string.IsNullOrEmpty(selfPath))
                return ret;
            var rootDirectory = Path.GetDirectoryName(selfPath);

            var guids = AssetDatabase.FindAssets("t:object", new string[]{ rootDirectory });
            // guid去重，查找出来的guid可能会有重复先，例如fbx带有多个动作，也会被找出多份。
            List<string> guidList = new List<string>();
            foreach(var guid in guids)
            {
                if (!guidList.Contains(guid))
                    guidList.Add(guid);
            }
            guids = guidList.ToArray();

            //            
            Dictionary<string, List<string>> datas = new Dictionary<string, List<string>>();
            foreach(var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);

                if (path == selfPath || !this.IsNeedMark(path))
                    continue;

                var dir = this.GetDirFromPath(path);
                if (!datas.ContainsKey(dir))
                    datas.Add(dir, new List<string>());

                if (dir == "")
                    path = path.Remove(0, rootDirectory.Length + 1);
                else
                    path = path.Remove(0, rootDirectory.Length + 1 + dir.Length + 1);

                datas[dir].Add(path);
            }

            foreach(var kv in datas)
            {
                ret[kv.Key] = kv.Value.ToArray();
            }

            return ret;
        }

        public void MarkAll()
        {
            var assets = this.FindAssets();
            var selfPath = AssetDatabase.GetAssetPath(this);
            if (string.IsNullOrEmpty(selfPath))
                return;
            var rootDirectory = Path.GetDirectoryName(selfPath);

            foreach(var kv in assets)
            {
                var dir = kv.Key;
                foreach(var assetPath in kv.Value)
                {
                    var realAssetPath = rootDirectory + "/";
                    if (dir != "")
                        realAssetPath += dir + "/";
                    realAssetPath += assetPath;
                    this.Mark(realAssetPath);
                }
            }
        }

		public void Mark(string path)
        {
            AssetImporter importer = AssetImporter.GetAtPath(path);
            if (importer == null)
            {
                Debug.LogError(string.Format("mark {0} failed!", path));
                return;
            }

            var dir = this.GetDirFromPath(path);
            var assetBundleName = this.GetMarkBundleName(dir, path);

            if (importer.assetBundleName == assetBundleName)
                return;

            Debug.Log(string.Format("mark asset {0} -> {1}", importer.assetBundleName, assetBundleName));
            importer.assetBundleName = assetBundleName;
            importer.SaveAndReimport();
        }
	}
}

