#if UNITY_EDITOR
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using PokoPuzzle.Core.Data;

namespace PokoPuzzle.Editor
{
    public static class ExcelDataImporter
    {
        public static IEnemyDataProvider LoadEnemyData(string xlsxPath, string sheetName)
        {
            var waves = new List<EnemyWaveData>();

            using (var stream = new FileStream(xlsxPath, FileMode.Open, FileAccess.Read))
            using (var workbook = new XSSFWorkbook(stream))
            {
                var sheet = workbook.GetSheet(sheetName) ?? workbook.GetSheetAt(0);
                for (var i = 1; i <= sheet.LastRowNum; i++)
                {
                    var row = sheet.GetRow(i);
                    if (row == null)
                    {
                        continue;
                    }

                    if (!IsEnabled(row.GetCell(4)))
                    {
                        continue;
                    }

                    var wave = new EnemyWaveData
                    {
                        Wave = ParseInt(GetCellValue(row.GetCell(0))),
                        Name = GetCellValue(row.GetCell(1))?.Trim() ?? "Monster",
                        Hp = ParseInt(GetCellValue(row.GetCell(2))),
                        DefeatBonus = ParseInt(GetCellValue(row.GetCell(3)))
                    };

                    if (wave.Wave > 0)
                    {
                        waves.Add(wave);
                    }
                }
            }

            return new ExcelEnemyDataProvider(waves);
        }

        public static IRegularEnemyDataProvider LoadRegularEnemyData(string xlsxPath, string sheetName)
        {
            var enemies = new List<RegularEnemyData>();

            using (var stream = new FileStream(xlsxPath, FileMode.Open, FileAccess.Read))
            using (var workbook = new XSSFWorkbook(stream))
            {
                var sheet = workbook.GetSheet(sheetName) ?? workbook.GetSheetAt(0);
                for (var i = 1; i <= sheet.LastRowNum; i++)
                {
                    var row = sheet.GetRow(i);
                    if (row == null)
                    {
                        continue;
                    }

                    if (!IsEnabled(row.GetCell(5)))
                    {
                        continue;
                    }

                    var enemy = new RegularEnemyData
                    {
                        EnemyId = ParseInt(GetCellValue(row.GetCell(0))),
                        Name = GetCellValue(row.GetCell(1))?.Trim() ?? "Monster",
                        Hp = ParseInt(GetCellValue(row.GetCell(2))),
                        ScoreBonus = ParseInt(GetCellValue(row.GetCell(3))),
                        Role = GetCellValue(row.GetCell(4))?.Trim() ?? "Normal"
                    };

                    if (enemy.EnemyId > 0)
                    {
                        enemies.Add(enemy);
                    }
                }
            }

            return new ExcelRegularEnemyDataProvider(enemies);
        }

        public static IEnemySkillProvider LoadSkillData(string xlsxPath, string sheetName)
        {
            var skills = new List<EnemySkillEntry>();

            using (var stream = new FileStream(xlsxPath, FileMode.Open, FileAccess.Read))
            using (var workbook = new XSSFWorkbook(stream))
            {
                var sheet = workbook.GetSheet(sheetName) ?? workbook.GetSheetAt(0);
                for (var i = 1; i <= sheet.LastRowNum; i++)
                {
                    var row = sheet.GetRow(i);
                    if (row == null)
                    {
                        continue;
                    }

                    if (!IsEnabled(row.GetCell(6)))
                    {
                        continue;
                    }

                    var skillType = ParseSkillType(GetCellValue(row.GetCell(2)));
                    if (skillType == EnemySkillType.None)
                    {
                        continue;
                    }

                    skills.Add(new EnemySkillEntry
                    {
                        Wave = ParseInt(GetCellValue(row.GetCell(0))),
                        SkillType = skillType,
                        TargetCount = ParseInt(GetCellValue(row.GetCell(3))),
                        CooldownSec = ParseFloat(GetCellValue(row.GetCell(4)))
                    });
                }
            }

            return new ExcelSkillDataProvider(skills);
        }

        public static IBalanceProfileProvider LoadBalanceProfileData(string xlsxPath, string sheetName)
        {
            var profiles = new List<BalanceProfileData>();

            using (var stream = new FileStream(xlsxPath, FileMode.Open, FileAccess.Read))
            using (var workbook = new XSSFWorkbook(stream))
            {
                var sheet = workbook.GetSheet(sheetName);
                if (sheet == null)
                {
                    return new ExcelBalanceProfileProvider(profiles);
                }

                for (var i = 1; i <= sheet.LastRowNum; i++)
                {
                    var row = sheet.GetRow(i);
                    if (row == null)
                    {
                        continue;
                    }

                    if (!IsEnabled(row.GetCell(10)))
                    {
                        continue;
                    }

                    var profile = new BalanceProfileData
                    {
                        ProfileId = GetCellValue(row.GetCell(0))?.Trim() ?? string.Empty,
                        DisplayName = GetCellValue(row.GetCell(1))?.Trim() ?? string.Empty,
                        MinAvailableChains = ParseInt(GetCellValue(row.GetCell(2))),
                        TargetAverageChainLength = ParseFloat(GetCellValue(row.GetCell(3))),
                        RainbowSpawnWeight = ParseFloat(GetCellValue(row.GetCell(4))),
                        RefillAssistRate = ParseFloat(GetCellValue(row.GetCell(5))),
                        BlockerBudget = ParseInt(GetCellValue(row.GetCell(6))),
                        RegularEnemyHpMultiplier = ParseFloat(GetCellValue(row.GetCell(7))),
                        BossHpMultiplier = ParseFloat(GetCellValue(row.GetCell(8))),
                        SkillCooldownMultiplier = ParseFloat(GetCellValue(row.GetCell(9)))
                    };

                    if (!string.IsNullOrEmpty(profile.ProfileId))
                    {
                        profiles.Add(profile);
                    }
                }
            }

            return new ExcelBalanceProfileProvider(profiles);
        }

        private static EnemySkillType ParseSkillType(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return EnemySkillType.None;
            }

            var trimmed = value.Trim().ToLowerInvariant();
            return trimmed switch
            {
                "freeze" => EnemySkillType.Freeze,
                "stone" => EnemySkillType.Stone,
                "colorswap" or "color_swap" or "swap" => EnemySkillType.ColorSwap,
                _ => EnemySkillType.None
            };
        }

        private static string GetCellValue(ICell cell)
        {
            if (cell == null)
            {
                return null;
            }

            return cell.CellType switch
            {
                CellType.String => cell.StringCellValue,
                CellType.Numeric => cell.NumericCellValue.ToString(CultureInfo.InvariantCulture),
                CellType.Boolean => cell.BooleanCellValue.ToString(),
                CellType.Formula => cell.CellFormula,
                _ => null
            };
        }

        private static int ParseInt(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return 0;
            }

            return double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed)
                ? (int)parsed
                : 0;
        }

        private static float ParseFloat(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return 0f;
            }

            return float.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed)
                ? parsed
                : 0f;
        }

        private static bool IsEnabled(ICell cell)
        {
            var value = GetCellValue(cell);
            if (string.IsNullOrWhiteSpace(value))
            {
                return true;
            }

            var normalized = value.Trim();
            return !string.Equals(normalized, "false", System.StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(normalized, "0", System.StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(normalized, "no", System.StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(normalized, "off", System.StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(normalized, "delete", System.StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(normalized, "deleted", System.StringComparison.OrdinalIgnoreCase);
        }

        private sealed class ExcelEnemyDataProvider : IEnemyDataProvider
        {
            private readonly List<EnemyWaveData> waves;

            public ExcelEnemyDataProvider(List<EnemyWaveData> waves)
            {
                this.waves = waves;
            }

            public IReadOnlyList<EnemyWaveData> GetAllWaves()
            {
                return waves;
            }

            public EnemyWaveData GetWave(int wave)
            {
                foreach (var enemyWave in waves)
                {
                    if (enemyWave.Wave == wave)
                    {
                        return enemyWave;
                    }
                }

                return waves.Count > 0
                    ? waves[^1]
                    : new EnemyWaveData { Wave = wave, Hp = 100, DefeatBonus = 500 };
            }
        }

        private sealed class ExcelRegularEnemyDataProvider : IRegularEnemyDataProvider
        {
            private readonly List<RegularEnemyData> enemies;

            public ExcelRegularEnemyDataProvider(List<RegularEnemyData> enemies)
            {
                this.enemies = enemies;
            }

            public RegularEnemyData GetEnemy(int enemyId)
            {
                foreach (var enemy in enemies)
                {
                    if (enemy.EnemyId == enemyId)
                    {
                        return enemy;
                    }
                }

                return enemies.Count > 0
                    ? enemies[0]
                    : new RegularEnemyData { EnemyId = enemyId, Name = "Monster", Hp = 30, ScoreBonus = 50, Role = "Normal" };
            }

            public IReadOnlyList<RegularEnemyData> GetAllEnemies()
            {
                return enemies;
            }
        }

        private sealed class ExcelSkillDataProvider : IEnemySkillProvider
        {
            private readonly List<EnemySkillEntry> skills;

            public ExcelSkillDataProvider(List<EnemySkillEntry> skills)
            {
                this.skills = skills;
            }

            public IReadOnlyList<EnemySkillEntry> GetAllSkills() => skills;

            public IReadOnlyList<EnemySkillEntry> GetSkillsForWave(int wave)
            {
                var result = new List<EnemySkillEntry>();
                foreach (var entry in skills)
                {
                    if (entry.Wave == wave)
                    {
                        result.Add(entry);
                    }
                }

                return result;
            }
        }

        private sealed class ExcelBalanceProfileProvider : IBalanceProfileProvider
        {
            private readonly List<BalanceProfileData> profiles;

            public ExcelBalanceProfileProvider(List<BalanceProfileData> profiles)
            {
                this.profiles = profiles;
            }

            public BalanceProfileData GetProfile(string profileId)
            {
                foreach (var profile in profiles)
                {
                    if (profile.ProfileId == profileId)
                    {
                        return profile;
                    }
                }

                return profiles.Count > 0
                    ? profiles[0]
                    : new BalanceProfileData { ProfileId = "default", DisplayName = "Default" };
            }

            public IReadOnlyList<BalanceProfileData> GetAllProfiles()
            {
                return profiles;
            }
        }
    }
}
#endif
