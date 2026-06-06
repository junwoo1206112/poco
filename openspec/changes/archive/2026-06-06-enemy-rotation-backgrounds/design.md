# Design

## Data Contract

Enemy and boss Excel rows gain a `BackgroundPath` Resources key beside `PortraitPath`. The generated ScriptableObject databases copy the value into `RegularEnemyData` and `EnemyWaveData`, and `BoardEnemy` carries it at runtime.

## Runtime

`LineLinkerBoard` already owns the enemy rotation. After each `SpawnNextEnemy` call creates the active `BoardEnemy`, it asks `BoardBackgroundRenderer` to load and fade in the enemy background. The renderer loads from `Resources`, caches sprites by path, and scales the sprite to cover the orthographic camera.

## Asset Direction

The placeholder backgrounds are original abstract stage plates grouped by enemy theme: ice, stone, prism, dark chroma, and obsidian. They support portfolio readability without depending on protected reference art.

## Evidence

Implementation evidence is the OpenSpec change, updated Excel workbook, generated Resources PNGs, and Unity code paths that make the backgrounds playable in the prototype scene.
