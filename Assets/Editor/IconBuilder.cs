using System.Collections.Generic;
using System.IO;
using Argensimia.UI;
using TMPro;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

// Ícono de la app: la "A" del logo con la bandera argentina (UiKit.LogoFlagMat), contorno azul y sombra dura,
// sobre el Sol de Mayo y fondo azul noche. Se renderiza en Play (URP solo dibuja cámaras con el loop del juego)
// a un PNG de 1024 px y queda como ícono por defecto en Player Settings. Si no estás en Play, entra, lo genera y sale.
[InitializeOnLoad]
public static class IconBuilder
{
    const string Dir = "Assets/Art/Icon/";
    const string PathPng = Dir + "AppIcon.png";
    const string KeyPending = "Argensimia.IconPending", KeyAutoExit = "Argensimia.IconAutoExit";
    const int S = 1024;
    const int Layer = 31;

    static IconBuilder()
    {
        EditorApplication.playModeStateChanged += st =>
        {
            if (st == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(KeyPending, false))
            {
                SessionState.SetBool(KeyPending, false);
                Capture();
            }
        };
    }

    [MenuItem("Argensimia/7. Generate App Icon")]
    public static string Build()
    {
        if (Application.isPlaying) { Capture(); return "generando"; }
        SessionState.SetBool(KeyPending, true);
        SessionState.SetBool(KeyAutoExit, true);
        EditorApplication.isPlaying = true;
        return "entrando a Play para generar el ícono";
    }

    static void Capture()
    {
        Directory.CreateDirectory(Dir);
        var disabled = DisableFullScreenFeatures();
        var rt = new RenderTexture(S, S, 24, RenderTextureFormat.ARGB32) { antiAliasing = 1 };
        var camGo = new GameObject("IconCam");
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = UiKit.NavyDark;
        cam.orthographic = true;
        cam.cullingMask = 1 << Layer;
        cam.targetTexture = rt;
        cam.depth = -100;
        cam.transform.position = new Vector3(0, -5000, 0);
        var acd = camGo.AddComponent<UniversalAdditionalCameraData>();
        acd.renderPostProcessing = false;
        acd.renderShadows = false;

        var cgo = new GameObject("IconCanvas", typeof(RectTransform)) { layer = Layer };
        var canvas = cgo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = cam;
        canvas.planeDistance = 10;
        var scaler = cgo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(S, S);
        var root = (RectTransform)cgo.transform;

        UiKit.Img(root, "Bg", null, UiKit.NavyDark).RT().Fill();
        UiKit.Img(root, "Halo", UiKit.Circle, new Color(0.25f, 0.33f, 0.6f, 0.55f)).RT().At(new Vector2(0.5f, 0.5f), new Vector2(0, 10), new Vector2(980, 980));
        var sun = UiKit.Rect("Sun", root).At(new Vector2(0.5f, 0.5f), new Vector2(0, 40), new Vector2(10, 10));
        for (int i = 0; i < 32; i++)
        {
            bool lng = i % 2 == 0;
            var ray = UiKit.Img(sun, "Ray", null, UiKit.Hex(lng ? "#f6b40e" : "#e89b0c")).RT();
            ray.At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(lng ? 34 : 22, lng ? 470 : 400), new Vector2(0.5f, 0));
            ray.localRotation = Quaternion.Euler(0, 0, i * 360f / 32);
        }
        var disc = UiKit.Img(sun, "Disc", UiKit.Circle, UiKit.Hex("#c98a12"));
        disc.RT().At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(470, 470));
        UiKit.Img(disc.transform, "In", UiKit.Circle, UiKit.Hex("#f6b40e")).RT().Fill(26, 26, 26, 26);
        var a = UiKit.LogoPart(root, "A", 880, true, 0);
        Object.Destroy(a.GetComponent<LogoWave>());
        a.alignment = TextAlignmentOptions.Center;
        a.RT().At(new Vector2(0.5f, 0.5f), new Vector2(0, -40), new Vector2(S, S));
        foreach (var t in root.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = Layer;

        int frames = 0;
        void Step()
        {
            if (++frames < 4) return; // que el canvas y la cámara rindan al menos un par de cuadros
            EditorApplication.update -= Step;
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(S, S, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, S, S), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;
            File.WriteAllBytes(PathPng, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            Object.Destroy(cgo);
            Object.Destroy(camGo);
            rt.Release();
            foreach (var f in disabled) if (f) f.SetActive(true);

            AssetDatabase.ImportAsset(PathPng);
            var imp = (TextureImporter)AssetImporter.GetAtPath(PathPng);
            imp.textureType = TextureImporterType.Default;
            imp.mipmapEnabled = false;
            imp.alphaIsTransparency = false;
            imp.maxTextureSize = 1024;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.SaveAndReimport();
            PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { AssetDatabase.LoadAssetAtPath<Texture2D>(PathPng) }, IconKind.Any);
            AssetDatabase.SaveAssets();
            Debug.Log("Ícono generado: " + PathPng);
            if (SessionState.GetBool(KeyAutoExit, false)) { SessionState.SetBool(KeyAutoExit, false); EditorApplication.isPlaying = false; }
        }
        EditorApplication.update += Step;
    }

    // El tilt-shift y el blur del menú son Full Screen Pass: se apagan mientras se renderiza el ícono.
    static List<ScriptableRendererFeature> DisableFullScreenFeatures()
    {
        var off = new List<ScriptableRendererFeature>();
        var urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        if (!urp) return off;
        var fld = typeof(UniversalRenderPipelineAsset).GetField("m_RendererDataList", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (!(fld?.GetValue(urp) is ScriptableRendererData[] list)) return off;
        foreach (var data in list)
        {
            if (!data) continue;
            foreach (var f in data.rendererFeatures)
                if (f && f.isActive && f is FullScreenPassRendererFeature) { f.SetActive(false); off.Add(f); }
        }
        return off;
    }
}
