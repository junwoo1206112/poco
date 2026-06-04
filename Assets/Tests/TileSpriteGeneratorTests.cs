using NUnit.Framework;
using PokoPuzzle.Core;
using UnityEngine;

namespace Tests
{
    public sealed class TileSpriteGeneratorTests
    {
        [Test]
        public void CircleInHexSprites_UseDistinctTileColors()
        {
            var sprites = TileSpriteGenerator.CreateTileSprites(PokoTileVisualStyle.CircleInHex);

            var red = CenterColor(sprites[(int)PokoTileType.Red]);
            var blue = CenterColor(sprites[(int)PokoTileType.Blue]);
            var green = CenterColor(sprites[(int)PokoTileType.Green]);

            Assert.Greater(red.r, red.b);
            Assert.Greater(blue.b, blue.r);
            Assert.Greater(green.g, green.r);
        }

        [Test]
        public void CircleInHexSprites_AreNotGrayscaleTiles()
        {
            var sprites = TileSpriteGenerator.CreateTileSprites(PokoTileVisualStyle.CircleInHex);
            var red = CenterColor(sprites[(int)PokoTileType.Red]);

            Assert.Greater(Mathf.Abs(red.r - red.g), 0.12f);
        }

        private static Color CenterColor(Sprite sprite)
        {
            var texture = sprite.texture;
            return texture.GetPixel(texture.width / 2, texture.height / 2);
        }
    }
}
