using System.IO;
using UnityEditor;
using UnityEngine;

// Polls BlenderSrc/unity_job.txt and runs build steps inside the Editor (no CLI / keychain involved).
[InitializeOnLoad]
public static class JobWatcher
{
    const string JobPath = "BlenderSrc/unity_job.txt";
    const string OutPath = "BlenderSrc/unity_out.txt";
    static double next;
    static System.DateTime last;

    static JobWatcher()
    {
        if (File.Exists(JobPath)) last = File.GetLastWriteTimeUtc(JobPath);
        EditorApplication.update += Tick;
    }

    static void Tick()
    {
        if (EditorApplication.timeSinceStartup < next) return;
        next = EditorApplication.timeSinceStartup + 1.0;
        if (!File.Exists(JobPath)) return;
        var t = File.GetLastWriteTimeUtc(JobPath);
        if (t == last) return;
        last = t;
        var log = new System.Text.StringBuilder();
        foreach (var raw in File.ReadAllLines(JobPath))
        {
            var line = raw.Trim(); if (line.Length == 0 || line.StartsWith("#")) continue;
            var p = line.Split(' ');
            try
            {
                switch (p[0])
                {
                    case "refresh": AssetDatabase.Refresh(); log.AppendLine("refreshed"); break;
                    case "step": log.AppendLine(PlazaBuilder.Queue(p[1])); log.AppendLine(File.ReadAllText(OutPath)); break;
                    case "capture": log.AppendLine(Capture(p[1], int.Parse(p[2]), int.Parse(p[3]))); break;
                    default: log.AppendLine("unknown " + line); break;
                }
            }
            catch (System.Exception e) { log.AppendLine("FAIL " + line + "\n" + e); }
        }
        File.WriteAllText(OutPath, "JOB " + t.ToString("o") + "\n" + log);
    }

    public static string Capture(string path, int w, int h)
    {
        var cam = Camera.main;
        if (cam == null) return "no main camera";
        foreach (var ps in Object.FindObjectsByType<ParticleSystem>()) ps.Simulate(4f, true, true);
        var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
        var prev = cam.targetTexture;
        cam.targetTexture = rt; cam.Render(); cam.targetTexture = prev;
        RenderTexture.active = rt;
        var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, w, h), 0, 0); tex.Apply();
        RenderTexture.active = null;
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex); rt.Release(); Object.DestroyImmediate(rt);
        return "captured " + path;
    }
}
