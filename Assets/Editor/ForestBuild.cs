using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using ForestCleanup;
public static class ForestBuild {
    [MenuItem("Forest Cleanup/Create or refresh game scene")]
    public static void CreateScene() {
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var material=AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/ForestBase.mat");
        if(material==null) {material=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(material,"Assets/Resources/ForestBase.mat");}
        var world=new GameObject("Forest Cleanup").AddComponent<ForestWorld>();world.baseMaterial=material;
        EditorSceneManager.SaveScene(scene,"Assets/Scenes/ForestCleanup.unity");
        EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/Scenes/ForestCleanup.unity",true)};
        PlayerSettings.productName="Forest Cleanup";PlayerSettings.companyName="Forest Night";
        PlayerSettings.defaultScreenWidth=1280;PlayerSettings.defaultScreenHeight=720;PlayerSettings.fullScreenMode=FullScreenMode.Windowed;
        PlayerSettings.runInBackground=true;
        AssetDatabase.SaveAssets();
    }
    public static void Build() {
        if (!System.IO.File.Exists("Assets/Scenes/ForestCleanup.unity")) CreateScene();
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes=new[]{"Assets/Scenes/ForestCleanup.unity"},locationPathName="Builds/Windows/ForestCleanup.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development });
        if(report.summary.result!=BuildResult.Succeeded)throw new System.Exception("Build failed: "+report.summary.result);
        Debug.Log("FOREST_BUILD_PASS");
    }
}

