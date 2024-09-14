using UnityEngine;
using UnityEditor;
using System.IO;

#if UNITY_EDITOR
public class MergeMapsWindow : EditorWindow
{
    private Texture2D metallicMap;
    private Texture2D smoothnessMap;
    private bool invertSmoothness;

    [MenuItem("Loki/Metallic and Smoothness Merger")]
    public static void ShowWindow()
    {
        GetWindow<MergeMapsWindow>("Metallic and Smoothness Merger");
    }

    private void OnGUI()
    {
        GUILayout.Label("Metallic and Smoothness Merger", EditorStyles.boldLabel);

        metallicMap = (Texture2D)EditorGUILayout.ObjectField("Metallic Map", metallicMap, typeof(Texture2D), false);
        smoothnessMap = (Texture2D)EditorGUILayout.ObjectField("Smoothness Map", smoothnessMap, typeof(Texture2D), false);
        invertSmoothness = EditorGUILayout.Toggle("Invert Smoothness Map", invertSmoothness);

        if (GUILayout.Button("Merge"))
        {
            MergeMaps();
        }
    }

    private void MergeMaps()
    {
        if (metallicMap == null || smoothnessMap == null)
        {
            Debug.LogError("Please assign both the metallic map and the smoothness map.");
            return;
        }

        Texture2D metallicMapCopy = GetReadableTexture(metallicMap);
        Texture2D smoothnessMapCopy = GetReadableTexture(smoothnessMap);

        int width = Mathf.Max(metallicMapCopy.width, smoothnessMapCopy.width);
        int height = Mathf.Max(metallicMapCopy.height, smoothnessMapCopy.height);

        Texture2D resizedMetallicMap = ResizeTexture(metallicMapCopy, width, height);
        Texture2D resizedSmoothnessMap = ResizeTexture(smoothnessMapCopy, width, height);

        Texture2D result = new Texture2D(width, height, TextureFormat.RGBA32, false);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Color metallicColor = resizedMetallicMap.GetPixel(x, y);
                Color smoothnessColor = resizedSmoothnessMap.GetPixel(x, y);
                float alpha = invertSmoothness ? 1.0f - smoothnessColor.r : smoothnessColor.r;
                result.SetPixel(x, y, new Color(metallicColor.r, metallicColor.g, metallicColor.b, alpha));
            }
        }

        result.Apply();

        byte[] bytes = result.EncodeToPNG();
        string metallicMapPath = AssetDatabase.GetAssetPath(metallicMap);
        string directory = Path.GetDirectoryName(metallicMapPath);
        string path = Path.Combine(directory, "MergedMSMap.png");

        File.WriteAllBytes(path, bytes);
        AssetDatabase.Refresh();
        Debug.Log("Merged map saved to: " + path);
    }

    private Texture2D GetReadableTexture(Texture2D texture)
    {
        string path = AssetDatabase.GetAssetPath(texture);
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;

        if (importer != null && !importer.isReadable)
        {
            importer.isReadable = true;
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        }

        Texture2D readableTexture = new Texture2D(texture.width, texture.height, texture.format, texture.mipmapCount > 1);
        Graphics.CopyTexture(texture, readableTexture);

        if (importer != null && importer.isReadable)
        {
            importer.isReadable = false;
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        }

        return readableTexture;
    }

    private Texture2D ResizeTexture(Texture2D source, int newWidth, int newHeight)
    {
        RenderTexture rt = RenderTexture.GetTemporary(newWidth, newHeight);
        rt.filterMode = FilterMode.Bilinear;
        RenderTexture.active = rt;
        Graphics.Blit(source, rt);
        Texture2D result = new Texture2D(newWidth, newHeight);
        result.ReadPixels(new Rect(0, 0, newWidth, newHeight), 0, 0);
        result.Apply();
        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(rt);
        return result;
    }
}
#endif