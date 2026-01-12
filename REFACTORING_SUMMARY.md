# Project Structure Refactoring Summary

## Overview
Reorganized the entire Space Invaders project for maximum clarity and maintainability. All folders and files now have clear, descriptive names that make the codebase easy to navigate.

---

## SpaceInvaders.Core (Game Logic Library)

### Before → After Changes:

#### Folder Structure
- **`Util/` → `Random/`**
  - More descriptive - contains only RNG-related code
  - Files: `IRng.cs`, `SeededRng.cs`
  - Namespace: `SpaceInvaders.Core.Random`

- **`Math/` → `Primitives/`**
  - Eliminated namespace collision with `System.Math`
  - Contains fundamental value types
  - Files: `GridPoint.cs` (renamed from `Int2.cs`)
  - Namespace: `SpaceInvaders.Core.Primitives`

- **Moved `GameCommand.cs` from root → `Engine/`**
  - Now lives with other game loop types
  - Namespace: `SpaceInvaders.Core.Engine`

- **Renamed `GlobalUsings.Engine.cs` → `GlobalUsings.cs`**
  - Simpler, clearer name
  - Location: `Engine/GlobalUsings.cs`

#### Type Renames
- **`Int2` → `GridPoint`**
  - More descriptive name for a 2D grid coordinate
  - Makes it obvious this represents a position on the game grid

### Final Core Structure
```
SpaceInvaders.Core/
├── Engine/              (Game loop, session, commands, config)
│   ├── Game.cs
│   ├── GameCommand.cs
│   ├── GameConfig.cs
│   ├── GameSession.cs
│   └── GlobalUsings.cs
├── Model/               (Game state and entities)
│   ├── Entity.cs
│   ├── EntityKind.cs
│   ├── GameState.cs
│   └── RunState.cs
├── Primitives/          (Fundamental value types)
│   └── GridPoint.cs
├── Random/              (RNG utilities)
│   ├── IRng.cs
│   └── SeededRng.cs
├── Upgrades/            (All upgrade system code)
│   ├── MetaApplication.cs
│   ├── MetaProgression.cs
│   ├── MetaUpgrade.cs
│   ├── MetaUpgradeCatalog.cs
│   ├── MetaUpgradeId.cs
│   ├── Upgrade.cs
│   ├── UpgradeCatalog.cs
│   ├── UpgradeDraft.cs
│   ├── UpgradeEffect.cs
│   ├── UpgradeId.cs
│   └── UpgradeRarity.cs
└── Sounds/              (Audio assets)
```

---

## SpaceInvaders.Wpf (WPF UI)

### Before → After Changes:

#### Major Reorganization
- **`Infrastructure/` → Deleted** (too vague)
  - Split into clear, purpose-specific folders

- **`Infrastructure/AudioManager.cs` → `Services/AudioManager.cs`**
  - Namespace: `SpaceInvaders.Wpf.Services`

- **`Infrastructure/ProfileStore.cs` → `Persistence/ProfileStore.cs`**
  - Namespace: `SpaceInvaders.Wpf.Persistence`

- **`Infrastructure/UiScaleHelper.cs` → `Helpers/UiScaleHelper.cs`**
  - Namespace: `SpaceInvaders.Wpf.Helpers`

- **`Infrastructure/ShellWindow.*` → Root of WPF project**
  - Main window should be at root level
  - Namespace: `SpaceInvaders.Wpf` (simplified from `SpaceInvaders.Wpf.Infrastructure`)

### Final WPF Structure
```
SpaceInvaders.Wpf/
├── Helpers/             (UI utility classes)
│   └── UiScaleHelper.cs
├── Persistence/         (Save/load functionality)
│   └── ProfileStore.cs
├── Services/            (App-wide services)
│   └── AudioManager.cs
├── Views/               (UI pages)
│   ├── GamePage.xaml/cs
│   ├── MainMenuPage.xaml/cs
│   ├── SettingsPage.xaml/cs
│   └── ShopPage.xaml/cs
├── ShellWindow.xaml/cs  (Main window at root)
├── App.xaml/cs
└── AssemblyInfo.cs
```

---

## Benefits of This Structure

### 1. **Eliminated Namespace Collisions**
   - `SpaceInvaders.Core.Math` → `SpaceInvaders.Core.Primitives` prevents conflict with `System.Math`
   - No more ambiguous method calls or qualified names needed

### 2. **Clear Folder Names**
   - Every folder name clearly describes its contents
   - No vague names like "Infrastructure" or "Util"
   - Easy to find what you need

### 3. **Proper Organization**
   - Game logic separate from UI
   - Related code grouped together
   - Clear separation of concerns

### 4. **Better Developer Experience**
   - New developers can understand the structure immediately
   - File locations are intuitive
   - Reduced cognitive load when navigating code

### 5. **Maintainability**
   - Easy to add new features in the right place
   - Clear ownership of functionality
   - Scales well as project grows

---

## All Builds Pass ✅
- Solution compiles successfully
- All namespaces updated correctly
- No breaking changes to functionality
- Only structural improvements

---

## Quick Reference: Where Things Live Now

| What                  | Where                              | Namespace                        |
|-----------------------|------------------------------------|----------------------------------|
| Game loop             | `Core/Engine/`                     | `SpaceInvaders.Core.Engine`      |
| Game state            | `Core/Model/`                      | `SpaceInvaders.Core.Model`       |
| Grid coordinates      | `Core/Primitives/GridPoint.cs`     | `SpaceInvaders.Core.Primitives`  |
| Random number gen     | `Core/Random/`                     | `SpaceInvaders.Core.Random`      |
| All upgrades          | `Core/Upgrades/`                   | `SpaceInvaders.Core.Upgrades`    |
| Audio system          | `Wpf/Services/AudioManager.cs`     | `SpaceInvaders.Wpf.Services`     |
| Profile save/load     | `Wpf/Persistence/ProfileStore.cs`  | `SpaceInvaders.Wpf.Persistence`  |
| UI helpers            | `Wpf/Helpers/`                     | `SpaceInvaders.Wpf.Helpers`      |
| Game pages            | `Wpf/Views/`                       | `SpaceInvaders.Wpf.Views`        |
| Main window           | `Wpf/ShellWindow.xaml`             | `SpaceInvaders.Wpf`              |

