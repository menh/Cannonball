using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Jingzhou.Battle.EditorTools
{
    public static class JingzhouFontSetup
    {
        const string FontPath = "Assets/Resources/Fonts/JingzhouFont.ttf";
        const string ScenePath = "Assets/Scenes/JingzhouBattle.unity";

        [InitializeOnLoadMethod]
        static void Hook()
        {
            EditorApplication.delayCall += EnsureDynamicFont;
            EditorApplication.delayCall += OpenBattleIfUntitled;
        }

        static void OpenBattleIfUntitled()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;
            if (SessionState.GetBool("JingzhouSceneOpened", false))
                return;
            Scene active = SceneManager.GetActiveScene();
            if (!string.IsNullOrEmpty(active.path))
                return;
            if (!System.IO.File.Exists(ScenePath))
                return;
            SessionState.SetBool("JingzhouSceneOpened", true);
            try
            {
                EditorSceneManager.OpenScene(ScenePath);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("Could not open the Jingzhou scene: " + ex.Message);
            }
        }

        static void EnsureDynamicFont()
        {
            TrueTypeFontImporter importer = AssetImporter.GetAtPath(FontPath) as TrueTypeFontImporter;
            if (importer == null)
                return;
            if (importer.fontTextureCase == FontTextureCase.Dynamic && importer.includeFontData)
                return;
            importer.fontTextureCase = FontTextureCase.Dynamic;
            importer.includeFontData = true;
            importer.SaveAndReimport();
        }
    }
}
