/* Copyright (C) 2012 Tim Graupmann, Tagenigma LLC 
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 * 
 */
using System;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using System.Threading;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class OuyaPanel : EditorWindow
{
    private static string[] m_toolSets =
        {
            "OUYA",
            "Unity",
            "Java JDK",
            "Android SDK"
        };

    private int m_selectedToolSet = 0;

    #region Operations

    private bool m_toggleBuildApplication = false;

    private bool m_toggleBuildAndRunApplication = false;

    private bool m_toggleCompileJava = false;
	

    #endregion

    #region OUYA SDK

    public const string KEY_PATH_OUYA_SDK = @"OUYA SDK";
    public const string KEY_PATH_JAR_GUAVA = @"Guava Jar";
    public const string KEY_PATH_JAR_GSON = @"GSON Jar";

    private static string pathOuyaSDKJar = string.Empty;
    private static string pathGsonJar = string.Empty;
    private static string pathGuavaJar = string.Empty;
    


    

    private static string pathManifestPath = string.Empty;
    private static string pathRes = string.Empty;
    private static string pathBin = string.Empty;
    private static string pathSrc = string.Empty;

    void UpdateOuyaPaths()
    {
        pathOuyaSDKJar = string.Format("{0}/Assets/Plugins/Android/libs/ouya-sdk.jar", pathUnityProject);
        pathGsonJar = string.Format("{0}/Assets/Plugins/Android/libs/gson-2.2.2.jar", pathUnityProject);
        pathGuavaJar = string.Format("{0}/Assets/Plugins/Android/libs/guava-r09.jar", pathUnityProject);
        

        pathManifestPath = string.Format("{0}/Assets/Plugins/Android/AndroidManifest.xml", pathUnityProject);
        pathRes = string.Format("{0}/Assets/Plugins/Android/res", pathUnityProject);
        pathBin = string.Format("{0}/Assets/Plugins/Android/bin", pathUnityProject);
        pathSrc = string.Format("{0}/Assets/Plugins/Android/src", pathUnityProject);

        EditorPrefs.SetString(KEY_PATH_OUYA_SDK, pathOuyaSDKJar);
    }

    string GetRJava()
    {
        if (string.IsNullOrEmpty(PlayerSettings.bundleIdentifier))
        {
            return string.Empty;
        }

        string path = string.Format("Assets/Plugins/Android/src/{0}/R.java", PlayerSettings.bundleIdentifier.Replace(".", "/"));
        FileInfo fi = new FileInfo(path);
        return fi.FullName;
    }

    string GetApplicationJava()
    {
        string path = "Assets/Plugins/Android/src/OuyaUnityActivity.java";
        FileInfo fi = new FileInfo(path);
        return fi.FullName;
    }

    string GetBundlePrefix()
    {
        string identifier = PlayerSettings.bundleIdentifier;
        if (string.IsNullOrEmpty(identifier))
        {
            return string.Empty;
        }

        foreach (string data in identifier.Split(".".ToCharArray()))
        {
            return data;
        }

        return string.Empty;
    }

    #endregion

    #region Android SDK

    public const string KEY_PATH_ANDROID_JAR = @"Android Jar";
    public const string KEY_PATH_ANDROID_ADB = @"ADB Path";
    public const string KEY_PATH_ANDROID_AAPT = @"APT Path";
    public const string KEY_PATH_ANDROID_SDK = @"SDK Path";

    public const string REL_ANDROID_PLATFORM_TOOLS = "platform-tools";
    public const string FILE_AAPT_WIN = "aapt.exe";
    public const string FILE_AAPT_MAC = "aapt";
    public const string FILE_ADB_WIN = "adb.exe";
    public const string FILE_ADB_MAC = "adb";

    public static string pathADB = string.Empty;
    public static string pathAAPT = string.Empty;
    public static string pathSDK = string.Empty;

    string GetPathAndroidJar()
    {
        return string.Format("{0}/platforms/android-{1}/android.jar", pathSDK, (int)PlayerSettings.Android.minSdkVersion);
    }

    void UpdateAndroidSDKPaths()
    {
        switch (Application.platform)
        {
            case RuntimePlatform.OSXEditor:
                pathADB = string.Format("{0}/{1}/{2}", pathSDK, REL_ANDROID_PLATFORM_TOOLS, FILE_ADB_MAC);
                pathAAPT = string.Format("{0}/{1}/{2}", pathSDK, REL_ANDROID_PLATFORM_TOOLS, FILE_AAPT_MAC);
                break;
            case RuntimePlatform.WindowsEditor:
                pathADB = string.Format("{0}/{1}/{2}", pathSDK, REL_ANDROID_PLATFORM_TOOLS, FILE_ADB_WIN);
                pathAAPT = string.Format("{0}/{1}/{2}", pathSDK, REL_ANDROID_PLATFORM_TOOLS, FILE_AAPT_WIN);
                break;
        }

        EditorPrefs.SetString(KEY_PATH_ANDROID_SDK, pathSDK);
    }

    void ResetAndroidSDKPaths()
    {
        switch (Application.platform)
        {
            case RuntimePlatform.OSXEditor:
                pathSDK = @"android-sdk-mac_x86";
                break;
            case RuntimePlatform.WindowsEditor:
                pathSDK = @"C:/Program Files (x86)/Android/android-sdk";
                break;
        }

        UpdateAndroidSDKPaths();
    }

    void SelectAndroidSDKPaths()
    {
        switch (Application.platform)
        {
            case RuntimePlatform.OSXEditor:
                pathSDK = EditorUtility.OpenFolderPanel(string.Format("Path to {0}", KEY_PATH_ANDROID_SDK), pathSDK, "../android-sdk-mac_x86");
                break;
            case RuntimePlatform.WindowsEditor:
                pathSDK = EditorUtility.OpenFolderPanel(string.Format("Path to {0}", KEY_PATH_ANDROID_SDK), pathSDK, @"..\android-sdk");
                break;
        }

        UpdateAndroidSDKPaths();
    }

    #endregion

    #region Android NDK

    private const string KEY_PATH_ANDROID_NDK = @"NDK Path";
    private const string KEY_PATH_ANDROID_NDK_MAKE = @"NDK Make";
    private const string KEY_PATH_ANDROID_JNI_MK = @"JNI mk";
    private const string KEY_PATH_ANDROID_JNI_CPP = @"JNI cpp";
    private const string KEY_PATH_OUYA_NDK_LIB = @"OUYA NDK Lib";

    public static string pathNDK = string.Empty;
    public static string pathNDKMake = string.Empty;
    public static string pathJNIMk = string.Empty;
    public static string pathJNIMkTemp = string.Empty;
    public static string pathJNICpp = string.Empty;
    public static string pathObj = string.Empty;
    public static string pathOuyaNDKLib = string.Empty;

    void UpdateAndroidNDKPaths()
    {
        pathObj = string.Format("{0}/Assets/Plugins/Android/obj", pathUnityProject);
        

        switch (Application.platform)
        {
            case RuntimePlatform.OSXEditor:
                pathNDKMake = string.Format("{0}/prebuilt/darwin-x86/bin/make", pathNDK);
                pathJNICpp = string.Format("{0}/Assets/Plugins/Android/jni/jni.cpp", pathUnityProject);
                pathOuyaNDKLib = string.Format("{0}/Assets/Plugins/Android/libs/armeabi/lib-ouya-ndk.so", pathUnityProject);
                break;
            case RuntimePlatform.WindowsEditor:
                pathNDKMake = string.Format("{0}/prebuilt/windows/bin/make.exe", pathNDK);
                pathJNICpp = string.Format("{0}/Assets/Plugins/Android/jni/jni.cpp", pathUnityProject);
                pathOuyaNDKLib = string.Format("{0}/Assets/Plugins/Android/libs/armeabi/lib-ouya-ndk.so", pathUnityProject);
                break;
        }

        EditorPrefs.SetString(KEY_PATH_ANDROID_NDK, pathNDK);
    }

    void ResetAndroidNDKPaths()
    {
        switch (Application.platform)
        {
            case RuntimePlatform.OSXEditor:
                pathNDK = @"android-ndk-r8c";
                break;
            case RuntimePlatform.WindowsEditor:
                pathNDK = @"android-ndk-r8c";
                break;
        }

        UpdateAndroidNDKPaths();
    }

    void SelectAndroidNDKPaths()
    {
        string path = string.Empty;
        switch (Application.platform)
        {
            case RuntimePlatform.OSXEditor:
                path = EditorUtility.OpenFolderPanel(string.Format("Path to {0}", KEY_PATH_ANDROID_NDK), pathNDK, "../android-ndk-r8c");
                break;
            case RuntimePlatform.WindowsEditor:
                path = EditorUtility.OpenFolderPanel(string.Format("Path to {0}", KEY_PATH_ANDROID_NDK), pathNDK, @"..\android-ndk-r8c");
                break;
        }
        if (!string.IsNullOrEmpty(path))
        {
            pathNDK = path;
            UpdateAndroidNDKPaths();
        }
    }

    #endregion

    #region Unity Paths

    public const string KEY_PATH_UNITY_JAR = @"Unity Jar";
    public const string KEY_PATH_UNITY_EDITOR = @"Unity Editor";
    public const string KEY_PATH_UNITY_PROJECT = @"Unity Project";

    public const string PATH_UNITY_JAR_WIN = "Data/PlaybackEngines/androidplayer/bin/classes.jar";
    public const string PATH_UNITY_JAR_MAC = "Unity.app/Contents/PlaybackEngines/AndroidPlayer/bin/classes.jar";

    private static string pathUnityJar = string.Empty;
    private static string pathUnityEditor = string.Empty;
    private static string pathUnityProject = string.Empty;

    void UpdateUnityPaths()
    {
        switch (Application.platform)
        {
            case RuntimePlatform.OSXEditor:
                pathUnityJar = string.Format("{0}/{1}", pathUnityEditor, PATH_UNITY_JAR_MAC);
                break;
            case RuntimePlatform.WindowsEditor:
                pathUnityJar = string.Format("{0}/{1}", pathUnityEditor, PATH_UNITY_JAR_WIN);
                break;
        }
    }

    #endregion

    #region Java JDK

    public const string KEY_PATH_JAVA_TOOLS_JAR = @"Tools Jar";
    public const string KEY_PATH_JAVA_JAR = @"Jar Path";
    public const string KEY_PATH_JAVA_JAVAC = @"JavaC Path";
    public const string KEY_PATH_JAVA_JAVAP = @"SDK Path";
    public const string KEY_PATH_JAVA_JDK = @"JDK Path";

    public const string REL_JAVA_PLATFORM_TOOLS = "bin";
    public const string FILE_JAR_WIN = "jar.exe";
    public const string FILE_JAR_MAC = "jar";
    public const string FILE_JAVAC_WIN = "javac.exe";
    public const string FILE_JAVAC_MAC = "javac";
    public const string FILE_JAVAP_WIN = "javap.exe";
    public const string FILE_JAVAP_MAC = "javap";

    public static string pathToolsJar = string.Empty;
    public static string pathJar = string.Empty;
    public static string pathJavaC = string.Empty;
    public static string pathJavaP = string.Empty;
    public static string pathJDK = string.Empty;

    void UpdateJavaJDKPaths()
    {
        switch (Application.platform)
        {
            case RuntimePlatform.OSXEditor:
                pathToolsJar = string.Format("{0}/Contents/Classes/classes.jar", pathJDK);
                pathJar = string.Format("{0}/Contents/Commands/{1}", pathJDK, FILE_JAR_MAC);
                pathJavaC = string.Format("{0}/Contents/Commands/{1}", pathJDK, FILE_JAVAC_MAC);
                pathJavaP = string.Format("{0}/Contents/Commands/{1}", pathJDK, FILE_JAVAP_MAC);
                break;
            case RuntimePlatform.WindowsEditor:
                pathToolsJar = string.Format("{0}/lib/tools.jar", pathJDK);
                pathJar = string.Format("{0}/{1}/{2}", pathJDK, REL_JAVA_PLATFORM_TOOLS, FILE_JAR_WIN);
                pathJavaC = string.Format("{0}/{1}/{2}", pathJDK, REL_JAVA_PLATFORM_TOOLS, FILE_JAVAC_WIN);
                pathJavaP = string.Format("{0}/{1}/{2}", pathJDK, REL_JAVA_PLATFORM_TOOLS, FILE_JAVAP_WIN);
                break;
        }

        EditorPrefs.SetString(KEY_PATH_JAVA_JDK, pathJDK);
    }

    void ResetJavaJDKPaths()
    {
        switch (Application.platform)
        {
            case RuntimePlatform.OSXEditor:
                pathJDK = @"/System/Library/Java/JavaVirtualMachines/1.6.0.jdk";
                break;
            case RuntimePlatform.WindowsEditor:
                pathJDK = @"C:\Program Files (x86)/Java/jdk1.6.0_37";
                break;
        }

        UpdateJavaJDKPaths();
    }

    void SelectJavaJDKPaths()
    {
        switch (Application.platform)
        {
            case RuntimePlatform.OSXEditor:
                pathJDK = EditorUtility.OpenFolderPanel(string.Format("Path to {0}", KEY_PATH_JAVA_JDK), pathJDK, "../jdk1.6.0_37");
                break;
            case RuntimePlatform.WindowsEditor:
                pathJDK = EditorUtility.OpenFolderPanel(string.Format("Path to {0}", KEY_PATH_JAVA_JDK), pathJDK, @"..\jdk1.6.0_37");
                break;
        }

        UpdateJavaJDKPaths();
    }

    #endregion

    [MenuItem("Window/Open Ouya Panel")]
    private static void MenuOpenPanel()
    {
        GetWindow<OuyaPanel>("Ouya Panel");
    }

    void OnEnable()
    {
        pathUnityEditor = new FileInfo(EditorApplication.applicationPath).Directory.FullName;
        pathUnityProject = new DirectoryInfo(Directory.GetCurrentDirectory()).FullName;
        UpdateUnityPaths();

        if (EditorPrefs.HasKey(KEY_PATH_ANDROID_SDK))
        {
            pathSDK = EditorPrefs.GetString(KEY_PATH_ANDROID_SDK);
        }

        if (string.IsNullOrEmpty(pathSDK))
        {
            ResetAndroidSDKPaths();
        }
        else
        {
            UpdateAndroidSDKPaths();
        }


        if (EditorPrefs.HasKey(KEY_PATH_ANDROID_NDK))
        {
            pathNDK = EditorPrefs.GetString(KEY_PATH_ANDROID_NDK);
        }

        if (string.IsNullOrEmpty(pathNDK))
        {
            ResetAndroidNDKPaths();
        }
        else
        {
            UpdateAndroidNDKPaths();
        }


        if (EditorPrefs.HasKey(KEY_PATH_JAVA_JDK))
        {
            pathJDK = EditorPrefs.GetString(KEY_PATH_JAVA_JDK);
        }

        if (string.IsNullOrEmpty(pathJDK))
        {
            ResetJavaJDKPaths();
        }
        else
        {
            UpdateJavaJDKPaths();
        }

        UpdateOuyaPaths();
    }

    void Update()
    {
        Repaint();

        if (m_toggleBuildApplication)
        {
            m_toggleBuildApplication = false;

            GenerateRJava();
            CompileApplicationClasses();
            BuildApplicationJar();

            AssetDatabase.Refresh();

            BuildPipeline.BuildPlayer(null, string.Format("{0}/OuyaUnityActivity.apk", pathUnityProject),
                                      BuildTarget.Android, BuildOptions.None);
        }

        if (m_toggleBuildAndRunApplication)
        {
            m_toggleBuildAndRunApplication = false;

            GenerateRJava();
            CompileApplicationClasses();
            BuildApplicationJar();

            AssetDatabase.Refresh();

            BuildPipeline.BuildPlayer(null, string.Format("{0}/OuyaUnityActivity.apk", pathUnityProject),
                                      BuildTarget.Android, BuildOptions.AutoRunPlayer);
        }

        if (m_toggleCompileJava)
        {
            m_toggleCompileJava = false;

            GenerateRJava();
            CompileApplicationClasses();
            BuildApplicationJar();

            AssetDatabase.Refresh();
        }
		

    }

    void GenerateRJava()
    {
        //clean meta files
        List<string> files = new List<string>();
        GetAssets("*.meta", files, new DirectoryInfo(pathRes.Replace(@"\", "/")));

        foreach (string meta in files)
        {
            //Debug.Log(meta);
            if (File.Exists(meta))
            {
                File.Delete(meta);
            }
        }
		
		if (!Directory.Exists(pathBin))
        {
            Directory.CreateDirectory(pathBin);
        }

        if (!Directory.Exists(pathBin))
        {
            Directory.CreateDirectory(pathBin);
        }

        if (Directory.Exists(pathBin))
        {
            Debug.Log(string.Format("Path exists: {0}", pathBin));
        }
        else
        {
            Debug.Log(string.Format("Path not exists: {0}", pathBin));
        }

        Thread.Sleep(100);

        RunProcess(pathAAPT, string.Format("package -v -f -m -J gen -M \"{0}\" -S \"{1}\" -I \"{2}\" -F \"{3}/resources.ap_\" -J \"{4}\"",
            pathManifestPath, pathRes, GetPathAndroidJar(), pathBin, pathSrc));

        string pathRJava = GetRJava();
        if (string.IsNullOrEmpty(pathRJava))
        {
            return;
        }
		if (File.Exists(pathRJava))
		{
	        using (FileStream fs = File.Open(pathRJava, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
	        {
	            using (StreamReader sr = new StreamReader(fs))
	            {
	                Debug.Log(sr.ReadToEnd());
	            }
	        }
		}

        if (Directory.Exists(pathBin))
        {
            Directory.Delete(pathBin, true);
        }
    }

    void CompileApplicationClasses()
    {
        string pathClasses = string.Format("{0}/Assets/Plugins/Android/Classes", pathUnityProject);
        if (!Directory.Exists(pathClasses))
        {
            Directory.CreateDirectory(pathClasses);
        }
        string pathRJava = GetRJava();
        if (string.IsNullOrEmpty(pathRJava))
        {
            return;
        }
        if (!File.Exists(pathRJava))
        {
            return;
        }
        string includeFiles = string.Format("\"{0}/OuyaUnityActivity.java\" \"{1}\"", pathSrc, pathRJava);
        string jars = string.Empty;
		
		if (File.Exists(pathToolsJar))
		{
			Debug.Log(string.Format("Found Java tools jar: {0}", pathToolsJar));
		}
		else
		{
			Debug.LogError(string.Format("Failed to find Java tools jar: {0}", pathToolsJar));
			return;
		}
		
		if (File.Exists(GetPathAndroidJar()))
		{
			Debug.Log(string.Format("Found Android jar: {0}", GetPathAndroidJar()));
		}
		else
		{
			Debug.LogError(string.Format("Failed to find Android jar: {0}", GetPathAndroidJar()));
			return;
		}
		
		if (File.Exists(pathGsonJar))
		{
			Debug.Log(string.Format("Found GJON jar: {0}", pathGsonJar));
		}
		else
		{
			Debug.LogError(string.Format("Failed to find GSON jar: {0}", pathGsonJar));
			return;
		}
		
		if (File.Exists(pathUnityJar))
		{
			Debug.Log(string.Format("Found Unity jar: {0}", pathUnityJar));
		}
		else
		{
			Debug.LogError(string.Format("Failed to find Unity jar: {0}", pathUnityJar));
			return;
		}
		
		
        switch (Application.platform)
        {
            case RuntimePlatform.OSXEditor:
                jars = string.Format("\"{0}:{1}:{2}:{3}:{4}\"", pathToolsJar, GetPathAndroidJar(), pathGsonJar, pathOuyaSDKJar, pathUnityJar);
			
				RunProcess(pathJavaC, string.Format("-g -source 1.6 -target 1.6 {0} -classpath {1} -bootclasspath {1} -d \"{2}\"",
		            includeFiles,
		            jars,
		            pathClasses));
                break;
            case RuntimePlatform.WindowsEditor:
                jars = string.Format("\"{0}\";\"{1}\";\"{2}\";\"{3}\";\"{4}\";", pathToolsJar, GetPathAndroidJar(), pathGsonJar, pathOuyaSDKJar, pathUnityJar);
			
				RunProcess(pathJavaC, string.Format("-g -source 1.6 -target 1.6 {0} -classpath {1} -bootclasspath {1} -d \"{2}\"",
		            includeFiles,
		            jars,
		            pathClasses));
                break;
        }
    }

    void BuildApplicationJar()
    {
        string pathClasses = string.Format("{0}/Assets/Plugins/Android/Classes", pathUnityProject);
        string bundlePrefix = GetBundlePrefix();
        if (string.IsNullOrEmpty(bundlePrefix))
        {
            return;
        }
        RunProcess(pathJar, pathClasses, string.Format("cvfM OuyaUnityActivity.jar {0}/", bundlePrefix));
        string pathAppJar = string.Format("{0}/OuyaUnityActivity.jar", pathClasses);
        string pathDest = string.Format("{0}/Assets/Plugins/Android/OuyaUnityActivity.jar", pathUnityProject);
        try
        {
            if (File.Exists(pathDest))
            {
                File.Delete(pathDest);
            }
            if (File.Exists(pathAppJar))
            {
                File.Move(pathAppJar, pathDest);
            }
            if (Directory.Exists(pathClasses))
            {
                Directory.Delete(pathClasses, true);
            }
        }
        catch (System.Exception)
        {
            
        }
    }

    void GUIDisplayFolder(string label, string path)
    {
        bool dirExists = Directory.Exists(path);

        if (!dirExists)
        {
            GUI.enabled = false;
        }
        GUILayout.BeginHorizontal(GUILayout.MaxWidth(position.width));
        GUILayout.Space(25);
        GUILayout.Label(string.Format("{0}:", label), GUILayout.Width(100));
        GUILayout.Space(5);
        GUILayout.Label(path.Replace("/", @"\"), EditorStyles.wordWrappedLabel, GUILayout.MaxWidth(position.width - 130));
        GUILayout.EndHorizontal();
        if (!dirExists)
        {
            GUI.enabled = true;
        }
    }

    void GUIDisplayFile(string label, string path)
    {
        bool fileExists = File.Exists(path);

        if (!fileExists)
        {
            GUI.enabled = false;
        }
        GUILayout.BeginHorizontal(GUILayout.MaxWidth(position.width));
        GUILayout.Space(25);
        GUILayout.Label(string.Format("{0}:", label), GUILayout.Width(100));
        GUILayout.Space(5);
        GUILayout.Label(path.Replace("/", @"\"), EditorStyles.wordWrappedLabel, GUILayout.MaxWidth(position.width - 130));
        GUILayout.EndHorizontal();
        if (!fileExists)
        {
            GUI.enabled = true;
        }
    }

    void GUIDisplayUnityFile(string label, string path)
    {
        bool fileExists = File.Exists(path);

        if (!fileExists)
        {
            GUI.enabled = false;
        }
        GUILayout.BeginHorizontal(GUILayout.MaxWidth(position.width));
        GUILayout.Space(25);
        GUILayout.Label(string.Format("{0}:", label), GUILayout.Width(100));
        GUILayout.Space(5);
        if (string.IsNullOrEmpty(path))
        {
            EditorGUILayout.ObjectField(string.Empty, null, typeof(UnityEngine.Object), false);
        }
        else
        {
            try
            {
                DirectoryInfo assets = new DirectoryInfo("Assets");
                Uri assetsUri = new Uri(assets.FullName);
                FileInfo fi = new FileInfo(path);
                string relativePath = assetsUri.MakeRelativeUri(new Uri(fi.FullName)).ToString();
                UnityEngine.Object fileRef = AssetDatabase.LoadAssetAtPath(relativePath, typeof (UnityEngine.Object));
                EditorGUILayout.ObjectField(string.Empty, fileRef, typeof (UnityEngine.Object), false);
            }
            catch (System.Exception)
            {
                Debug.LogError(string.Format("Path is invalid: label={0} path={1}", label, path));
            }
        }
        GUILayout.EndHorizontal();
        if (!fileExists)
        {
            GUI.enabled = true;
        }
    }

    private Vector2 m_scroll = Vector2.zero;

    void OnGUI()
    {
        m_scroll = GUILayout.BeginScrollView(m_scroll, GUILayout.MaxWidth(position.width));

        GUILayout.Label(string.Format("UID: {0}", UID));

        m_selectedToolSet = GUILayout.Toolbar(m_selectedToolSet, m_toolSets, GUILayout.MaxWidth(position.width));

        GUILayout.Space(20);

        switch (m_selectedToolSet)
        {
            case 0:

                if (GUILayout.Button("Build Application"))
                {
                    m_toggleBuildApplication = true;
                }

                if (GUILayout.Button("Build and Run Application"))
                {
                    m_toggleBuildAndRunApplication = true;
                }

                if (GUILayout.Button("Compile"))
                {
                    m_toggleCompileJava = true;
                }

                if (GUILayout.Button("Compile Java"))
                {
                    m_toggleCompileJava = true;
                }
			
			

                /*
                if (GUILayout.Button("Build Application Jar"))
                {
                    BuildApplicationJar();
                }

                if (GUILayout.Button("Compile Application Classes"))
                {
                    CompileApplicationClasses();
                }

                if (GUILayout.Button("Generate R.java from main layout"))
                {
                    GenerateRJava();
                }
                */ 

                GUILayout.Label("OUYA", EditorStyles.boldLabel);

                GUILayout.BeginHorizontal(GUILayout.MaxWidth(position.width));
                GUILayout.Space(25);
                GUILayout.Label(string.Format("{0}:", "Bundle Identifier"), GUILayout.Width(100));
                GUILayout.Space(5);
                PlayerSettings.bundleIdentifier = GUILayout.TextField(PlayerSettings.bundleIdentifier, EditorStyles.wordWrappedLabel, GUILayout.MaxWidth(position.width - 130));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal(GUILayout.MaxWidth(position.width));
                GUILayout.Space(25);
                GUILayout.Label(string.Format("{0}:", "Bundle Prefix"), GUILayout.Width(100));
                GUILayout.Space(5);
                GUILayout.Label(GetBundlePrefix(), EditorStyles.wordWrappedLabel, GUILayout.MaxWidth(position.width - 130));
                GUILayout.EndHorizontal();

                GameObject go = GameObject.Find("OuyaBridge");
                OuyaBridge ouyaGO = null;
                if (go)
                {
                    ouyaGO = go.GetComponent<OuyaBridge>();
                }
                if (null == ouyaGO)
                {
                    GUI.enabled = false;
                }
                GUILayout.BeginHorizontal(GUILayout.MaxWidth(position.width));
                GUILayout.Space(25);
                GUILayout.Label(string.Format("{0}:", "GameObject"), GUILayout.Width(100));
                GUILayout.Space(5);
                EditorGUILayout.ObjectField(string.Empty, ouyaGO, typeof (OuyaBridge), true);
                GUILayout.EndHorizontal();
                if (null == ouyaGO)
                {
                    GUI.enabled = true;
                }

                GUIDisplayUnityFile(KEY_PATH_OUYA_SDK, pathOuyaSDKJar);
                GUIDisplayUnityFile(KEY_PATH_JAR_GUAVA, pathGsonJar);
                GUIDisplayUnityFile(KEY_PATH_JAR_GSON, pathGuavaJar);
                
                GUIDisplayUnityFile("Manifest", pathManifestPath);
                GUIDisplayUnityFile("R.Java", GetRJava());
                GUIDisplayUnityFile("Application.Java", GetApplicationJava());
                //GUIDisplayFolder("Bin", pathBin);
                GUIDisplayFolder("Res", pathRes);
                GUIDisplayFolder("Src", pathSrc);
                
                break;
            case 1:
                GUILayout.Label("Unity Paths", EditorStyles.boldLabel);

                GUIDisplayFile(KEY_PATH_UNITY_JAR, pathUnityJar);
                GUIDisplayFolder(KEY_PATH_UNITY_EDITOR, pathUnityEditor);
                GUIDisplayFolder(KEY_PATH_UNITY_PROJECT, pathUnityProject);

                break;
            case 2:
                GUILayout.Label("Java JDK Paths", EditorStyles.boldLabel);

                GUIDisplayFile(KEY_PATH_JAVA_TOOLS_JAR, pathToolsJar);
                GUIDisplayFile(KEY_PATH_JAVA_JAR, pathJar);
                GUIDisplayFile(KEY_PATH_JAVA_JAVAC, pathJavaC);
                GUIDisplayFile(KEY_PATH_JAVA_JAVAP, pathJavaP);
                GUIDisplayFolder(KEY_PATH_JAVA_JDK, pathJDK);
                
                GUILayout.BeginHorizontal(GUILayout.MaxWidth(position.width));
                if (GUILayout.Button("Select SDK Path..."))
                {
                    SelectJavaJDKPaths();
                }
                if (GUILayout.Button("Reset Paths"))
                {
                    ResetJavaJDKPaths();
                }

                GUILayout.EndHorizontal();
                break;
            case 3:
                GUILayout.Label("Android SDK", EditorStyles.boldLabel);

                GUILayout.BeginHorizontal(GUILayout.MaxWidth(position.width));
                GUILayout.Space(25);
                GUILayout.Label(string.Format("{0}:", "minSDKVersion"), GUILayout.Width(100));
                GUILayout.Space(5);
                GUILayout.Label(((int)(PlayerSettings.Android.minSdkVersion)).ToString(), EditorStyles.wordWrappedLabel, GUILayout.MaxWidth(position.width - 130));
                GUILayout.EndHorizontal();

                GUIDisplayFile(KEY_PATH_ANDROID_JAR, GetPathAndroidJar());
                GUIDisplayFile(KEY_PATH_ANDROID_ADB, pathADB);
                GUIDisplayFile(KEY_PATH_ANDROID_AAPT, pathAAPT);
                GUIDisplayFolder(KEY_PATH_ANDROID_SDK, pathSDK);

                GUILayout.BeginHorizontal(GUILayout.MaxWidth(position.width));
                if (GUILayout.Button("Select SDK Path..."))
                {
                    SelectAndroidSDKPaths();
                }
                if (GUILayout.Button("Reset Paths"))
                {
                    ResetAndroidSDKPaths();
                }
                GUILayout.EndHorizontal();

                break;

        }
        
        GUILayout.EndScrollView();
    }

    #region RUN PROCESS
    public static void RunProcess(string path, string arguments)
    {
        string error = string.Empty;
        string output = string.Empty;
        RunProcess(path, string.Empty, arguments, ref output, ref error);
    }

    public static void RunProcess(string path, string workingDirectory, string arguments)
    {
        string error = string.Empty;
        string output = string.Empty;
        RunProcess(path, workingDirectory, arguments, ref output, ref error);
    }

    public static void RunProcess(string path, string workingDirectory, string arguments, ref string output, ref string error)
    {
        try
        {
            System.Diagnostics.Process process = new System.Diagnostics.Process();
            process.StartInfo.Arguments = arguments;
            process.StartInfo.WorkingDirectory = workingDirectory;
            process.StartInfo.FileName = path;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.ErrorDialog = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardInput = true;
            process.StartInfo.RedirectStandardError = true;
            DateTime startTime = DateTime.Now;
            Debug.Log(string.Format("[Running Process] filename={0} arguments={1}", process.StartInfo.FileName,
                                    process.StartInfo.Arguments));

            process.Start();

            output = process.StandardOutput.ReadToEnd();
            error = process.StandardError.ReadToEnd();

            float elapsed = (float)(DateTime.Now - startTime).TotalSeconds;
            Debug.Log(string.Format("[Results] elapsedTime: {3} errors: {2}\noutput: {1}", process.StartInfo.FileName,
                                    output, error, elapsed));

            //if (output.Length > 0 ) Debug.Log("Output: " + output);
            //if (error.Length > 0 ) Debug.Log("Error: " + error); 
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning(string.Format("Unable to run process: path={0} arguments={1} exception={2}", path, arguments, ex));
        }

    }
    #endregion

    #region File IO

    private static void GetAssets(string extension, List<string> files, DirectoryInfo directory)
    {
        if (null == directory)
        {
            return;
        }
        foreach (FileInfo file in directory.GetFiles(extension))
        {
            if (string.IsNullOrEmpty(file.FullName) ||
                files.Contains(file.FullName))
            {
                continue;
            }
            files.Add(file.FullName);
            //Debug.Log(string.Format("File: {0}", file.FullName));
        }
        foreach (DirectoryInfo subDir in directory.GetDirectories())
        {
            if (null == subDir)
            {
                continue;
            }
            //Debug.Log(string.Format("Directory: {0}", subDir));
            GetAssets(extension, files, subDir);
        }
    }

    #endregion

    #region Unique identification

    public static string UID = GetUID();

    /// <summary>
    /// Get the machine name
    /// </summary>
    /// <returns></returns>
    private static string GetMachineName()
    {
        try
        {
            string machineName = System.Environment.MachineName;
            if (!string.IsNullOrEmpty(machineName))
            {
                return machineName;
            }
        }
        catch (System.Exception)
        {
            Debug.LogError("GetMachineName: Failed to get machine name");
        }

        return "Unknown";
    }

    /// <summary>
    /// Get the IDE process IDE
    /// </summary>
    /// <returns></returns>
    private static int GetProcessID()
    {
        try
        {
            Process process = Process.GetCurrentProcess();
            if (null != process)
            {
                return process.Id;
            }
        }
        catch
        {
            Debug.LogError("GetProcessID: Failed to get process id");
        }

        return 0;
    }

    /// <summary>
    /// Get a unique identifier for the Unity instance
    /// </summary>
    /// <returns></returns>
    private static string GetUID()
    {
        try
        {
            return string.Format("{0}_{1}", GetMachineName(), GetProcessID());
        }
        catch (System.Exception)
        {
            Debug.LogError("GetUID: Failed to create uid");
        }

        return string.Empty;
    }

    #endregion
}