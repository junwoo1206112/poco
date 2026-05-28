using NUnit.Framework;
using PokoPuzzle.Core;
using UnityEngine;

namespace Tests
{
    public sealed class PokoTileTypeTests
    {
        [Test]
        public void ToColor_Red_ReturnsExpectedColor()
        {
            var color = PokoTileType.Red.ToColor();
            Assert.AreEqual(0.95f, color.r, 0.01f);
            Assert.AreEqual(0.22f, color.g, 0.01f);
            Assert.AreEqual(0.28f, color.b, 0.01f);
        }

        [Test]
        public void ToColor_AllTypes_ReturnsNonTransparent()
        {
            foreach (PokoTileType type in System.Enum.GetValues(typeof(PokoTileType)))
            {
                var color = type.ToColor();
                Assert.Greater(color.a, 0.9f, $"{type} alpha is too low");
            }
        }

        [Test]
        public void ToColor_InvalidValue_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => ((PokoTileType)99).ToColor());
        }

        [Test]
        public void ToColor_EachTypeHasDistinctColor()
        {
            var colors = new System.Collections.Generic.HashSet<Color>();
            foreach (PokoTileType type in System.Enum.GetValues(typeof(PokoTileType)))
            {
                var added = colors.Add(type.ToColor());
                Assert.IsTrue(added, $"{type} has duplicate color");
            }
        }
    }
}
