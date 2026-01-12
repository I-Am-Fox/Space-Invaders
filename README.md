# Space Invaders (Roguelike prototype)

This repo is a C#/.NET prototype of a **roguelike Space Invaders**.

- `SpaceInvaders.Core` contains the deterministic simulation (entities, waves, upgrades).
- `SpaceInvaders.Wpf` is the WPF GUI runner (recommended).
- `Space Invaders` is a simple console runner.

## Controls (WPF)

- Move: **Left/Right Arrow** or **A/D**
- Shoot: **Space**
- Pause: **P**
- Quit: **Esc** (closes window)
- When offered upgrades: press **1/2/3** or click the buttons

## How it works (high level)

- Each wave spawns a block of aliens.
- Clearing a wave drafts 3 random upgrades; pick one to improve your ship.
- The game loop is step-based and uses a seedable RNG for deterministic runs.

## Build / run

```powershell
cd "C:\Users\josh-\RiderProjects\Space Invaders"
dotnet build

# WPF GUI
dotnet run --project "SpaceInvaders.Wpf\SpaceInvaders.Wpf.csproj"

# Console runner
dotnet run --project "Space Invaders\Space Invaders.csproj"
```

## Next ideas

- Multiple enemy types + elite waves
- Reward shop between waves (spend credits)
- More upgrade hooks: on-kill effects, shields, pierce, crits
- Save/load run seed for replays

