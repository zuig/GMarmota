﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GStd.Asset{
	public static class AssetManager
	{
#region editor
#if UNITY_EDITOR
        private const string PREF_SIMULATE = "absimulate";

        [MenuItem("GStd/AssetBundle/Simulate", false, 0)]
        private static void Simulate()
        {
            Menu.SetChecked("GStd/AssetBundle/Simulate", true);
            EditorPrefs.SetBool(PREF_SIMULATE, true);
        }

        [MenuItem("GStd/AssetBundle/Simulate", true, 0)]
        private static bool CheckSimulate()
        {
            Menu.SetChecked("GStd/AssetBundle/Simulate", EditorPrefs.GetBool(PREF_SIMULATE));
            return true;
        }

        [MenuItem("GStd/AssetBundle/No Simulate", false, 1)]
        private static void NoSimulate()
        {
            Menu.SetChecked("GStd/AssetBundle/Simulate", false);
            EditorPrefs.SetBool(PREF_SIMULATE, false);
        }

        [MenuItem("GStd/AssetBundle/No Simulate", true, 1)]
        private static bool CheckNoSimulate()
        {
            Menu.SetChecked("GStd/AssetBundle/No Simulate", !EditorPrefs.GetBool(PREF_SIMULATE));
            return true;
        }

		private static bool CheckSwitchPlatform(BuildTarget target)
		{
			#if UNITY_ANDROID
			if (target != BuildTarget.Android)
				return true;
			#elif UNITY_IOS
			if (target != BuildTarget.iOS)
				return true;
			#else
			if (target != BuildTarget.StandaloneWindows && target != BuildTarget.StandaloneWindows64)
				return true; 
			#endif

			return false;
		}

		[MenuItem("GStd/AssetBundle/Build/PC", false, 2)]
		private static void BuildPC()
		{
			if (CheckSwitchPlatform(BuildTarget.StandaloneWindows) && !EditorUtility.DisplayDialog("提示", "将触发切换平台", "确定", "取消"))
				return;

            string outputPath = Application.dataPath + "/../AssetBundle/PC/AssetBundle";
			if (!System.IO.Directory.Exists(outputPath))
                System.IO.Directory.CreateDirectory(outputPath);
			BuildPipeline.BuildAssetBundles(outputPath, BuildAssetBundleOptions.None, BuildTarget.StandaloneWindows64);
		}

        [MenuItem("GStd/AssetBundle/Build/Android", false, 3)]
        private static void BuildAndroid()
        {
            if (CheckSwitchPlatform(BuildTarget.Android) && !EditorUtility.DisplayDialog("提示", "将触发切换平台", "确定", "取消"))
                return;

            string outputPath = Application.dataPath + "/../AssetBundle/Android/AssetBundle";
            if (!System.IO.Directory.Exists(outputPath))
                System.IO.Directory.CreateDirectory(outputPath);
            BuildPipeline.BuildAssetBundles(outputPath, BuildAssetBundleOptions.None, BuildTarget.Android);
        }

        [MenuItem("GStd/AssetBundle/Build/iOS", false, 4)]
        private static void BuildiOS()
        {
            if (CheckSwitchPlatform(BuildTarget.iOS) && !EditorUtility.DisplayDialog("提示", "将触发切换平台", "确定", "取消"))
                return;

            string outputPath = Application.dataPath + "/../AssetBundle/iOS/AssetBundle";
            if (!System.IO.Directory.Exists(outputPath))
                System.IO.Directory.CreateDirectory(outputPath);
            BuildPipeline.BuildAssetBundles(outputPath, BuildAssetBundleOptions.None, BuildTarget.iOS);
        }
#endif
#endregion
        
#region setup
		private static AssetLoader assetLoader;

		// setup
#if UNITY_EDITOR
		private static bool IsSimulateAssetBundle()
		{
			return EditorPrefs.GetBool(PREF_SIMULATE);
		}

		private static void SetupSimulateLoader()
		{
			assetLoader = new AssetLoaderSimulate();
		}
#endif

		

		public static void Setup()
		{
#if UNITY_EDITOR
				if (IsSimulateAssetBundle())
					SetupSimulateLoader();
				else
					SetupABLoader();
#else
				SetupABLoader();
#endif

            gameObjectPool = new GameObjectPool();
		}

		private static void SetupABLoader()
		{
			assetLoader = new AssetLoaderAB();
		}
		#endregion

#region asset bundle functions
		// asset bundle
		public struct LoadItem
		{
			public string assetBundleName;
			public string assetName;
			public Object inst;
		}

		private static List<LoadItem> loaded = new List<LoadItem>();

		public static T LoadAsset<T>(string assetBundleName, string assetName) where T:UnityEngine.Object
		{
			foreach(var item in loaded)
			{
				if (item.assetBundleName == assetBundleName && item.assetName == assetName)
					return item.inst as T;
			}

			var ret = assetLoader.LoadAsset<T>(assetBundleName, assetName);
			loaded.Add(new LoadItem(){assetBundleName=assetBundleName, assetName=assetName, inst=ret}); 

			return ret;
		}

		public static UnityEngine.Object LoadAsset(string assetBundleName, string assetName, System.Type type)
		{
			return assetLoader.LoadAsset(assetBundleName, assetName, type);
		}

		public static bool IsAssetBundleCache(string assetBundleName)
		{
			return false;	
		}

		public static void UnloadAssetBundle(string assetBundleName)
		{
			
		}
#endregion

#region pool
		// pool
		private static GameObjectPool gameObjectPool;

		public static GameObject Spawn(string assetBundleName, string assetName)
		{
			return gameObjectPool.Spawn(assetBundleName, assetName);
		}
		public static GameObject Spawn(string assetBundleName, string assetName, Vector3 position, Vector3 rotation, Transform parent)
		{
			return gameObjectPool.Spawn(assetBundleName, assetName);
		}

		public static GameObject Spawn(GameObject prefab)
		{
			return gameObjectPool.Spawn(prefab, Vector3.zero, Quaternion.identity, null);
		}

		public static GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent)
		{
			return gameObjectPool.Spawn(prefab, position, rotation, parent);
		}

		public static void Despawn(GameObject inst)
		{
			gameObjectPool.Despawn(inst);
		}

		public static void Clear()
		{

		}
#endregion
	}
}
