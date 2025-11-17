using UnityEditor;
using UnityEngine;

namespace Core
{
    /// <summary>
    /// Scene generic authoring/reference collection. Can also contain the settings for booting up a particular scene
    /// </summary>
    public class SceneReference : MonoBehaviour
    {
        public ApplicationState applicationState = ApplicationState.Invalid;
        public GameMode gameMode;
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(SceneReference))]
    public class SceneAuthoringEditor : Editor
    {
        private SceneReference sceneReference;

        private SerializedProperty appState;
        private SerializedProperty gameMode;

        private void OnEnable()
        {
            sceneReference = (SceneReference)target;
            gameMode = serializedObject.FindProperty("gameMode");
            appState = serializedObject.FindProperty("applicationState");
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.PropertyField(appState, new GUIContent("Application State"));
            if (sceneReference.applicationState == ApplicationState.GameMode)
            {
                EditorGUILayout.PropertyField(gameMode, new GUIContent("Game Mode"));
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
#endif
}
