using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEditor;
using TMPro;
using System.IO;

namespace FWTCG.Editor
{
    /// <summary>
    /// Copies a system Chinese font (Microsoft YaHei) into the project,
    /// creates a Dynamic TMP Font Asset from it, and sets it as the
    /// TMP global default so all TextMeshProUGUI components render Chinese.
    /// Run via: FWTCG / Setup Chinese Font   -or-   batch -executeMethod
    /// </summary>
    public static class FontSetup
    {
        [MenuItem("FWTCG/Setup Chinese Font")]
        public static void SetupChineseFont()
        {
            // ── 1. Find a system Chinese font ─────────────────────────────────
            string[] candidates = new[]
            {
                @"C:\Windows\Fonts\msyh.ttc",
                @"C:\Windows\Fonts\msyhbd.ttc",
                @"C:\Windows\Fonts\simsun.ttc",
                @"C:\Windows\Fonts\simhei.ttf",
                @"C:\Windows\Fonts\simkai.ttf",
            };

            string srcPath = null;
            foreach (string c in candidates)
            {
                if (File.Exists(c)) { srcPath = c; break; }
            }

            if (srcPath == null)
            {
                Debug.LogError("[FontSetup] 未找到任何中文字体文件，请手动导入。");
                return;
            }

            Debug.Log($"[FontSetup] 使用字体：{srcPath}");

            // ── 2. Copy font into Assets/Fonts/ ───────────────────────────────
            const string destDir = "Assets/Fonts";
            if (!Directory.Exists(destDir))
                Directory.CreateDirectory(destDir);

            string fileName    = Path.GetFileName(srcPath);
            string destPath    = $"{destDir}/{fileName}";

            if (!File.Exists(destPath))
                File.Copy(srcPath, destPath, overwrite: true);

            AssetDatabase.ImportAsset(destPath, ImportAssetOptions.ForceUpdate);
            AssetDatabase.Refresh();

            // ── 3. Load the Unity Font object ─────────────────────────────────
            Font font = AssetDatabase.LoadAssetAtPath<Font>(destPath);
            if (font == null)
            {
                Debug.LogError($"[FontSetup] 加载字体失败：{destPath}");
                return;
            }

            // ── 4. Create Dynamic TMP Font Asset ──────────────────────────────
            //    AtlasPopulationMode.Dynamic = glyphs baked at runtime,
            //    no pre-render needed → safe in batch mode.
            TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(
                font,
                samplingPointSize:    90,
                atlasPadding:          9,
                renderMode:            GlyphRenderMode.SDFAA,
                atlasWidth:          1024,
                atlasHeight:         1024,
                atlasPopulationMode: AtlasPopulationMode.Dynamic,
                enableMultiAtlasSupport: true);

            if (fontAsset == null)
            {
                Debug.LogError("[FontSetup] TMP_FontAsset.CreateFontAsset 返回 null！");
                return;
            }

            // ── 5. Save Font Asset to disk ────────────────────────────────────
            string fontAssetPath = $"{destDir}/ChineseFont SDF.asset";
            if (File.Exists(fontAssetPath))
                AssetDatabase.DeleteAsset(fontAssetPath);

            AssetDatabase.CreateAsset(fontAsset, fontAssetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontAssetPath);
            if (fontAsset == null)
            {
                Debug.LogError($"[FontSetup] 保存后重新加载 Font Asset 失败：{fontAssetPath}");
                return;
            }

            // ── 6. Set as TMP global default font ─────────────────────────────
            TMP_Settings tmpSettings = Resources.Load<TMP_Settings>("TMP Settings");
            if (tmpSettings == null)
            {
                // Try finding it by path in case Resources.Load doesn't work in batch
                string[] guids = AssetDatabase.FindAssets("TMP Settings t:TMP_Settings");
                if (guids.Length > 0)
                {
                    string p = AssetDatabase.GUIDToAssetPath(guids[0]);
                    tmpSettings = AssetDatabase.LoadAssetAtPath<TMP_Settings>(p);
                }
            }

            if (tmpSettings != null)
            {
                SerializedObject so = new SerializedObject(tmpSettings);
                so.Update();

                SerializedProperty prop = so.FindProperty("m_defaultFontAsset");
                if (prop != null)
                {
                    prop.objectReferenceValue = fontAsset;
                    so.ApplyModifiedProperties();
                    EditorUtility.SetDirty(tmpSettings);
                    AssetDatabase.SaveAssets();
                    Debug.Log($"[FontSetup] ✅ 中文字体设置完成！默认字体 → {fontAssetPath}");
                }
                else
                {
                    Debug.LogWarning("[FontSetup] 找不到 TMP Settings.m_defaultFontAsset 字段。");
                }
            }
            else
            {
                Debug.LogWarning("[FontSetup] 未找到 TMP Settings asset。请在 Edit → Project Settings → TextMeshPro 手动指定默认字体。");
                Debug.Log($"[FontSetup] Font Asset 已保存到：{fontAssetPath}");
            }
        }
    }
}
