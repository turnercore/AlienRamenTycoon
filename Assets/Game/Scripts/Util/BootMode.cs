using System;
using UnityEditor;
using UnityEngine;

namespace Project
{
#if UNITY_EDITOR
    public enum BootType
    {
        FullBoot,
        SceneBoot,
        UnityDefault,
    }

    /// <summary>
    /// Allows the user to set the boot mode for the project which will be used by the EntryPoint to mark if the startup scene should be loaded or not
    /// </summary>
    [ExecuteInEditMode]
    public class BootMode : MonoBehaviour
    {
        public static BootType BootType
        {
            get => (BootType)EditorPrefs.GetInt("bootType");
            private set
            {
                EditorPrefs.SetInt("bootType", (int)value);
                Debug.Log($"Boot mode {BootType} set...");
            }
        }

        private void OnEnable()
        {
            if (!EditorPrefs.HasKey("bootType"))
            {
                BootType = BootType.FullBoot;
            }
        }

        [MenuItem("Project/&Boot/&Full Boot...", false)]
        private static void SetFullBoot()
        {
            BootType = BootType.FullBoot;
        }

        [MenuItem("Project/&Boot/&Scene Boot...", false)]
        private static void SetSceneBoot()
        {
            BootType = BootType.SceneBoot;
        }

        [MenuItem("Project/&Boot/&Unity Default No Boot...", false)]
        private static void SetDefaultBoot()
        {
            BootType = BootType.UnityDefault;
        }
    }
#endif
}
