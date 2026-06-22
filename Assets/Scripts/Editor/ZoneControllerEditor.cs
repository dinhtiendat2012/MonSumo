using UnityEditor;
using UnityEngine;
using MonSumo.World.Zone;

namespace MonSumo.Editor
{
    [CustomEditor(typeof(ZoneController))]
    public class ZoneControllerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            // Draw standard fields
            DrawDefaultInspector();

            ZoneController controller = (ZoneController)target;

            if (Application.isPlaying)
            {
                EditorGUILayout.Space(15);
                EditorGUILayout.LabelField("Play Mode Debug Controls", EditorStyles.boldLabel);

                if (GUILayout.Button("Force Start Shrinking (Server)", GUILayout.Height(30)))
                {
                    controller.DebugForceStartShrink();
                }

                if (GUILayout.Button("Force Start Moving (Server)", GUILayout.Height(30)))
                {
                    controller.DebugForceStartMoving();
                }
            }
        }

        private void OnSceneGUI()
        {
            ZoneController controller = (ZoneController)target;
            if (controller == null) return;

            // Use reflection or read serialized properties if they are private
            // We can read mapMin and mapMax using serialized properties to be safe
            SerializedProperty mapMinProp = serializedObject.FindProperty("mapMin");
            SerializedProperty mapMaxProp = serializedObject.FindProperty("mapMax");

            if (mapMinProp != null && mapMaxProp != null)
            {
                Vector2 min = mapMinProp.vector2Value;
                Vector2 max = mapMaxProp.vector2Value;

                Handles.color = Color.yellow;
                Vector3[] verts = new Vector3[]
                {
                    new Vector3(min.x, min.y, 0f),
                    new Vector3(max.x, min.y, 0f),
                    new Vector3(max.x, max.y, 0f),
                    new Vector3(min.x, max.y, 0f)
                };

                // Draw outline representing map boundary limits for safe zone bouncing
                Handles.DrawSolidRectangleWithOutline(verts, new Color(1f, 0.92f, 0.016f, 0.01f), Color.yellow);
                Handles.Label(new Vector3(min.x, max.y + 0.5f, 0f), "Zone Bounding Box Limits", EditorStyles.boldLabel);
            }
        }
    }
}
