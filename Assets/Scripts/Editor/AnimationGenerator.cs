using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace MonSumo.Editor
{
    public static class AnimationGenerator
    {
        private const string SpritesRoot = "Assets/Sprites/Character";
        private const string AnimationsDir = "Assets/Animations/Tanuki";
        private const string ControllerPath = "Assets/Animations/Tanuki/TanukiController.controller";

        [MenuItem("MonSumo/Generate Animations")]
        public static void GenerateAllAnimations()
        {
            Debug.Log("[AnimationGenerator] Starting Tanuki Animation Generation...");

            // 1. Ensure target directory exists
            if (!AssetDatabase.IsValidFolder("Assets/Animations"))
            {
                AssetDatabase.CreateFolder("Assets", "Animations");
            }
            if (!AssetDatabase.IsValidFolder(AnimationsDir))
            {
                AssetDatabase.CreateFolder("Assets/Animations", "Tanuki");
            }

            // 2. Define Spritesheets and slice them
            var sheets = new Dictionary<string, (string path, bool loop)>
            {
                { "Idle_North", (Path.Combine(SpritesRoot, "Idle/Tanuki_Idle_North.png"), true) },
                { "Idle_South", (Path.Combine(SpritesRoot, "Idle/Tanuki_Idle_South.png"), true) },
                { "Idle_Side", (Path.Combine(SpritesRoot, "Idle/Tanuki_Idle_Side.png"), true) },

                { "Walk_North", (Path.Combine(SpritesRoot, "Walk/Tanuki_Walk_North.png"), true) },
                { "Walk_South", (Path.Combine(SpritesRoot, "Walk/Tanuki_Walk_South.png"), true) },
                { "Walk_Side", (Path.Combine(SpritesRoot, "Walk/Tanuki_Walk_EastWest.png"), true) },

                { "Run_North", (Path.Combine(SpritesRoot, "Run/Tanuki_Run_North.png"), true) },
                { "Run_South", (Path.Combine(SpritesRoot, "Run/Tanuki_Run_South.png"), true) },
                { "Run_Side", (Path.Combine(SpritesRoot, "Run/Tanuki_Run_Side.png"), true) }
            };

            var clips = new Dictionary<string, AnimationClip>();

            foreach (var kvp in sheets)
            {
                string id = kvp.Key;
                string path = kvp.Value.path.Replace('\\', '/');
                bool loop = kvp.Value.loop;

                if (!File.Exists(path))
                {
                    Debug.LogError($"[AnimationGenerator] Spritesheet not found at: {path}");
                    continue;
                }

                // Slice Spritesheet
                SliceSpritesheet(path);

                // Create Animation Clip
                AnimationClip clip = CreateClipFromSpritesheet(path, id, loop);
                if (clip != null)
                {
                    clips[id] = clip;
                }
            }

            if (clips.Count < 9)
            {
                Debug.LogError("[AnimationGenerator] Failed to generate all 9 directional clips. Aborting controller creation.");
                return;
            }

            // 3. Create Animator Controller
            if (File.Exists(ControllerPath))
            {
                AssetDatabase.DeleteAsset(ControllerPath);
            }

            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            if (controller == null)
            {
                Debug.LogError($"[AnimationGenerator] Failed to create AnimatorController at: {ControllerPath}");
                return;
            }

            // 4. Setup parameters
            controller.AddParameter("State", AnimatorControllerParameterType.Int);
            controller.AddParameter("DirX", AnimatorControllerParameterType.Float);
            controller.AddParameter("DirY", AnimatorControllerParameterType.Float);

            // Get Root State Machine of Layer 0
            AnimatorStateMachine rootSm = controller.layers[0].stateMachine;

            // 5. Create Blend Trees for each State
            
            // --- IDLE STATE ---
            BlendTree idleTree;
            AnimatorState idleState = controller.CreateBlendTreeInController("Idle", out idleTree, 0);
            SetupDirectionalBlendTree(idleTree, clips["Idle_South"], clips["Idle_North"], clips["Idle_Side"]);

            // --- WALK STATE ---
            BlendTree walkTree;
            AnimatorState walkState = controller.CreateBlendTreeInController("Walk", out walkTree, 0);
            SetupDirectionalBlendTree(walkTree, clips["Walk_South"], clips["Walk_North"], clips["Walk_Side"]);

            // --- SPRINT STATE ---
            BlendTree sprintTree;
            AnimatorState sprintState = controller.CreateBlendTreeInController("Sprint", out sprintTree, 0);
            SetupDirectionalBlendTree(sprintTree, clips["Run_South"], clips["Run_North"], clips["Run_Side"]);

            // --- DASH STATE (Sprint at 2.0x speed) ---
            BlendTree dashTree;
            AnimatorState dashState = controller.CreateBlendTreeInController("Dash", out dashTree, 0);
            SetupDirectionalBlendTree(dashTree, clips["Run_South"], clips["Run_North"], clips["Run_Side"]);
            dashState.speed = 2.0f;

            // --- KNOCKBACK STATE (Idle at 0.0x speed - freezes on first frame) ---
            BlendTree knockbackTree;
            AnimatorState knockbackState = controller.CreateBlendTreeInController("Knockback", out knockbackTree, 0);
            SetupDirectionalBlendTree(knockbackTree, clips["Idle_South"], clips["Idle_North"], clips["Idle_Side"]);
            knockbackState.speed = 0.0f;

            // 6. Setup Transitions using Any State for total state machine robustness
            AddAnyStateTransition(rootSm, idleState, 0);
            AddAnyStateTransition(rootSm, walkState, 1);
            AddAnyStateTransition(rootSm, sprintState, 2);
            AddAnyStateTransition(rootSm, dashState, 3);
            AddAnyStateTransition(rootSm, knockbackState, 4);

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();

            Debug.Log($"[AnimationGenerator] Successfully created and configured AnimatorController at: {ControllerPath}");
        }

        private static void SliceSpritesheet(string path)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return;

            // Configure importer settings for 2D Pixel Art
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            
            // Set max size and make it readable
            importer.maxTextureSize = 256;
            importer.isReadable = true;

            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture == null) return;

            int frameCount = 4;
            int tileWidth = texture.width / frameCount;
            int tileHeight = texture.height;

            var metas = new List<SpriteMetaData>();
            string baseName = Path.GetFileNameWithoutExtension(path);

            for (int i = 0; i < frameCount; i++)
            {
                SpriteMetaData meta = new SpriteMetaData
                {
                    name = $"{baseName}_{i}",
                    rect = new Rect(i * tileWidth, 0, tileWidth, tileHeight),
                    alignment = (int)SpriteAlignment.Center,
                    pivot = new Vector2(0.5f, 0.5f)
                };
                metas.Add(meta);
            }

            importer.spritesheet = metas.ToArray();
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();
        }

        private static AnimationClip CreateClipFromSpritesheet(string path, string clipName, bool loop)
        {
            // Load sliced sub-sprites
            Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(path);
            var sprites = subAssets.OfType<Sprite>().OrderBy(s => s.name).ToList();

            if (sprites.Count == 0)
            {
                Debug.LogError($"[AnimationGenerator] No sub-sprites found in {path}");
                return null;
            }

            AnimationClip clip = new AnimationClip();
            clip.name = clipName;
            clip.frameRate = 8f; // Perfect retro speed for 4-frame clips

            // Configure Looping
            if (loop)
            {
                AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
                settings.loopTime = true;
                AnimationUtility.SetAnimationClipSettings(clip, settings);
            }

            // Bind Curve to SpriteRenderer's m_Sprite field
            EditorCurveBinding curveBinding = new EditorCurveBinding
            {
                type = typeof(SpriteRenderer),
                path = "",
                propertyName = "m_Sprite"
            };

            ObjectReferenceKeyframe[] keyframes = new ObjectReferenceKeyframe[sprites.Count];
            for (int i = 0; i < sprites.Count; i++)
            {
                keyframes[i] = new ObjectReferenceKeyframe
                {
                    time = i / clip.frameRate,
                    value = sprites[i]
                };
            }

            AnimationUtility.SetObjectReferenceCurve(clip, curveBinding, keyframes);

            string clipPath = $"{AnimationsDir}/{clipName}.anim";
            AssetDatabase.CreateAsset(clip, clipPath);
            return clip;
        }

        private static void SetupDirectionalBlendTree(BlendTree tree, Motion south, Motion north, Motion side)
        {
            tree.blendType = BlendTreeType.SimpleDirectional2D;
            tree.blendParameter = "DirX";
            tree.blendParameterY = "DirY";

            // Add children
            tree.AddChild(south, new Vector2(0, -1)); // Front / Down
            tree.AddChild(north, new Vector2(0, 1));  // Back / Up
            tree.AddChild(side, new Vector2(1, 0));   // Side (East, uses normal rendering)
            tree.AddChild(side, new Vector2(-1, 0));  // Side (West, C# code handles SpriteRenderer.flipX)
        }

        private static void AddAnyStateTransition(AnimatorStateMachine stateMachine, AnimatorState targetState, int stateValue)
        {
            var transition = stateMachine.AddAnyStateTransition(targetState);
            transition.AddCondition(AnimatorConditionMode.Equals, stateValue, "State");
            transition.hasExitTime = false;
            transition.duration = 0f; // Instant state transition
            transition.canTransitionToSelf = false; // Never loop transition to self
        }
    }
}