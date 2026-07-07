using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public class BuildScript
{
    private const string OutputPath = "Builds/Android/WordPuzzle.apk";

    public static void BuildAndroid()
    {
        // 빌드 출력 폴더 생성
        string dir = Path.GetDirectoryName(OutputPath);
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        // Build Settings에서 enabled 씬 수집
        string[] scenes = EditorBuildSettings.scenes
            .Where(s => s.enabled)
            .Select(s => s.path)
            .ToArray();

        if (scenes.Length == 0)
        {
            Debug.LogError("[BuildScript] Build Settings에 enabled된 씬이 없습니다. 빌드를 중단합니다.");
            throw new Exception("No enabled scenes in Build Settings.");
        }

        Debug.Log("[BuildScript] 빌드할 씬 목록:");
        foreach (string s in scenes)
            Debug.Log("  " + s);

        // Android 빌드 타겟으로 전환
        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);

        var options = new BuildPlayerOptions
        {
            scenes           = scenes,
            locationPathName = OutputPath,
            target           = BuildTarget.Android,
            options          = BuildOptions.None,
        };

        BuildReport  report  = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log("[BuildScript] 빌드 성공: " + summary.outputPath);
        }
        else
        {
            throw new Exception(
                "[BuildScript] 빌드 실패 — result: " + summary.result +
                ", errors: " + summary.totalErrors);
        }
    }
}
