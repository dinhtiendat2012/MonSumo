# Project Overview
- **Game Title**: MonSumo
- **High-Level Concept**: A 2D top-down multiplayer sumo battle game where Yokai characters use items and push abilities to knock each other out of a shrinking arena.
- **Players**: Multiplayer (Host-Client networking using Unity Netcode for GameObjects)
- **Inspiration / Reference Games**: Sumo, Super Smash Bros (ring-out mechanic)
- **Tone / Art Direction**: Stylized Japanese folklore / Yokai (Tanuki, Oni, Tengu)
- **Target Platform**: Standalone PC (Windows 64-bit)
- **Screen Orientation / Resolution**: Landscape (1920x1080)
- **Render Pipeline**: URP (Universal Render Pipeline)

# Game Mechanics
## Core Gameplay Loop
Players select their Yokai characters, join a lobby, and spawn in the Match scene. They move around, collect skill cards, use dashes/sprints, and execute standard pushes or special abilities to launch opponents backward. The goal is to remain the last standing Yokai inside the shrinking sumo ring (Zone).

## Controls and Input Methods
- **WASD / Arrow Keys**: 4-way movement (supporting diagonal inputs)
- **Shift (Hold)**: Sprint
- **Space**: Dash (causes stronger physics knockback upon collision with an opponent)
- **Mouse Left Click**: Normal Push Attack (light melee knockback with a short cooldown)

# UI
A new top-center warning panel will be added to the HUD Canvas in the Match scene. It will show a countdown warning before the Sumo Ring starts to shrink and shift, followed by active alerts during the shrinking/moving phase.

# Key Asset & Context
### Existing Assets & Script Context
1. **`Assets/Scripts/Core/PlayerMovement.cs`**:
   - Currently runs `Update()` and `UpdateAnimator()` only on the owning client (`IsOwner == true`), causing animation freezing on non-owner clones.
   - Triggers melee push attacks using `RequestAttackServerRpc`.
2. **`Assets/Scripts/World/Zone/ZoneController.cs`**:
   - Manages safe zone size, shrinking, and movement.
   - Contains map boundaries and synchronizes radius/center.
3. **`Assets/Scripts/UI/GameHUD.cs`**:
   - Manages player HUD elements (avatar, hearts, stamina bar).

---

# Implementation Steps

### Step 1: Network Animation & Visual Syncing (Priority 1)
- **Description**: Fix the multiplayer animation freezing issue by synchronizing player movement state and facing direction over the network using `NetworkVariable`s with `NetworkVariableWritePermission.Owner`.
  1. Add `_netState` (`NetworkVariable<int>`) and `_netFacingDirection` (`NetworkVariable<Vector2>`) to `PlayerMovement.cs`.
  2. Inside `Update()`, split the owner-only input/stamina checks and the global visual updates.
  3. The Owner writes the current state and normalized facing direction to the network variables.
  4. Both Owner and Non-owners execute `UpdateAnimatorAndVisuals()` in `Update()`, which reads from the network variables and updates the local Animator parameters and `SpriteRenderer.flipX`.
- **Assigned role**: developer
- **Dependencies**: None
- **Parallelizable**: No

### Step 2: Darken Outer Zone Area Using SpriteMask (No Large Visual Ring Sprite)
- **Description**: Darken the area outside the safe zone to guide players visually without cluttering the screen with giant glowing circle textures.
  1. Add a `SpriteMask` component to the `SafeZone` GameObject using `Ring3.png` solely as the mask source (completely invisible, no `SpriteRenderer` component).
  2. Link this `SpriteMask` to `ZoneController.zoneSpriteMask`.
  3. Create a dark fullscreen overlay sprite `ZoneVignetteOverlay` with deep translucent dark color (e.g. `rgba(0, 0, 0.05, 0.65)`), set its `Mask Interaction` to `Visible Outside Mask` and its `Sorting Order` to 40 (rendering below characters but above background).
- **Assigned role**: developer
- **Dependencies**: Step 1
- **Parallelizable**: Yes

### Step 3: Top-Center Zone Alarm & Countdown Popups
- **Description**: Implement a beautiful alert HUD at the top center of the screen displaying real-time countdown alerts before the zone starts shrinking/moving.
  1. Add a `NetworkVariable<float> shrinkCountdown` to `ZoneController.cs`. Update this countdown value on the Server in `UpdateServerZone()`.
  2. Create a top-center TMP text popup `ZoneAlarmText` inside the Match `Canvas`.
  3. Update `GameHUD.cs` to read `shrinkCountdown` from the local `ZoneController` reference and update the text:
     - When countdown is $> 0$ and $\le 30$ seconds: Display "SAFE ZONE SHRINKING IN: XXs" in orange, flashing red when under 10 seconds.
     - When countdown reaches $0$: Display "SAFE ZONE SHRINKING & MOVING!" in bright red.
- **Assigned role**: developer
- **Dependencies**: Step 1
- **Parallelizable**: Yes

### Step 4: Double-Action Knockback (Mouse Left Click vs. Q/Space Dash)
- **Description**: Refine the combat logic so that normal attacks (Left Click) apply a light push force with a custom cooldown, while Dash collisions (Q/Space) apply a heavy push force.
  1. Add `meleePushForce` (default `8f`) and `dashPushForce` (default `18f`) fields to `PlayerDataSO.cs`.
  2. Update `PlayerMovement.RequestAttackServerRpc()` to apply the customizable `meleePushForce` when clicked.
  3. Implement active collision/overlap check inside `PlayerDashState.cs` on the client during dash. If an opponent is hit, invoke `RequestDashKnockbackServerRpc` in `PlayerMovement.cs` to apply the heavy `dashPushForce` from the server.
  4. Ensure a `HashSet` tracks already hit players during a single dash to prevent multi-hit physics glitches.
- **Assigned role**: developer
- **Dependencies**: Step 1
- **Parallelizable**: No

### Step 5: Disable Passive Player-to-Player Pushing (Explicit Push Combat)
- **Description**: Prevent players from pushing or moving each other by simply walking or bumping into one another normally.
  1. In `PlayerMovement.Start()`, run a helper method `IgnorePlayerCollisions()` that calls `Physics2D.IgnoreCollision(myCollider, otherPlayerCollider, true)` for all active players in the scene.
  2. This guarantees players can smoothly slide or pass through each other during normal movement, making combat purely explicit (i.e. only Dash collisions or Left-Click melee attacks can displace opponents).
- **Assigned role**: developer
- **Dependencies**: Step 4
- **Parallelizable**: No

---

# Verification & Testing

### 1. Multiplayer Animation Sync Verification
- Run a Host and Client build. Verify that moving the Host character displays correct Walk/Run animations on the Client screen, and vice versa.
- Verify sprite flipping (Left/Right) is synchronized flawlessly on all remote clones.

### 2. Zone Vignette Mask Verification
- Play the game and verify that the area outside the Sumo Rope LineRenderer is darker, while the inside remains perfectly bright and visible.
- Verify that as the Sumo Rope shrinks, the darkened vignette follows the border exactly.

### 3. Countdown UI Alarm Verification
- Ensure the top-center HUD displays a precise countdown starting 30 seconds before shrinking begins.
- Flashing color shifts (Orange -> Red) must trigger properly at the 10-second mark.

### 4. Push Physics & Passive Collision Verification
- Walking into another player must NOT nudge or push them (they should pass through or ignore each other's solid bodies).
- Left-clicking next to an opponent must push them back slightly (light knockback).
- Dashing directly into an opponent must launch them significantly farther (heavy knockback).
