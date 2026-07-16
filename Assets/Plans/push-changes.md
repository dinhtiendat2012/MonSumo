# Project Overview
- Game Title: MonSumo
- High-Level Concept: 2D Sumo wrestling multiplayer game where players select Yokai characters, push opponents out of a shrinking arena, gather items, and dodge dynamic map hazards.
- Players: Single player, Local multiplayer, Network multiplayer (Netcode for GameObjects)
- Inspiration / Reference Games: Sumo, Super Smash Bros, dynamic party games
- Tone / Art Direction: 2D Pixel Art Retro
- Target Platform: StandaloneWindows64
- Screen Orientation / Resolution: Landscape (1920x1080 / any dynamically resized Aspect Ratio)
- Render Pipeline: URP (Universal Render Pipeline)

# Game Mechanics
## Core Gameplay Loop
Players join a networked lobby, choose their Yokai character (e.g., Tanuki, Oni), load into the matching arena, fight to knock opponents out of the shrinking safety zone using dynamic force-push and dash mechanics, and collect spawned items to turn the tide of battle.

## Controls and Input Methods
Keyboard & Mouse controls using both New Input System and Legacy Input Manager for movement, sprinting, lướt (dashing), and push attacks.

# UI
- **Character Selection Screen**: Allows clicking on character buttons (such as the newly unlocked Oni) to choose a Yokai, with a background that scales perfectly.
- **Match HUD**: Features health hearts, stamina slider, skill cooldown alerts, and an active player avatar within a transparent frame.

# Key Asset & Context
- `Assets/Scripts/UI/BackgroundCameraFitter.cs`: Attached to `bground` in `CharacterSelectScene` to ensure the background sprite fits the orthographic camera seamlessly on any resolution or maximized state.
- `Assets/Scripts/UI/GameHUD.cs`: Updated to dynamically sync player avatar on network/spawn and set the frame background to transparent.
- `Assets/Prefab/Character/Oni.prefab`: Networked character prefab for the newly added Oni Yokai.
- `Assets/Settings/GameData/Characters/OniData.asset`, `TanukiData.asset`, `TenguData.asset`: Updated to map characters with their respective sliced lobbyIcon.
- `Assets/DefaultNetworkPrefabs.asset`: Registered netcode prefabs including `Oni.prefab`.

# Implementation Steps
### Step 1: Stage All Local Changes
- **Description**: Run `git add -A` to stage all modified files (Scenes, HUD script, Character assets, Network Prefabs asset) and untracked files (BackgroundCameraFitter script and its meta).
- **Assigned role**: developer
- **Dependencies**: None
- **Parallelizable**: No

### Step 2: Commit staged changes
- **Description**: Run `git commit -m "feat: complete Oni integration, fix networked player HUD avatar, and resolve selection screen background scale"` to bundle the entire feature set cleanly.
- **Assigned role**: developer
- **Dependencies**: Step 1
- **Parallelizable**: No

### Step 3: Push changes to remote
- **Description**: Run `git push origin huy-netcode` to push all local commits safely to the remote branch on GitHub.
- **Assigned role**: developer
- **Dependencies**: Step 2
- **Parallelizable**: No

# Verification & Testing
- Run `git status` on local machine to verify the working tree is clean.
- Verify GitHub remote repository for branch `huy-netcode` has the new commit and is successfully up-to-date.
