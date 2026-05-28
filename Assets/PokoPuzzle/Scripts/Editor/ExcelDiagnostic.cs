#if UNITY_EDITOR
using System.IO;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using UnityEditor;
using UnityEngine;

namespace PokoPuzzle.Editor
{
    public static class ExcelDiagnostic
    {
        [MenuItem("Tools/Poko Puzzle/Diagnose Excel Boss Data")]
        public static void Diagnose()
        {
            var path = "Assets/PokoPuzzle/Data/Excel/GameData.xlsx";
            if (!File.Exists(path))
            {
                Debug.LogError($"[ExcelDiagnostic] Excel file not found: {path}");
                return;
            }

            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var workbook = new XSSFWorkbook(stream);

            var sheet = workbook.GetSheet("Boss");
            if (sheet == null)
            {
                Debug.LogError("[ExcelDiagnostic] 'Boss' sheet not found!");
                Debug.Log("Available sheets:");
                for (var i = 0; i < workbook.NumberOfSheets; i++)
                {
                    Debug.Log($"  - {workbook.GetSheetName(i)}");
                }
                return;
            }

            Debug.Log($"[ExcelDiagnostic] Boss sheet has {sheet.LastRowNum} rows (0-based)");

            for (var i = 0; i <= sheet.LastRowNum; i++)
            {
                var row = sheet.GetRow(i);
                if (row == null)
                {
                    Debug.Log($"  Row {i}: null");
                    continue;
                }

                var parts = new System.Collections.Generic.List<string>();
                for (var c = 0; c <= 4; c++)
                {
                    var cell = row.GetCell(c);
                    if (cell == null)
                    {
                        parts.Add($"Col{c}: NULL");
                    }
                    else
                    {
                        parts.Add($"Col{c}: type={cell.CellType} value='{GetDisplayValue(cell)}'");
                    }
                }

                Debug.Log($"  Row {i}: {string.Join(" | ", parts)}");
            }

            Debug.Log("[ExcelDiagnostic] Done. Check the generated PokoEnemyDatabase.asset:");
            var db = AssetDatabase.LoadAssetAtPath<ScriptableObject>("Assets/PokoPuzzle/Data/Resources/PokoEnemyDatabase.asset");
            Debug.Log($"  Database asset: {(db != null ? "FOUND" : "NULL")}");
        }

        private static string GetDisplayValue(ICell cell)
        {
            try
            {
                switch (cell.CellType)
                {
                    case CellType.String:
                        return cell.StringCellValue;
                    case CellType.Numeric:
                        return cell.NumericCellValue.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    case CellType.Boolean:
                        return cell.BooleanCellValue.ToString();
                    case CellType.Formula:
                        return cell.CellFormula;
                    default:
                        return $"(unhandled: {cell.CellType})";
                }
            }
            catch (System.Exception ex)
            {
                return $"(error: {ex.Message})";
            }
        }
    }
}
#endif
