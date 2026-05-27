# COLOR CARGO LOOP - GAME DESIGN & UNITY PRODUCTION DOCUMENT

## 1. Project Overview

**Project Name:** Color Cargo Loop  
**Genre:** Hybrid-casual / puzzle / sorting game  
**Platform:** Mobile, Android first  
**Engine:** Unity  
**Camera Style:** Fixed orthographic 3D camera  
**Visual Style:** Premium casual mobile puzzle, soft rounded 3D, toy-like objects, clean UI, strong color readability  
**Target Production Goal:** Fast playable prototype suitable for closed testing within 3 days

Color Cargo Loop is a simple but satisfying sorting puzzle game where the player taps cargo carts to release colored packages onto a moving loop conveyor. Packages travel around the loop and must be collected, matched, or cleared according to color/order rules. The gameplay should be easy to understand in one second, visually satisfying, and highly suitable for fail-based ad creatives.

The game should feel like a modern hybrid-casual puzzle title: simple controls, clear feedback, juicy animations, readable colors, and scalable level structure.

This project must be built with a clean, modular Unity architecture so levels, colors, obstacles, carts, boosters, and monetization hooks can be expanded later without rewriting the core system.

---

## 2. Core Gameplay Concept

The screen contains:

- A rounded rectangular loop conveyor path.
- Several cargo carts placed inside the loop.
- Each cart contains colored cargo blocks/packages.
- The player taps a cart to release one package, or a row/slot of packages depending on the level rules.
- Released packages enter the loop and move along the path.
- Packages are sorted/cleared when they reach matching target zones, color gates, or collection areas.
- The level is completed when the target number of cargo pieces is successfully cleared.
- The player loses if the loop becomes blocked, the queue overflows, or there are no valid moves left.

The game should be built around **one-tap interaction**.

The player should instantly understand:

> "Tap the carts, release the correct colored packages, keep the loop flowing, and clear all cargo."

---

## 3. Core Design Pillars

### 3.1 Simple to Understand

The player should understand the game by watching 2 seconds of gameplay.

There should be no heavy tutorial required. The first levels should teach through layout.

### 3.2 Satisfying Movement

Cargo pieces should move smoothly on the loop. Movement should feel soft, polished, and toy-like.

Every released package should create:

- Small pop animation
- Light bounce
- Tiny particle burst
- SFX
- Optional haptic feedback

### 3.3 Clear Color Logic

Colors must be readable:

- Red
- Blue
- Yellow
- Green
- Purple / Orange later

Each package, cart slot, target zone, and UI element must be visually clean.

### 3.4 Fail-Friendly Design

The game should naturally create "almost solved but failed" moments.

Good fail scenarios:

- Wrong color released too early
- Conveyor gets crowded
- Target color blocked
- Only one move left but wrong package is on top
- Player panics and taps wrong cart

These moments are important for ad creatives.

### 3.5 Expandable Hybrid-Casual Structure

The base game should support:

- More colors
- More carts
- More cargo types
- Obstacles
- Boosters
- Skins
- Level packs
- Rewarded ads
- Remove ads
- IAP booster packs

---

## 4. Visual Style Direction

The game should use a **premium 3D casual puzzle style**.

### 4.1 General Look

- Dark bluish-purple background
- Rounded chunky loop conveyor
- Glossy toy-like package blocks
- Soft bevels
- Clean highlights
- Strong shadows
- Subtle glow accents
- High contrast between objects and background

The style should feel polished, soft, and readable. Avoid realistic textures. Avoid noisy details.

### 4.2 Camera

Use a fixed orthographic camera.

Recommended camera:

- Orthographic
- Slight top-down angle or pure top-down with 3D perspective feel
- Vertical mobile composition
- Gameplay area centered
- UI overlay above and below

Camera should not rotate during gameplay. Minor camera shake is allowed for feedback.

### 4.3 Environment

The background should be simple.

Recommended:

- Dark purple gradient floor
- Rounded loop track in the center
- Inner play area slightly darker
- Track edges purple with glossy highlights
- Conveyor lane black/dark blue

Do not create a complex environment. The game must be readable.

### 4.4 Carts

Carts should be cute rounded mini delivery carts or toy cargo wagons.

Each cart should have:

- Rounded body
- 2x2 or 1x3 cargo slots
- Small wheels
- Color accent light
- Small side handle/detail
- Clear slot positions

Carts should not look exactly like any reference game. They should feel original.

### 4.5 Cargo Pieces

Cargo pieces can be:

- Toy blocks
- Gift parcels
- Cargo crates
- Rounded cubes
- Small boxes with optional ribbon

Each color should have the same silhouette for gameplay clarity.

Optional visual variants:

- Plain cargo box
- Gift box
- Locked crate
- Ice crate
- Mystery crate

### 4.6 UI Style

UI must be bold, readable, and mobile-friendly.

Top UI:

- Left: level label, example: `Seviye 12`
- Center: progress badge, example: crate icon + `5/12`
- Right: settings button

Bottom UI:

- Undo booster
- Shuffle booster
- Extra Cart / Extra Slot booster

Buttons:

- Rounded square/pill
- Purple base
- Yellow/gold outline
- White icon
- Small green count badge

---

## 5. Gameplay Rules - Base Version

### 5.1 Level Start

At level start:

- Load level data.
- Spawn carts.
- Fill carts with cargo colors.
- Spawn loop path.
- Initialize target count.
- Set progress to 0.
- Enable player input.

### 5.2 Player Input

Player taps a cart.

If the cart has available cargo:

- Select the next cargo piece.
- Remove it from the cart slot.
- Spawn/release it onto the conveyor entry point.
- Cargo starts moving along the loop path.
- Update cart visual.

If the cart is empty:

- Do nothing or play invalid tap feedback.

### 5.3 Cargo Movement

Cargo moves along the loop path using waypoint/path movement.

Important:

- Do not rely on complex physics for main movement.
- Use deterministic movement along a path.
- Cargo pieces should keep spacing from each other.
- If spacing is too small, cargo should slow or queue.

### 5.4 Color Clearing

Base clearing rule:

- Cargo pieces move around the loop.
- If a cargo reaches a matching target/checkpoint, it clears.
- Progress increases.
- Play clear VFX/SFX.
- Destroy or pool the cargo object.

Alternative simpler V1 rule:

- Cargo pieces of the same color group together on the loop.
- When 3 same-colored cargo pieces connect or reach a matching collector, they clear.

For the first prototype, use the simplest stable rule:

> Each released cargo moves along the loop. If it reaches its matching color collector, it is collected and progress increases.

### 5.5 Win Condition

Win when:

```text
clearedCargoCount >= requiredCargoCount
```

On win:

- Stop input
- Play celebration particles
- Play success sound
- Show win panel
- Unlock next level

### 5.6 Lose Condition

Lose when one of these happens:

```text
loopCargoCount >= maxLoopCapacity
```

or

```text
no valid cargo remains and target is not completed
```

or

```text
cargo queue is blocked for too long
```

For V1, use only:

```text
loopCargoCount >= maxLoopCapacity
```

This is simple and easy to test.

On lose:

- Stop input
- Play fail shake
- Play fail sound
- Show fail panel
- Offer retry / rewarded continue later

---

## 6. Level Design

### 6.1 Level Data

Each level should be data-driven.

Create a `LevelData` ScriptableObject.

It should contain:

```csharp
int levelIndex;
int requiredCargoCount;
int maxLoopCapacity;
float cargoMoveSpeed;
List<CartData> carts;
List<ColorTargetData> targets;
```

Each cart should contain:

```csharp
Vector3 cartPosition;
CargoColor cartAccentColor;
List<CargoColor> cargoSlots;
```

Each target should contain:

```csharp
CargoColor targetColor;
int requiredAmount;
Transform/path position reference;
```

### 6.2 First 10 Level Progression

Level 1:
- 2 carts
- 2 colors
- No fail pressure
- Teach tapping

Level 2:
- 3 carts
- 2 colors
- Slight ordering puzzle

Level 3:
- 3 carts
- 3 colors
- Introduce loop capacity

Level 4:
- 4 carts
- 3 colors
- More cargo pieces

Level 5:
- 4 carts
- 3 colors
- First "wrong tap can crowd loop" moment

Level 6:
- 4 carts
- 4 colors
- Introduce harder order

Level 7:
- 4 carts
- 4 colors
- More target count

Level 8:
- 5 carts
- 4 colors
- Limited loop capacity

Level 9:
- 5 carts
- 4 colors
- Harder color distribution

Level 10:
- 5 carts
- 4 colors
- First "hard level" label possibility

### 6.3 Level Generation

Do not build full random generation first.

Use:

- Handcrafted level templates
- Then create a simple generator later

Recommended generator approach:

```text
Template + seed + difficulty parameters
```

Difficulty parameters:

- color count
- cart count
- cargo per cart
- max loop capacity
- target count
- blocker count
- speed

The generator should not create impossible levels.

For the 3-day prototype, handcrafted 30 levels is enough.

---

## 7. Boosters

### 7.1 Undo

Undo the last released cargo.

Behavior:

- Remove last cargo from loop if still active.
- Return it to original cart slot.
- Decrease progress only if it was not cleared.
- For V1, only allow undo for active non-cleared cargo.

### 7.2 Shuffle

Shuffle cargo order inside carts.

Behavior:

- Randomize remaining cargo pieces in all carts.
- Do not affect cargo already on loop.
- Play cart shake animation.

### 7.3 Extra Cart / Extra Slot

Adds temporary capacity.

Two possible versions:

Option A:

- Increase loop max capacity by +1 temporarily.

Option B:

- Add one temporary empty helper cart/slot.

For V1, use Option A:

```text
maxLoopCapacity += 1
```

This is much simpler.

---

## 8. Monetization Hooks

For closed test prototype, do not fully implement monetization if time is short. But prepare hooks.

### 8.1 Rewarded Ad Hooks

Rewarded ad opportunities:

- Continue after fail
- Get +1 extra capacity
- Get extra booster
- Skip hard level

### 8.2 Interstitial Hooks

Potential moments:

- Every 2 or 3 level completions
- After retry count
- After returning to menu

### 8.3 IAP Hooks

Future IAP:

- Remove Ads
- Starter Pack
- Booster Pack
- Premium Bundle

Do not overbuild this in first prototype. Just create placeholders/events.

---

## 9. Required Unity Scene Structure

Recommended hierarchy:

```text
GameScene
│
├── Main Camera
├── Directional Light
├── Global Volume / Lighting
│
├── GameRoot
│   ├── LoopTrack
│   ├── ConveyorPath
│   ├── CartContainer
│   ├── CargoContainer
│   ├── TargetContainer
│   └── FXContainer
│
├── UI
│   ├── Canvas
│   │   ├── TopHUD
│   │   │   ├── LevelLabel
│   │   │   ├── ProgressBadge
│   │   │   └── SettingsButton
│   │   ├── BottomBoosters
│   │   │   ├── UndoButton
│   │   │   ├── ShuffleButton
│   │   │   └── ExtraSlotButton
│   │   ├── WinPanel
│   │   ├── LosePanel
│   │   └── TutorialHand
│
└── Managers
    ├── GameManager
    ├── LevelManager
    ├── InputManager
    ├── UIManager
    ├── AudioManager
    ├── HapticManager
    └── BoosterManager
```

---

## 10. Required Scripts

### 10.1 GameManager.cs

Responsible for:

- Game state
- Win
- Lose
- Start level
- Restart level
- Next level

Game states:

```csharp
public enum GameState
{
    Loading,
    Playing,
    Won,
    Lost,
    Paused
}
```

### 10.2 LevelManager.cs

Responsible for:

- Loading `LevelData`
- Spawning carts
- Spawning cargo
- Spawning targets
- Tracking progress

### 10.3 CartController.cs

Responsible for:

- Holding cargo slots
- Handling tap
- Releasing next cargo
- Visual update
- Invalid tap feedback

### 10.4 CargoController.cs

Responsible for:

- Cargo color
- Movement along path
- Current path progress
- Clear / collect logic
- Pool return

### 10.5 ConveyorPath.cs

Responsible for:

- Waypoint list
- Path interpolation
- Getting position by normalized path value
- Looping movement

### 10.6 CargoQueueManager.cs

Responsible for:

- Active cargo list
- Capacity check
- Spacing between cargo pieces
- Lose condition when capacity exceeded

### 10.7 TargetZone.cs

Responsible for:

- Color matching
- Collecting cargo
- Triggering progress increase

### 10.8 BoosterManager.cs

Responsible for:

- Undo
- Shuffle
- Extra capacity
- Booster counts

### 10.9 UIManager.cs

Responsible for:

- Level text
- Progress text
- Booster counts
- Win panel
- Lose panel

### 10.10 HapticManager.cs

Responsible for:

- Light tap vibration
- Clear vibration
- Fail vibration
- Win vibration

Use simple wrappers so mobile vibration can be swapped later.

### 10.11 AudioManager.cs

Responsible for:

- Tap sound
- Release sound
- Cargo clear sound
- Fail sound
- Win sound
- Button sound

---

## 11. Data Types

### CargoColor enum

```csharp
public enum CargoColor
{
    Red,
    Blue,
    Yellow,
    Green,
    Purple,
    Orange
}
```

### CargoData

```csharp
[System.Serializable]
public class CargoData
{
    public CargoColor color;
}
```

### CartData

```csharp
[System.Serializable]
public class CartData
{
    public Vector3 position;
    public CargoColor accentColor;
    public List<CargoColor> cargoSlots;
}
```

### LevelData

```csharp
[CreateAssetMenu(menuName = "Color Cargo Loop/Level Data")]
public class LevelData : ScriptableObject
{
    public int levelIndex;
    public int requiredCargoCount;
    public int maxLoopCapacity;
    public float cargoMoveSpeed;
    public List<CartData> carts;
}
```

---

## 12. Controls

### Main Control

```text
Tap cart -> release next cargo
```

### Booster Controls

```text
Tap Undo -> undo last cargo
Tap Shuffle -> shuffle remaining cargo
Tap Extra Slot -> increase loop capacity by 1
```

No drag controls in V1.

---

## 13. Feedback & Juice Requirements

Every important action must have feedback.

### Cart Tap

- Cart scale punch
- Cargo pop out
- Light SFX
- Light haptic

### Cargo Release

- Small trail
- Smooth movement
- Tiny particle burst

### Cargo Clear

- Color burst
- Score/progress pop
- SFX
- Haptic

### Wrong Tap

- Cart shake
- Low error sound
- Optional red blink

### Loop Almost Full

- Slight warning glow
- UI shake
- Warning sound

### Fail

- Camera shake
- Red flash
- Lose panel

### Win

- Confetti
- Progress badge pop
- Win panel

---

## 14. Art Asset Requirements

### 14.1 Essential Prefabs

Create these prefabs:

```text
CartPrefab
CargoPrefab_Red
CargoPrefab_Blue
CargoPrefab_Yellow
CargoPrefab_Green
LoopTrackPrefab
TargetZonePrefab
ClearVFXPrefab
TapVFXPrefab
ConfettiVFXPrefab
```

### 14.2 Materials

Recommended materials:

```text
MAT_Background_DarkPurple
MAT_Track_Purple
MAT_Track_InnerDark
MAT_Cargo_Red
MAT_Cargo_Blue
MAT_Cargo_Yellow
MAT_Cargo_Green
MAT_Cart_Red
MAT_Cart_Blue
MAT_Cart_Yellow
MAT_Cart_Green
MAT_UI_Purple
MAT_UI_Gold
```

### 14.3 Lighting

Use:

- Soft directional light
- Ambient purple/blue tone
- Slight bloom if URP is available
- Avoid heavy post-processing

---

## 15. Technical Direction

### 15.1 Use 3D, Not 2D

The gameplay should be implemented in 3D with simple meshes.

Reason:

- Better premium look
- Easier rounded toy visuals
- Better motion readability
- Better ad creative output

### 15.2 Use Orthographic Camera

Use orthographic camera for mobile puzzle clarity.

No dynamic camera movement required.

Optional:

- Tiny camera shake on clear/fail
- Very small zoom punch on win

### 15.3 Avoid Physics for Core Logic

Do not rely on Rigidbody physics for sorting logic.

Use deterministic code:

- Path positions
- Slot lists
- Active cargo list
- Capacity checks

Physics can be used only for cosmetic effects if needed.

### 15.4 Object Pooling

Use pooling for cargo pieces and VFX.

If time is short, simple instantiate/destroy is acceptable for first prototype, but code should be easy to replace with pooling.

### 15.5 Mobile Performance

Keep it light:

- Low-poly rounded meshes
- Few particles
- No expensive real-time shadows if unnecessary
- Simple materials
- Stable 60 FPS target

---

## 16. First Prototype Scope

The first prototype must include:

- 1 gameplay scene
- 10–30 levels
- Cart tapping
- Cargo release
- Conveyor movement
- Color collection
- Progress
- Win
- Lose
- Restart
- Next level
- Undo booster
- Shuffle booster
- Extra capacity booster
- Basic SFX
- Basic haptic
- Simple VFX
- Polished UI

Do not include:

- Complex meta
- Skins
- Shop
- Battle pass
- Daily rewards
- Advanced procedural generation
- Complicated obstacles

These can be added later.

---

## 17. Three-Day Production Plan

## Day 1 - Core Gameplay

Goal: playable base loop.

Tasks:

- Create scene
- Set orthographic camera
- Build loop path
- Create cart prefab
- Create cargo prefab
- Implement `LevelData`
- Implement `LevelManager`
- Implement `CartController`
- Implement `CargoController`
- Implement `ConveyorPath`
- Tap cart to release cargo
- Cargo moves along path
- Matching cargo clears
- Progress increases
- Win condition

Day 1 success condition:

```text
Player can complete a simple level by tapping carts and clearing cargo.
```

---

## Day 2 - Fail, UI, Boosters, Levels

Goal: make it game-like.

Tasks:

- Add top HUD
- Add progress badge
- Add booster buttons
- Add lose condition
- Add loop capacity logic
- Add undo booster
- Add shuffle booster
- Add extra capacity booster
- Add 10–20 levels
- Add restart and next level flow
- Add simple tutorial hand for level 1

Day 2 success condition:

```text
The game has multiple levels, win/lose flow, boosters, and playable progression.
```

---

## Day 3 - Polish & Closed Test Build

Goal: make it testable and visually acceptable.

Tasks:

- Improve materials
- Add VFX
- Add SFX
- Add haptic
- Add button animations
- Add cart punch animation
- Add cargo clear effect
- Add win/lose panel polish
- Fix bugs
- Build Android test APK/AAB
- Record 3–5 fail creatives

Day 3 success condition:

```text
Game is stable enough for closed testing and ad creative recording.
```

---

## 18. Suggested Initial Level Data

### Level 1

```text
requiredCargoCount: 4
maxLoopCapacity: 8
colors: Red, Blue
carts:
- Cart 1: Red, Blue
- Cart 2: Blue, Red
```

### Level 2

```text
requiredCargoCount: 6
maxLoopCapacity: 8
colors: Red, Blue, Yellow
carts:
- Cart 1: Red, Yellow
- Cart 2: Blue, Red
- Cart 3: Yellow, Blue
```

### Level 3

```text
requiredCargoCount: 8
maxLoopCapacity: 10
colors: Red, Blue, Yellow
carts:
- Cart 1: Yellow, Red
- Cart 2: Red, Blue
- Cart 3: Blue, Yellow
- Cart 4: Red, Yellow
```

### Level 4

```text
requiredCargoCount: 10
maxLoopCapacity: 10
colors: Red, Blue, Yellow, Green
carts:
- Cart 1: Red, Yellow
- Cart 2: Blue, Green
- Cart 3: Yellow, Blue
- Cart 4: Green, Red
```

### Level 5

```text
requiredCargoCount: 12
maxLoopCapacity: 9
colors: Red, Blue, Yellow, Green
carts:
- Cart 1: Red, Red, Yellow
- Cart 2: Blue, Green, Red
- Cart 3: Yellow, Blue, Green
- Cart 4: Green, Yellow, Blue
```

---

## 19. Game Feel Reference

The game should feel:

- Easy to start
- Lightly strategic
- Satisfying
- Juicy
- Fail-friendly
- Colorful
- Smooth
- Premium casual

The player should say:

> "One more level."

The ad viewer should say:

> "I can solve this."

---

## 20. Important Originality Rule

This project should not be a direct clone of any existing game.

Allowed:

- Use general sorting puzzle mechanics
- Use conveyor/loop structure
- Use color matching logic
- Use one-tap interaction

Not allowed:

- Copy exact UI layout from any specific game
- Copy exact colors, icons, vehicles, track shape, or level designs
- Copy store assets or screenshots
- Copy brand identity

The game should have its own identity:

```text
Cute cargo delivery / toy package sorting puzzle
```

The main theme should be **cargo, parcels, delivery carts, toy logistics**, not the same visual identity as any reference.

---

## 21. Naming Ideas

Possible names:

- Color Cargo Loop
- Cargo Sort Loop
- Parcel Loop
- Cargo Jam Sort
- Loop Cargo Puzzle
- Toy Cargo Sort
- Parcel Flow
- Color Delivery Jam
- Cargo Conveyor
- Sort Express

Recommended working title:

```text
Color Cargo Loop
```

---

## 22. Store Positioning

Short description:

```text
Tap, sort, and clear colorful cargo in a satisfying loop puzzle!
```

Longer positioning:

```text
Color Cargo Loop is a relaxing yet challenging sorting puzzle where you release colorful packages from cute cargo carts onto a moving loop. Match colors, avoid traffic jams, use boosters, and clear every level with smart taps.
```

Keywords:

```text
sort puzzle
color puzzle
cargo puzzle
loop puzzle
conveyor puzzle
tap puzzle
brain puzzle
relaxing puzzle
casual puzzle
```

---

## 23. Closed Test Target

The first closed test build should measure:

- Is the core mechanic understandable?
- Do players continue after level 1?
- Where do players fail?
- Are levels too easy or too confusing?
- Is the visual style attractive?
- Are fail moments good for ads?
- Does the game feel satisfying?

Do not overthink monetization before confirming the core loop.

---

## 24. Final Production Priority

Priority order:

1. Core mechanic works
2. Level completion works
3. Fail condition works
4. UI is readable
5. Visuals are clean
6. Feedback feels satisfying
7. 20+ levels exist
8. Closed test build is stable
9. Ad creatives can be recorded

Do not spend too much time on advanced systems before the first test.

---

# Final Direction

Build Color Cargo Loop as a fast, clean, premium-looking 3D hybrid-casual puzzle game.

The game must be:

```text
simple to play
easy to understand
fast to produce
visually polished
expandable
ad-creative friendly
closed-test ready in 3 days
```

The first version should focus only on the core loop:

```text
tap cart -> release cargo -> cargo moves on loop -> matching color clears -> progress fills -> win/fail
```

Once this is fun and stable, add obstacles, more levels, booster monetization, and ad/IAP systems.
