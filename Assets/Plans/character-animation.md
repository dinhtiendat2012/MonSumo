# Project Overview
- **Game Title**: MonSumo
- **High-Level Concept**: A 2D top-down multiplayer sumo battle game where Yokai characters use items and push abilities to knock each other out of a shrinking arena.
- **Players**: Multiplayer (Host-Client networking using Unity Netcode for GameObjects)
- **Inspiration / Reference Games**: Sumo, Super Smash Bros (ring-out mechanic), top-down party games
- **Tone / Art Direction**: Stylized Japanese folklore / Yokai (Tanuki, Oni, Tengu)
- **Target Platform**: Standalone PC (Windows 64-bit)
- **Screen Orientation / Resolution**: Landscape (1920x1080)
- **Render Pipeline**: URP (Universal Render Pipeline)

# Game Mechanics
## Core Gameplay Loop
Players select their Yokai characters, join a sảnh chờ (Lobby), and spawn in the arena. They move around, collect skill cards, use dashes/sprints, and execute standard pushes or special abilities to launch opponents backward. The goal is to remain the last standing Yokai inside the shrinking sumo ring (Zone).

## Controls and Input Methods
- **WASD / Arrow Keys**: 4-way movement (supporting diagonal inputs)
- **Shift (Hold)**: Sprint
- **Space**: Dash
- **Mouse Left Click**: Normal Push Attack / Use Card Skill (if holding a skill card)

# UI
Not directly modified in this step. The Lobby UI recently built will remain unchanged, but the in-game player character will now render and animate correctly in the Match scene based on movement input.

# Key Asset & Context
### Existing Sprite Sheets (under `Assets/Sprites/Character/`)
All sheets are exactly **192x48** pixels (containing 4 horizontal frames of **48x48**):
1. **Idle**:
   - `Tanuki_Idle_South.png` (Front / Down)
   - `Tanuki_Idle_North.png` (Back / Up)
   - `Tanuki_Idle_Side.png` (Side / Left-Right)
2. **Walk**:
   - `Tanuki_Walk_South.png` (Front / Down)
   - `Tanuki_Walk_North.png` (Back / Up)
   - `Tanuki_Walk_EastWest.png` (Side / Left-Right)
3. **Run** (Sprint):
   - `Tanuki_Run_South.png` (Front / Down)
   - `Tanuki_Run_North.png` (Back / Up)
   - `Tanuki_Run_Side.png` (Side / Left-Right)

### Key Files to Create/Modify
1. **`Assets/Scripts/Editor/AnimationGenerator.cs`** (New Editor script): Automates slicing of the 192x48 sprite sheets and builds the complete Animator Controller with 2D Blend Trees.
2. **`Assets/Scripts/Core/PlayerMovement.cs`** (Modify): Feeds the direction vector `DirX`, `DirY` and state enum to the Animator component and applies the controller from PlayerDataSO.
3. **`Assets/Settings/GameData/Characters/TanukiData.asset`** (New/Update): Create a `PlayerDataSO` for Tanuki, linking the newly generated Animator Controller.

---

# Implementation Steps

### Step 1: Editor Automation Script for Slicing & Animator Generation
- **Description**: Create an editor script `Assets/Scripts/Editor/AnimationGenerator.cs` that:
  1. Configures the 9 sprite sheets to **Sprite Import Mode: Multiple** and slices them into four 48x48 pixel horizontal frames.
  2. Generates individual `AnimationClip` assets (`.anim`) for each sheet with looping enabled for Idle, Walk, and Run.
  3. Generates the `AnimatorController` asset with three parameters: `State` (Int), `DirX` (Float), `DirY` (Float).
  4. Configures 2D Blend Trees (Simple Directional) for the principal states:
     - **Idle State**: Points (0, -1) -> South, (0, 1) -> North, (1, 0) -> Side, (-1, 0) -> Side.
     - **Walk State**: Points (0, -1) -> South, (0, 1) -> North, (1, 0) -> EastWest, (-1, 0) -> EastWest.
     - **Sprint State**: Points (0, -1) -> South, (0, 1) -> North, (1, 0) -> Side, (-1, 0) -> Side.
     - **Dash State**: Reuses the Sprint Blend Tree but overrides the state playback speed to `2.0f`.
     - **Knockback State**: Reuses the Idle Blend Tree but with playback speed `0.0f` to freeze the sprite in hit-stun posture.
  5. Connects all states via clean transition conditions based solely on `State == (int)PlayerMovementState`.
- **Assigned role**: developer
- **Dependencies**: None
- **Parallelizable**: No

### Step 2: C# Script Integration (PlayerMovement & Animator Wiring)
- **Description**: Update `PlayerMovement.cs` to integrate with the new animator parameters:
  1. In `Start()`, dynamically set `_animator.runtimeAnimatorController = _player.playerData.animatorController` if available.
  2. In `UpdateAnimator()`, set the Float parameters `DirX` and `DirY` using the normalized `_facingDirection` vector.
  3. Set the Integer parameter `State` using `(int)_stateMachine.StateEnum`.
- **Assigned role**: developer
- **Dependencies**: Step 1
- **Parallelizable**: No

### Step 3: Character Data Setup
- **Description**: Create or configure the `TanukiData.asset` (`PlayerDataSO`) asset:
  1. Link the generated `TanukiController.controller` to the `animatorController` field.
  2. Configure base stats for the Tanuki character.
- **Assigned role**: developer
- **Dependencies**: Step 1
- **Parallelizable**: Yes

---

# Verification & Testing

### 1. Sprite Slicing Verification
- Inspect the 9 sprite sheets in the Project window. They must expand to show 4 individual sliced sub-sprites (e.g., `Tanuki_Idle_South_0` to `Tanuki_Idle_South_3`).
- Verify the PPU (Pixels Per Unit) and Filter Mode settings (Point filtering for crisp pixel art).

### 2. Animator Controller Structure Verification
- Open the generated `TanukiController.controller` in the Animator window.
- Verify that the 5 states (`Idle`, `Walk`, `Sprint`, `Dash`, `Knockback`) exist and are connected via parameter conditions (e.g., `State Equals 0` for Idle, `State Equals 4` for Knockback).
- Inspect the Blend Trees to ensure they are 2D Simple Directional and contain the 4 correct directional points.

### 3. Gameplay Playtest (Manual Checks)
- Run the game in Host or Client mode.
- Perform the following actions and check visual feedback:
  - **Stationary**: Character should play the `Idle` animation in the last-faced direction.
  - **Walking (WASD)**: Plays `Walk` animation. If moving Left, the sprite must flip horizontally (`flipX = true`).
  - **Sprinting (Hold Shift + WASD)**: Plays the faster, more aggressive `Run` animation.
  - **Dashing (Press Space)**: Quickly flashes a high-speed sprint animation.
  - **Knockback (Press K to test taking damage)**: Character instantly freezes in the first frame of the faced direction and flies back, returning to idle once physics come to rest.
