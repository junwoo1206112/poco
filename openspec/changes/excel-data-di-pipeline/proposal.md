## Why

Excel `.xlsx` files are a good authoring format for stage and enemy tuning, but the runtime board should not open spreadsheets during play. The portfolio should show a production-style flow where NPOI is used in editor tooling, then converted Unity assets are injected into gameplay.

## What Changes

- Add ScriptableObject databases for stage and enemy data.
- Convert Excel data into those databases in the Unity editor.
- Inject the databases into `LineLinkerBoard` through serialized references.
- Keep runtime gameplay on provider interfaces instead of direct `.xlsx` file reads.

## Capabilities

### New Capabilities

- `excel-data-pipeline`: Excel-authored gameplay data can be converted into runtime-safe Unity assets and injected into gameplay.

### Modified Capabilities

- `line-linker-puzzle`: Runtime board tuning can come from injected data databases instead of direct spreadsheet reads.

## Impact

- `Assets/PokoPuzzle/Scripts/Core/Data/` - runtime data database assets and provider contracts.
- `Assets/PokoPuzzle/Editor/` - Excel generation/conversion menu tools.
- `Assets/PokoPuzzle/Scripts/Core/LineLinkerBoard.cs` - serialized data provider injection.
- `md/` - portfolio evidence describing the data pipeline.
