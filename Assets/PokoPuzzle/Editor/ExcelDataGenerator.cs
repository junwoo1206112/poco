#if UNITY_EDITOR
using System.IO;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using UnityEditor;
using UnityEngine;
using PokoPuzzle.Core.Data;

namespace PokoPuzzle.Editor
{
    public static class ExcelDataGenerator
    {
        private static readonly string DataDir = "Assets/PokoPuzzle/Data/Excel";
        private static readonly string GeneratedDir = "Assets/PokoPuzzle/Data/Resources";
        private static readonly string ReportDir = "md/cli-reports";
        private static readonly string GameDataFileName = "GameData.xlsx";

        [MenuItem("Tools/Poko Puzzle/Generate Excel Data Files")]
        public static void GenerateAll()
        {
            Directory.CreateDirectory(DataDir);
            GenerateGameData();
            AssetDatabase.Refresh();
            Debug.Log($"[ExcelDataGenerator] Data files generated in {DataDir}");
        }

        [MenuItem("Tools/Poko Puzzle/Refresh All From Excel &#x")]
        public static void RefreshAll()
        {
            Directory.CreateDirectory(DataDir);
            Directory.CreateDirectory(GeneratedDir);
            GenerateGameData();
            var sourcePath = Path.Combine(DataDir, GameDataFileName);
            ConvertBossData(sourcePath);
            ConvertRegularEnemyData(sourcePath);
            ConvertSkillData(sourcePath);
            ConvertBalanceProfileData(sourcePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[ExcelDataGenerator] Refreshed all. Ready to play.");
        }

        [MenuItem("Tools/Poko Puzzle/Convert Excel Data To Assets")]
        public static void ConvertAll()
        {
            Directory.CreateDirectory(GeneratedDir);
            var sourcePath = EnsureGameData();
            ConvertBossData(sourcePath);
            ConvertRegularEnemyData(sourcePath);
            ConvertSkillData(sourcePath);
            ConvertBalanceProfileData(sourcePath);
            WriteConversionReport(sourcePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[ExcelDataGenerator] Converted Excel data into ScriptableObject databases in {GeneratedDir}");
        }

        private static void ConvertBossData(string sourcePath)
        {
            var provider = ExcelDataImporter.LoadEnemyData(sourcePath, "Boss");
            var assetPath = Path.Combine(GeneratedDir, "PokoEnemyDatabase.asset").Replace("\\", "/");
            var database = AssetDatabase.LoadAssetAtPath<PokoEnemyDatabase>(assetPath);
            if (database == null)
            {
                database = ScriptableObject.CreateInstance<PokoEnemyDatabase>();
                AssetDatabase.CreateAsset(database, assetPath);
            }

            database.Configure(provider.GetAllWaves());
            EditorUtility.SetDirty(database);
        }

        private static void ConvertRegularEnemyData(string sourcePath)
        {
            var provider = ExcelDataImporter.LoadRegularEnemyData(sourcePath, "Enemy");
            var assetPath = Path.Combine(GeneratedDir, "PokoRegularEnemyDatabase.asset").Replace("\\", "/");
            var database = AssetDatabase.LoadAssetAtPath<PokoRegularEnemyDatabase>(assetPath);
            if (database == null)
            {
                database = ScriptableObject.CreateInstance<PokoRegularEnemyDatabase>();
                AssetDatabase.CreateAsset(database, assetPath);
            }

            database.Configure(provider.GetAllEnemies());
            EditorUtility.SetDirty(database);
        }

        private static void ConvertSkillData(string sourcePath)
        {
            var provider = ExcelDataImporter.LoadSkillData(sourcePath, "Skill");
            var assetPath = Path.Combine(GeneratedDir, "PokoEnemySkillDatabase.asset").Replace("\\", "/");
            var database = AssetDatabase.LoadAssetAtPath<PokoEnemySkillDatabase>(assetPath);
            if (database == null)
            {
                database = ScriptableObject.CreateInstance<PokoEnemySkillDatabase>();
                AssetDatabase.CreateAsset(database, assetPath);
            }

            database.Configure(provider.GetAllSkills());
            EditorUtility.SetDirty(database);
        }

        private static void ConvertBalanceProfileData(string sourcePath)
        {
            var provider = ExcelDataImporter.LoadBalanceProfileData(sourcePath, "BalanceProfile");
            var assetPath = Path.Combine(GeneratedDir, "PokoBalanceProfileDatabase.asset").Replace("\\", "/");
            var database = AssetDatabase.LoadAssetAtPath<PokoBalanceProfileDatabase>(assetPath);
            if (database == null)
            {
                database = ScriptableObject.CreateInstance<PokoBalanceProfileDatabase>();
                AssetDatabase.CreateAsset(database, assetPath);
            }

            database.Configure(provider.GetAllProfiles());
            EditorUtility.SetDirty(database);
        }

        private static string EnsureGameData()
        {
            Directory.CreateDirectory(DataDir);
            var path = Path.Combine(DataDir, GameDataFileName);
            if (!File.Exists(path))
            {
                GenerateGameData();
            }

            return path;
        }

        private static void GenerateGameData()
        {
            Directory.CreateDirectory(DataDir);
            var path = Path.Combine(DataDir, GameDataFileName);

            using var workbook = new XSSFWorkbook();
            WriteSheet(
                workbook,
                "Enemy",
                new[] { "EnemyId", "Name", "HP", "ScoreBonus", "Role", "Enabled", "PortraitPath", "BackgroundPath" },
                new[]
                {
                    new[] { "1", "Frostbinder Imp", "45", "50", "Normal", "TRUE", "EnemyPortraits/frostbinder_imp", "EnemyBackgrounds/frostbinder_imp" },
                    new[] { "2", "Rime Wisp", "60", "75", "Flying", "TRUE", "EnemyPortraits/rime_wisp", "EnemyBackgrounds/rime_wisp" },
                    new[] { "3", "Granite Mite", "50", "60", "Normal", "TRUE", "EnemyPortraits/granite_mite", "EnemyBackgrounds/granite_mite" },
                    new[] { "4", "Crystal Guard", "75", "90", "Armored", "TRUE", "EnemyPortraits/crystal_guard", "EnemyBackgrounds/crystal_guard" },
                    new[] { "5", "Prism Bat", "70", "120", "Flying", "TRUE", "EnemyPortraits/prism_bat", "EnemyBackgrounds/prism_bat" },
                    new[] { "6", "Chroma Shifter", "55", "55", "Dark", "TRUE", "EnemyPortraits/chroma_shifter", "EnemyBackgrounds/chroma_shifter" },
                    new[] { "7", "Icebound Larva", "85", "85", "Normal", "TRUE", "EnemyPortraits/icebound_larva", "EnemyBackgrounds/icebound_larva" },
                    new[] { "8", "Stonehide Serpent", "40", "45", "Armored", "TRUE", "EnemyPortraits/stonehide_serpent", "EnemyBackgrounds/stonehide_serpent" },
                    new[] { "9", "Hue Imp", "70", "70", "Dark", "TRUE", "EnemyPortraits/hue_imp", "EnemyBackgrounds/hue_imp" },
                    new[] { "10", "Chroma Wisp", "80", "110", "Flying", "TRUE", "EnemyPortraits/chroma_wisp", "EnemyBackgrounds/chroma_wisp" },
                    new[] { "11", "Basalt Crawler", "85", "95", "Petrified", "TRUE", "EnemyPortraits/basalt_crawler", "EnemyBackgrounds/basalt_crawler" },
                    new[] { "12", "Onyx Gargoyle", "110", "130", "Petrified", "TRUE", "EnemyPortraits/onyx_gargoyle", "EnemyBackgrounds/onyx_gargoyle" }
                });
            WriteSheet(
                workbook,
                "Boss",
                new[] { "Wave", "Name", "HP", "DefeatBonus", "Enabled", "PortraitPath", "BackgroundPath" },
                new[]
                {
                    new[] { "1", "Frostbind Queen", "240", "500", "TRUE", "EnemyPortraits/frostbind_queen", "EnemyBackgrounds/frostbind_queen" },
                    new[] { "2", "Stoneheart Golem", "300", "550", "TRUE", "EnemyPortraits/stoneheart_golem", "EnemyBackgrounds/stoneheart_golem" },
                    new[] { "3", "Prism Trickster", "380", "600", "TRUE", "EnemyPortraits/prism_trickster", "EnemyBackgrounds/prism_trickster" },
                    new[] { "4", "Blizzard Wyrm", "480", "750", "TRUE", "EnemyPortraits/blizzard_wyrm", "EnemyBackgrounds/blizzard_wyrm" },
                    new[] { "5", "Chroma Overlord", "650", "1000", "TRUE", "EnemyPortraits/chroma_overlord", "EnemyBackgrounds/chroma_overlord" },
                    new[] { "6", "Obsidian Basilisk", "760", "1200", "TRUE", "EnemyPortraits/obsidian_basilisk", "EnemyBackgrounds/obsidian_basilisk" }
                });
            WriteSheet(
                workbook,
                "Skill",
                new[] { "Wave", "Target", "SkillType", "TargetCount", "CooldownSec", "Param1", "Enabled" },
                new[]
                {
                    new[] { "1", "Board", "freeze", "3", "12", "", "TRUE" },
                    new[] { "2", "Board", "stone", "2", "15", "", "TRUE" },
                    new[] { "3", "Board", "colorswap", "3", "14", "", "TRUE" },
                    new[] { "4", "Board", "freeze", "5", "10", "", "TRUE" },
                    new[] { "5", "Board", "colorswap", "5", "10", "", "TRUE" },
                    new[] { "6", "Board", "petrify", "3", "16", "", "TRUE" }
                });
            WriteSheet(
                workbook,
                "BalanceProfile",
                new[] { "ProfileId", "DisplayName", "MinAvailableChains", "TargetAverageChainLength", "FeverGaugeMultiplier", "RefillAssistRate", "BlockerBudget", "RegularEnemyHpMultiplier", "BossHpMultiplier", "SkillCooldownMultiplier", "Enabled" },
                new[]
                {
                    new[] { "default", "Default", "3", "4.0", "1.0", "0.15", "2", "1.0", "1.0", "1.0", "TRUE" },
                    new[] { "readable", "Readable", "4", "4.5", "1.3", "0.25", "1", "0.85", "0.85", "1.2", "TRUE" },
                    new[] { "pressure", "Pressure", "2", "3.5", "0.8", "0.08", "4", "1.2", "1.25", "0.85", "TRUE" },
                    new[] { "combo", "Combo", "3", "5.0", "1.1", "0.18", "2", "1.15", "1.15", "1.0", "TRUE" }
                });

            using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write))
            {
                workbook.Write(stream);
            }

            Debug.Log($"[ExcelDataGenerator] Created {GameDataFileName}");
        }

        private static void WriteSheet(XSSFWorkbook workbook, string sheetName, string[] headers, string[][] rows)
        {
            var sheet = workbook.CreateSheet(sheetName);
            WriteRow(sheet, 0, headers);
            for (var index = 0; index < rows.Length; index++)
            {
                WriteRow(sheet, index + 1, rows[index]);
            }

            for (var index = 0; index < headers.Length; index++)
            {
                sheet.AutoSizeColumn(index);
                var minWidth = Mathf.Max(headers[index].Length + 2, 8) * 256;
                if (sheet.GetColumnWidth(index) < minWidth)
                {
                    sheet.SetColumnWidth(index, minWidth);
                }
            }
        }

        private static void WriteRow(ISheet sheet, int rowIndex, string[] values)
        {
            var row = sheet.CreateRow(rowIndex);
            for (var index = 0; index < values.Length; index++)
            {
                row.CreateCell(index).SetCellValue(values[index]);
            }
        }

        private static void WriteConversionReport(string sourcePath)
        {
            Directory.CreateDirectory(ReportDir);
            var bossProvider = ExcelDataImporter.LoadEnemyData(sourcePath, "Boss");
            var enemyProvider = ExcelDataImporter.LoadRegularEnemyData(sourcePath, "Enemy");
            var skillProvider = ExcelDataImporter.LoadSkillData(sourcePath, "Skill");
            var profileProvider = ExcelDataImporter.LoadBalanceProfileData(sourcePath, "BalanceProfile");
            var body =
                "# Excel Data Conversion Report\n\n" +
                "## Source\n\n" +
                $"- Workbook: `{sourcePath.Replace("\\", "/")}`\n" +
                "- Enemy sheet: `Enemy`\n" +
                "- Boss sheet: `Boss`\n" +
                "- Skill sheet: `Skill`\n" +
                "- Balance profile sheet: `BalanceProfile`\n\n" +
                "## Result\n\n" +
                $"- Imported regular enemies: `{enemyProvider.GetAllEnemies().Count}`\n" +
                $"- Imported boss waves: `{bossProvider.GetAllWaves().Count}`\n" +
                $"- Imported enemy skills: `{skillProvider.GetAllSkills().Count}`\n" +
                $"- Imported balance profiles: `{profileProvider.GetAllProfiles().Count}`\n" +
                "- Disabled rows are skipped when `Enabled` is `FALSE`, `0`, `NO`, `OFF`, or `DELETE`.\n" +
                "- Enemy and boss background transitions load from each row's `BackgroundPath` Resources key.\n" +
                "- Deleted Excel rows disappear from the generated databases on the next conversion.\n";

            File.WriteAllText(Path.Combine(ReportDir, "excel-data-conversion.md"), body);
        }
    }
}
#endif
