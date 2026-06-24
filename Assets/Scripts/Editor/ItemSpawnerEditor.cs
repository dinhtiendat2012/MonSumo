using UnityEditor;
using UnityEngine;
using MonSumo.World.Spawning;

namespace MonSumo.Editor
{
    [CustomEditor(typeof(ItemSpawner))]
    public class ItemSpawnerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            // Draw standard fields
            DrawDefaultInspector();

            ItemSpawner spawner = (ItemSpawner)target;

            if (Application.isPlaying)
            {
                EditorGUILayout.Space(15);
                EditorGUILayout.LabelField("Play Mode Debug Controls", EditorStyles.boldLabel);

                if (GUILayout.Button("Force Spawn Item (Server)", GUILayout.Height(30)))
                {
                    bool spawned = spawner.TrySpawnItem(onlyCommon: false);
                    if (spawned)
                    {
                        Debug.Log("[ItemSpawner Debug] Item successfully spawned!");
                    }
                    else
                    {
                        Debug.LogWarning("[ItemSpawner Debug] Failed to spawn item (check zone boundaries or limit).");
                    }
                }
            }
        }
    }
}
