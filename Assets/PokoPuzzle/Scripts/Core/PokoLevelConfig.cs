using UnityEngine;

namespace PokoPuzzle.Core
{
    [CreateAssetMenu(menuName = "Poko Puzzle/Level Config", fileName = "PokoLevelConfig")]
    public sealed class PokoLevelConfig : ScriptableObject
    {
        [SerializeField] private string levelId = "level_001";
        [SerializeField] private int width = 7;
        [SerializeField] private int height = 7;
        [SerializeField] private int tileTypes = 5;
        [SerializeField] private bool useHexGrid = true;
        [SerializeField] private int moveLimit = 20;
        [SerializeField] private int targetScore = 10000;
        [SerializeField] private int[] spawnWeights = { 100, 100, 100, 100, 100 };
        [SerializeField] private int regularEnemyHp;
        [SerializeField] private int bossHp;
        [SerializeField] private string balanceProfileId = "default";

        public string LevelId => levelId;
        public int Width => width;
        public int Height => height;
        public int TileTypes => tileTypes;
        public bool UseHexGrid => useHexGrid;
        public int MoveLimit => moveLimit;
        public int TargetScore => targetScore;
        public int[] SpawnWeights => spawnWeights;
        public int RegularEnemyHp => regularEnemyHp;
        public int BossHp => bossHp;
        public string BalanceProfileId => string.IsNullOrWhiteSpace(balanceProfileId) ? "default" : balanceProfileId;

        public void Configure(string newLevelId, int newWidth, int newHeight, int newTileTypes, bool newUseHexGrid, int newMoveLimit, int newTargetScore, int[] newSpawnWeights, int newRegularEnemyHp = 0, int newBossHp = 0, string newBalanceProfileId = "default")
        {
            levelId = newLevelId;
            width = Mathf.Max(3, newWidth);
            height = Mathf.Max(3, newHeight);
            tileTypes = Mathf.Clamp(newTileTypes, 3, 6);
            useHexGrid = newUseHexGrid;
            moveLimit = Mathf.Max(1, newMoveLimit);
            targetScore = Mathf.Max(100, newTargetScore);
            spawnWeights = NormalizeWeights(newSpawnWeights, tileTypes);
            regularEnemyHp = Mathf.Max(0, newRegularEnemyHp);
            bossHp = Mathf.Max(0, newBossHp);
            balanceProfileId = string.IsNullOrWhiteSpace(newBalanceProfileId) ? "default" : newBalanceProfileId;
        }

        private static int[] NormalizeWeights(int[] source, int count)
        {
            var weights = new int[count];
            for (var index = 0; index < count; index++)
            {
                var value = source != null && index < source.Length ? source[index] : 100;
                weights[index] = Mathf.Max(1, value);
            }

            return weights;
        }
    }
}
