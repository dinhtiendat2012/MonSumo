using UnityEditor;
using UnityEngine;
using MonSumo.World.Zone;

namespace MonSumo.Editor
{
    [CustomEditor(typeof(ZoneController))]
    public class ZoneControllerEditor : UnityEditor.Editor
    {
        private readonly UnityEditor.IMGUI.Controls.BoxBoundsHandle m_BoundsHandle = new UnityEditor.IMGUI.Controls.BoxBoundsHandle();

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Draw standard fields
            DrawDefaultInspector();

            ZoneController controller = (ZoneController)target;
            SerializedProperty mapMinProp = serializedObject.FindProperty("mapMin");
            SerializedProperty mapMaxProp = serializedObject.FindProperty("mapMax");

            if (!Application.isPlaying)
            {
                EditorGUILayout.Space(10);
                EditorGUILayout.LabelField("Designer Helpers", EditorStyles.boldLabel);

                if (GUILayout.Button("Fit Bounds to Background Sprite", GUILayout.Height(30)))
                {
                    // Find background object
                    GameObject bgObj = GameObject.Find("Background");
                    if (bgObj == null) bgObj = GameObject.Find("Map");
                    if (bgObj == null) bgObj = GameObject.Find("Arena");
                    if (bgObj == null) bgObj = GameObject.Find("Stage");

                    SpriteRenderer sr = null;
                    if (bgObj != null) bgObj.TryGetComponent<SpriteRenderer>(out sr);

                    if (sr == null)
                    {
                        // Find largest SpriteRenderer in scene
                        var renderers = FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None);
                        float maxArea = 0f;
                        foreach (var r in renderers)
                        {
                            if (r.gameObject.layer == 5) continue; // Skip UI
                            float area = r.bounds.size.x * r.bounds.size.y;
                            if (area > maxArea && area > 10f)
                            {
                                maxArea = area;
                                sr = r;
                            }
                        }
                    }

                    if (sr != null)
                    {
                        Undo.RecordObject(controller, "Fit Zone to Background Sprite");
                        mapMinProp.vector2Value = sr.bounds.min;
                        mapMaxProp.vector2Value = sr.bounds.max;
                        serializedObject.ApplyModifiedProperties();
                        Debug.Log($"[ZoneEditor] Boundary successfully fit to background sprite '{sr.gameObject.name}': Min={sr.bounds.min}, Max={sr.bounds.max}");
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("Find Background Sprite", "No background sprite renderer (with name Background/Map/Arena/Stage or large size) was found in the scene.", "OK");
                    }
                }
            }
            else
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

            serializedObject.ApplyModifiedProperties();
        }

        private void OnSceneGUI()
        {
            ZoneController controller = (ZoneController)target;
            if (controller == null) return;

            SerializedProperty mapMinProp = serializedObject.FindProperty("mapMin");
            SerializedProperty mapMaxProp = serializedObject.FindProperty("mapMax");

            if (mapMinProp != null && mapMaxProp != null)
            {
                serializedObject.Update();

                Vector2 min = mapMinProp.vector2Value;
                Vector2 max = mapMaxProp.vector2Value;

                // 1. Draw interactive Box Bounds Handle
                Bounds bounds = new Bounds();
                bounds.SetMinMax(new Vector3(min.x, min.y, 0f), new Vector3(max.x, max.y, 0f));
                
                m_BoundsHandle.center = bounds.center;
                m_BoundsHandle.size = bounds.size;
                m_BoundsHandle.axes = UnityEditor.IMGUI.Controls.PrimitiveBoundsHandle.Axes.X | UnityEditor.IMGUI.Controls.PrimitiveBoundsHandle.Axes.Y;
                m_BoundsHandle.handleColor = Color.green;
                m_BoundsHandle.wireframeColor = Color.yellow;

                EditorGUI.BeginChangeCheck();
                m_BoundsHandle.DrawHandle();
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(controller, "Modify Zone Boundaries");
                    Vector3 newMin = m_BoundsHandle.center - m_BoundsHandle.size * 0.5f;
                    Vector3 newMax = m_BoundsHandle.center + m_BoundsHandle.size * 0.5f;

                    mapMinProp.vector2Value = new Vector2(newMin.x, newMin.y);
                    mapMaxProp.vector2Value = new Vector2(newMax.x, newMax.y);
                    serializedObject.ApplyModifiedProperties();
                }

                // 2. Draw label and fill rectangle
                Handles.color = Color.yellow;
                Vector3[] verts = new Vector3[]
                {
                    new Vector3(min.x, min.y, 0f),
                    new Vector3(max.x, min.y, 0f),
                    new Vector3(max.x, max.y, 0f),
                    new Vector3(min.x, max.y, 0f)
                };
                Handles.DrawSolidRectangleWithOutline(verts, new Color(1f, 0.92f, 0.016f, 0.02f), Color.yellow);
                Handles.Label(new Vector3(min.x, max.y + 0.5f, 0f), "Zone Bounding Box Limits (Drag green handles to resize)", EditorStyles.boldLabel);
            }
        }
    }
}
