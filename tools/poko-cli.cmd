@echo off
setlocal

set "PROJECT_DIR=%~dp0.."
set "UNITY_EXE=%UNITY_EXE%"

if "%UNITY_EXE%"=="" (
  set "UNITY_EXE=C:\Program Files\Unity\Hub\Editor\6000.3.11f1\Editor\Unity.exe"
)

if not exist "%UNITY_EXE%" (
  echo Unity executable not found: "%UNITY_EXE%"
  echo Set UNITY_EXE to your Unity.exe path and run again.
  exit /b 1
)

if "%~1"=="" goto usage

set "COMMAND=%~1"
shift

if /I "%COMMAND%"=="create-core-board" (
  "%UNITY_EXE%" -batchmode -quit -projectPath "%PROJECT_DIR%" -executeMethod PokoPuzzle.Editor.PokoPuzzleCli.CreateCoreBoardScene %*
  exit /b %ERRORLEVEL%
)

if /I "%COMMAND%"=="validate-core-board" (
  "%UNITY_EXE%" -batchmode -quit -projectPath "%PROJECT_DIR%" -executeMethod PokoPuzzle.Editor.PokoPuzzleCli.ValidateCoreBoardScene %*
  exit /b %ERRORLEVEL%
)

if /I "%COMMAND%"=="analyze-board" (
  "%UNITY_EXE%" -batchmode -quit -projectPath "%PROJECT_DIR%" -executeMethod PokoPuzzle.Editor.PokoPuzzleCli.AnalyzeBoard %*
  exit /b %ERRORLEVEL%
)

if /I "%COMMAND%"=="generate-level" (
  "%UNITY_EXE%" -batchmode -quit -projectPath "%PROJECT_DIR%" -executeMethod PokoPuzzle.Editor.PokoPuzzleCli.GenerateLevel %*
  exit /b %ERRORLEVEL%
)

if /I "%COMMAND%"=="apply-level" (
  "%UNITY_EXE%" -batchmode -quit -projectPath "%PROJECT_DIR%" -executeMethod PokoPuzzle.Editor.PokoPuzzleCli.ApplyLevel %*
  exit /b %ERRORLEVEL%
)

if /I "%COMMAND%"=="analyze-playlog" (
  "%UNITY_EXE%" -batchmode -quit -projectPath "%PROJECT_DIR%" -executeMethod PokoPuzzle.Editor.PokoPuzzleCli.AnalyzePlayLog %*
  exit /b %ERRORLEVEL%
)

if /I "%COMMAND%"=="retune-level" (
  "%UNITY_EXE%" -batchmode -quit -projectPath "%PROJECT_DIR%" -executeMethod PokoPuzzle.Editor.PokoPuzzleCli.RetuneLevel %*
  exit /b %ERRORLEVEL%
)

if /I "%COMMAND%"=="plan-level-experiments" (
  "%UNITY_EXE%" -batchmode -quit -projectPath "%PROJECT_DIR%" -executeMethod PokoPuzzle.Editor.PokoPuzzleCli.PlanLevelExperiments %*
  exit /b %ERRORLEVEL%
)

if /I "%COMMAND%"=="compare-level-experiments" (
  "%UNITY_EXE%" -batchmode -quit -projectPath "%PROJECT_DIR%" -executeMethod PokoPuzzle.Editor.PokoPuzzleCli.CompareLevelExperiments %*
  exit /b %ERRORLEVEL%
)

if /I "%COMMAND%"=="promote-experiment-winner" (
  "%UNITY_EXE%" -batchmode -quit -projectPath "%PROJECT_DIR%" -executeMethod PokoPuzzle.Editor.PokoPuzzleCli.PromoteExperimentWinner %*
  exit /b %ERRORLEVEL%
)

if /I "%COMMAND%"=="designer-loop-status" (
  "%UNITY_EXE%" -batchmode -quit -projectPath "%PROJECT_DIR%" -executeMethod PokoPuzzle.Editor.PokoPuzzleCli.DesignerLoopStatus %*
  exit /b %ERRORLEVEL%
)

if /I "%COMMAND%"=="llm-design-review" (
  "%UNITY_EXE%" -batchmode -quit -projectPath "%PROJECT_DIR%" -executeMethod PokoPuzzle.Editor.PokoPuzzleCli.LlmDesignReview %*
  exit /b %ERRORLEVEL%
)

if /I "%COMMAND%"=="convert-excel-data" (
  "%UNITY_EXE%" -batchmode -quit -projectPath "%PROJECT_DIR%" -executeMethod PokoPuzzle.Editor.PokoPuzzleCli.ConvertExcelData %*
  exit /b %ERRORLEVEL%
)

:usage
echo Poko Puzzle CLI
echo.
echo Usage:
echo   tools\poko-cli.cmd create-core-board [--layout hex] [--tileVisual circle-in-hex^|hex] [--scenePath Assets/Scenes/PokoPrototype.unity] [--width 4] [--height 13] [--tileTypes 5] [--spacing 0.74] [--reportPath md/cli-reports/core-board-input.md]
echo   tools\poko-cli.cmd validate-core-board [--scenePath Assets/Scenes/PokoPrototype.unity] [--reportPath md/cli-reports/core-board-validation.md]
echo   tools\poko-cli.cmd analyze-board [--layout hex] [--width 7] [--height 7] [--tileTypes 5] [--seed 1001] [--score 0] [--movesUsed 0] [--reportPath md/agent-reports/latest-board-analysis.md] [--jsonPath md/agent-reports/latest-board-analysis.json]
echo   tools\poko-cli.cmd generate-level [--levelId level_001] [--layout hex] [--width 7] [--height 7] [--tileTypes 5] [--seed 1001] [--assetPath Assets/PokoPuzzle/Data/Generated/level_001.asset]
echo   tools\poko-cli.cmd apply-level [--levelId level_001] [--scenePath Assets/Scenes/PokoPrototype.unity] [--assetPath Assets/PokoPuzzle/Data/Generated/level_001.asset]
echo   tools\poko-cli.cmd analyze-playlog [--logPath md/playtest-logs/latest-playtest.jsonl] [--reportPath md/agent-reports/latest-playtest-analysis.md]
echo   tools\poko-cli.cmd retune-level [--levelId level_002] [--logPath md/playtest-logs/latest-playtest.jsonl] [--assetPath Assets/PokoPuzzle/Data/Generated/level_002.asset]
echo   tools\poko-cli.cmd plan-level-experiments [--experimentId exp_001] [--logPath md/playtest-logs/latest-playtest.jsonl] [--reportPath md/experiment-reports/exp_001.md]
echo   tools\poko-cli.cmd compare-level-experiments [--experimentId exp_001] [--controlLog md/playtest-logs/by-level/exp_001_control-latest.jsonl] [--reportPath md/experiment-reports/exp_001-comparison.md]
echo   tools\poko-cli.cmd promote-experiment-winner [--experimentId exp_001] [--levelId exp_001_winner] [--applyScene true] [--reportPath md/experiment-reports/exp_001-promotion.md]
echo   tools\poko-cli.cmd designer-loop-status [--experimentId exp_001] [--reportPath md/designer-loop/latest-status.md] [--jsonPath md/designer-loop/latest-status.json]
echo   tools\poko-cli.cmd llm-design-review [--inputPath md/agent-reports/latest-playtest-analysis.json] [--model gpt-5.4-mini] [--reportPath md/llm-reports/latest-designer-review.md]
echo   tools\poko-cli.cmd convert-excel-data
echo.
echo Optional:
echo   set UNITY_EXE=C:\Program Files\Unity\Hub\Editor\6000.3.11f1\Editor\Unity.exe
exit /b 1
