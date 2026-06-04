# Pokopang Tile Audit - 2026-06-04

## Web Reference Baseline

- LINE PokoPang is described as a timed line-linking game where players connect 3 or more adjoining blocks of the same color and longer chains reward attacks, cherries, bombs, and Fever.
- Public screenshots and descriptions consistently frame the board as colorful hexagonal pieces that must be read quickly by color.
- POKOPOKO special-item help describes line-clearing bombs, surrounding-area bombs, and rainbow bombs as distinct special item identities.

Sources:

- https://pokopang.en.uptodown.com/android
- https://treenod.helpshift.com/hc/en/12-pokopoko/faq/114-what-are-special-items-and-what-kinds-are-there/
- https://www.linecorp.com/en/pr/news/en/2017/1981

## Findings

- The active prototype scene uses `CircleInHex`, but `TileSpriteGenerator.FillCircleStyle` generated grayscale tiles. This broke the core line-linker read: color should be the first thing the player sees.
- Frozen and Stone overlays multiplied the whole tile strongly enough that the underlying color identity could be lost. Blocker state should be visible, but it should not make the board feel like unrelated gray pieces.
- `BlueBombRadius` was set to 4. On the current compact 4x9 board this reads like a near-board-wide clear, not a surrounding-area bomb.
- The current input fixes remain directionally right: the board needs center-based tile fallback and current-drag cancellation when enemy skills change selected tiles.

## Changes

- `CircleInHex` tiles now render as saturated colored round blocks inside a darker rim, with a bright type mark.
- Frozen and Stone status tinting was softened so blocker state remains visible without erasing tile identity.
- Blue Bomb radius was restored to a two-ring hex radius so it reads as a strong local burst.
- Tests now assert that `CircleInHex` sprites are color-distinct and not grayscale.

## Verification Targets

- `dotnet build Assembly-CSharp.csproj`
- `dotnet build Tests.csproj`
- `dotnet build Assembly-CSharp-Editor.csproj`
- `openspec.cmd validate --all`
