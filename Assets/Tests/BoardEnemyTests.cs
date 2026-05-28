using NUnit.Framework;
using PokoPuzzle.Core;

namespace Tests
{
    public sealed class BoardEnemyTests
    {
        [Test]
        public void Constructor_Default_CreatesAliveEnemy()
        {
            var enemy = new BoardEnemy();
            Assert.AreEqual(100, enemy.MaxHp);
            Assert.AreEqual(100, enemy.CurrentHp);
            Assert.AreEqual(500, enemy.DefeatBonus);
            Assert.AreEqual("Monster", enemy.Name);
            Assert.AreEqual(1, enemy.Wave);
            Assert.IsFalse(enemy.IsDefeated);
        }

        [Test]
        public void Constructor_WithParams_SetsCorrectValues()
        {
            var enemy = new BoardEnemy(100, 500, "Test Boss", 3);
            Assert.AreEqual(100, enemy.MaxHp);
            Assert.AreEqual(100, enemy.CurrentHp);
            Assert.AreEqual(500, enemy.DefeatBonus);
            Assert.AreEqual("Test Boss", enemy.Name);
            Assert.AreEqual(3, enemy.Wave);
        }

        [Test]
        public void ApplyDamage_ReducesHp()
        {
            var enemy = new BoardEnemy(100, 500, "Test", 1);
            var dealt = enemy.ApplyDamage(30);
            Assert.AreEqual(30, dealt);
            Assert.AreEqual(70, enemy.CurrentHp);
            Assert.IsFalse(enemy.IsDefeated);
        }

        [Test]
        public void ApplyDamage_ExactKill_DefeatsEnemy()
        {
            var enemy = new BoardEnemy(50, 200, "Test", 1);
            enemy.ApplyDamage(50);
            Assert.AreEqual(0, enemy.CurrentHp);
            Assert.IsTrue(enemy.IsDefeated);
        }

        [Test]
        public void ApplyDamage_Overkill_CapsAtZero()
        {
            var enemy = new BoardEnemy(30, 100, "Test", 1);
            var dealt = enemy.ApplyDamage(50);
            Assert.AreEqual(30, dealt);
            Assert.AreEqual(0, enemy.CurrentHp);
            Assert.IsTrue(enemy.IsDefeated);
        }

        [Test]
        public void ApplyDamage_AlreadyDefeated_ReturnsZero()
        {
            var enemy = new BoardEnemy(10, 100, "Test", 1);
            enemy.ApplyDamage(10);
            var dealt = enemy.ApplyDamage(20);
            Assert.AreEqual(0, dealt);
            Assert.AreEqual(0, enemy.CurrentHp);
            Assert.IsTrue(enemy.IsDefeated);
        }

        [Test]
        public void HpRatio_FullHealth_ReturnsOne()
        {
            var enemy = new BoardEnemy(80, 300, "Test", 1);
            Assert.AreEqual(1f, enemy.HpRatio);
        }

        [Test]
        public void HpRatio_HalfHealth_ReturnsHalf()
        {
            var enemy = new BoardEnemy(100, 400, "Test", 1);
            enemy.ApplyDamage(50);
            Assert.AreEqual(0.5f, enemy.HpRatio);
        }

        [Test]
        public void HpRatio_Defeated_ReturnsZero()
        {
            var enemy = new BoardEnemy(20, 100, "Test", 1);
            enemy.ApplyDamage(20);
            Assert.AreEqual(0f, enemy.HpRatio);
        }

        [Test]
        public void MultipleDamageCalls_Accumulates()
        {
            var enemy = new BoardEnemy(100, 500, "Test", 1);
            enemy.ApplyDamage(20);
            enemy.ApplyDamage(30);
            enemy.ApplyDamage(40);
            Assert.AreEqual(10, enemy.CurrentHp);
            Assert.IsFalse(enemy.IsDefeated);
            enemy.ApplyDamage(10);
            Assert.IsTrue(enemy.IsDefeated);
        }
    }
}
