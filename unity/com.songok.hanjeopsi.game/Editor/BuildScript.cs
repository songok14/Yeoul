using System;
using System.IO;
using Hanjeopsi.Game.DebugTools;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Hanjeopsi.EditorTools
{
    /// <summary>
    /// 안드로이드 테스트 APK 빌드. GitHub Actions(GameCI)가 -executeMethod 로 부르고,
    /// 에디터에서는 메뉴 "한접시"에서 부른다. 씬과 플레이어 설정을 코드로 만들어서
    /// 빈 프로젝트에서도 같은 APK가 나온다.
    /// </summary>
    public static class BuildScript
    {
        public const string LabScene = "Assets/Scenes/CoreLab.unity";
        private const string Keystore = "Keystore/ci-test.keystore";
        private const string KeystorePass = "hanjeopsi-test";
        private const string KeyAlias = "hanjeopsi";

        [MenuItem("한접시/점검 씬 만들기")]
        public static void MakeLabScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cam = new GameObject("Main Camera");
            cam.tag = "MainCamera";
            var c = cam.AddComponent<Camera>();
            c.orthographic = true;
            c.clearFlags = CameraClearFlags.SolidColor;
            c.backgroundColor = new Color(0.106f, 0.165f, 0.255f);
            new GameObject("CoreLab").AddComponent<CoreLab>();
            Directory.CreateDirectory("Assets/Scenes");
            EditorSceneManager.SaveScene(scene, LabScene);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(LabScene, true) };
            Debug.Log("점검 씬을 만들었다: " + LabScene);
        }

        [MenuItem("한접시/안드로이드 APK 빌드")]
        public static void BuildAndroidMenu()
        {
            BuildAndroid();
        }

        /// <summary>GameCI buildMethod. -customBuildPath 로 받은 곳에 APK를 만든다</summary>
        public static void BuildAndroid()
        {
            try
            {
                MakeLabScene();
                ConfigurePlayer();
                string output = Arg("-customBuildPath") ?? "Builds/Android/hanjeopsi.apk";
                if (!output.EndsWith(".apk", StringComparison.OrdinalIgnoreCase)) output = Path.Combine(output, "hanjeopsi.apk");
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output)));

                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = new[] { LabScene },
                    locationPathName = output,
                    target = BuildTarget.Android,
                    targetGroup = BuildTargetGroup.Android,
                    options = BuildOptions.None,
                });
                var sum = report.summary;
                Debug.Log("빌드 결과: " + sum.result + ", 크기 " + (sum.totalSize / 1024 / 1024) + "MB, 시간 " + sum.totalTime + ", 경로 " + output);
                Finish(sum.result == BuildResult.Succeeded);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                Finish(false);
            }
        }

        private static void ConfigurePlayer()
        {
#if HANJEOPSI_UNITY_CHECK
            // dotnet 점검 컴파일(Unity 2021.1 참조본)에는 NamedBuildTarget 오버로드가 없다
            var android = BuildTargetGroup.Android;
            const ManagedStrippingLevel minimal = ManagedStrippingLevel.Low;
#else
            var android = NamedBuildTarget.Android;
            const ManagedStrippingLevel minimal = ManagedStrippingLevel.Minimal;
#endif
            string run = Environment.GetEnvironmentVariable("GITHUB_RUN_NUMBER");
            int code;
            if (!int.TryParse(run, out code)) code = 1;

            PlayerSettings.companyName = "songok";
            PlayerSettings.productName = "한 접시의 바다";
            PlayerSettings.SetApplicationIdentifier(android, "com.songok.hanjeopsi");
            PlayerSettings.bundleVersion = "0.2." + code;
            PlayerSettings.Android.bundleVersionCode = code;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.SetScriptingBackend(android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.SetManagedStrippingLevel(android, minimal);
            EditorUserBuildSettings.buildAppBundle = false;

            // 매번 같은 키로 서명해야 폰에서 덮어 설치된다 (테스트 전용 키)
            PlayerSettings.Android.useCustomKeystore = true;
            PlayerSettings.Android.keystoreName = Path.GetFullPath(Keystore);
            PlayerSettings.Android.keystorePass = KeystorePass;
            PlayerSettings.Android.keyaliasName = KeyAlias;
            PlayerSettings.Android.keyaliasPass = KeystorePass;
        }

        private static string Arg(string name)
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == name) return args[i + 1];
            return null;
        }

        private static void Finish(bool ok)
        {
            if (Application.isBatchMode) EditorApplication.Exit(ok ? 0 : 1);
            else if (!ok) Debug.LogError("빌드 실패. 위 Console 오류를 확인한다");
        }
    }
}
