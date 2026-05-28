using System.Collections;
using System.Collections.Generic;
using PokoPuzzle.Core.Data;
using UnityEngine;

namespace PokoPuzzle.Core
{
    public sealed class BoardEffectRenderer : MonoBehaviour
    {
        private Camera boardCamera;
        private static Sprite circleSprite;

        public void Configure(Camera camera)
        {
            boardCamera = camera;
        }

        public void PlayChainClear(IReadOnlyList<Vector3> positions, Vector3 center, int gainedScore, bool feverActive)
        {
            if (positions == null || positions.Count == 0)
            {
                return;
            }

            var color = feverActive ? new Color(1f, 0.55f, 0.12f, 0.95f) : new Color(1f, 0.9f, 0.35f, 0.95f);
            foreach (var position in positions)
            {
                SpawnBurst(position, color, feverActive ? 0.42f : 0.32f, 0.28f);
            }

            SpawnFloatingText($"+{gainedScore}", center + Vector3.up * 0.18f, color, 0.72f, 0.95f);
        }

        public void PlayDamage(Vector3 start, int damage, bool boss)
        {
            if (damage <= 0)
            {
                return;
            }

            var target = GetCombatTargetWorldPosition();
            var moteCount = damage >= 100 ? 3 : damage >= 60 ? 2 : 1;
            var color = boss ? new Color(1f, 0.3f, 0.12f, 0.95f) : new Color(0.55f, 0.88f, 1f, 0.95f);
            for (var index = 0; index < moteCount; index++)
            {
                var offset = new Vector3((index - (moteCount - 1) * 0.5f) * 0.16f, index * 0.04f, 0f);
                StartCoroutine(AnimateMote(start + offset, target, color, 0.28f + index * 0.04f));
            }

            SpawnFloatingText($"-{damage}", target + Vector3.up * 0.26f, color, 0.58f, 0.75f);
            SpawnBurst(target, color, boss ? 0.48f : 0.36f, 0.24f);
        }

        public void PlayBossSpawn(string bossName)
        {
            var target = GetCombatTargetWorldPosition();
            SpawnFloatingText($"BOSS {bossName}", target + Vector3.up * 0.55f, new Color(1f, 0.62f, 0.18f, 1f), 0.95f, 1.0f);
            SpawnBurst(target, new Color(1f, 0.18f, 0.08f, 0.82f), 0.7f, 0.35f);
        }

        public void PlayFeverStart()
        {
            var target = GetCombatTargetWorldPosition();
            SpawnFloatingText("FEVER!", target + Vector3.up * 0.95f, new Color(1f, 0.5f, 0.08f, 1f), 1.05f, 0.95f);
            SpawnBurst(target + Vector3.up * 0.75f, new Color(1f, 0.55f, 0.08f, 0.75f), 0.9f, 0.32f);
        }

        public void PlayBossSkill(EnemySkillType skillType, IReadOnlyList<PokoTile> targets)
        {
            var color = SkillColor(skillType);
            var label = skillType switch
            {
                EnemySkillType.Freeze => "FREEZE",
                EnemySkillType.Stone => "STONE",
                EnemySkillType.ColorSwap => "COLOR SWAP",
                _ => "SKILL"
            };

            var combatTarget = GetCombatTargetWorldPosition();
            SpawnFloatingText(label, combatTarget + Vector3.down * 0.18f, color, 0.62f, 0.72f);
            SpawnBurst(combatTarget, color, 0.44f, 0.24f);

            if (targets == null)
            {
                return;
            }

            foreach (var tile in targets)
            {
                if (tile != null)
                {
                    StartCoroutine(AnimateSkillSweep(combatTarget, tile.transform.position, color, 0.18f));
                }
            }
        }

        public void PlayRainbowPreview(IReadOnlyList<Vector3> positions, Vector3 center)
        {
            if (positions == null || positions.Count == 0)
            {
                return;
            }

            SpawnFloatingText("Rainbow target", center + Vector3.up * 0.22f, new Color(0.9f, 0.55f, 1f, 1f), 0.32f, 0.72f);
            for (var index = 0; index < positions.Count; index++)
            {
                var color = Color.HSVToRGB(Mathf.Repeat(index * 0.17f, 1f), 0.75f, 1f);
                color.a = 0.55f;
                SpawnBurst(positions[index], color, 0.24f, 0.18f);
            }
        }

        public void PlayRainbowClear(IReadOnlyList<Vector3> positions, Vector3 center, int gainedScore)
        {
            if (positions == null || positions.Count == 0)
            {
                return;
            }

            var colors = new[]
            {
                new Color(1f, 0.2f, 0.2f, 0.92f),
                new Color(1f, 0.7f, 0.1f, 0.92f),
                new Color(0.2f, 0.9f, 0.35f, 0.92f),
                new Color(0.25f, 0.55f, 1f, 0.92f),
                new Color(0.75f, 0.35f, 1f, 0.92f)
            };

            for (var index = 0; index < positions.Count; index++)
            {
                SpawnBurst(positions[index], colors[index % colors.Length], 0.42f, 0.34f);
            }

            SpawnFloatingText($"Rainbow +{gainedScore}", center + Vector3.up * 0.28f, new Color(0.85f, 0.45f, 1f, 1f), 0.78f, 1.05f);
        }

        private Vector3 GetCombatTargetWorldPosition()
        {
            if (boardCamera != null)
            {
                var point = boardCamera.ViewportToWorldPoint(new Vector3(0.5f, 0.82f, Mathf.Abs(boardCamera.transform.position.z)));
                point.z = -0.35f;
                return point;
            }

            return new Vector3(0f, 4.5f, -0.35f);
        }

        private void SpawnBurst(Vector3 position, Color color, float maxScale, float duration)
        {
            var effect = new GameObject("Effect_Burst");
            effect.transform.SetParent(transform);
            effect.transform.position = new Vector3(position.x, position.y, -0.35f);
            var renderer = effect.AddComponent<SpriteRenderer>();
            renderer.sprite = GetCircleSprite();
            renderer.color = color;
            renderer.sortingOrder = 20;
            StartCoroutine(AnimateBurst(effect.transform, renderer, maxScale, duration));
        }

        private void SpawnFloatingText(string text, Vector3 position, Color color, float duration, float size)
        {
            var effect = new GameObject("Effect_Text");
            effect.transform.SetParent(transform);
            effect.transform.position = new Vector3(position.x, position.y, -0.45f);
            var textMesh = effect.AddComponent<TextMesh>();
            textMesh.text = text;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.fontSize = 42;
            textMesh.characterSize = 0.07f * size;
            textMesh.color = color;
            var renderer = textMesh.GetComponent<MeshRenderer>();
            renderer.sortingOrder = 30;
            StartCoroutine(AnimateText(effect.transform, textMesh, duration));
        }

        private IEnumerator AnimateBurst(Transform target, SpriteRenderer renderer, float maxScale, float duration)
        {
            var time = 0f;
            while (time < duration && target != null && renderer != null)
            {
                var t = time / duration;
                var scale = Mathf.Lerp(0.08f, maxScale, Mathf.Sin(t * Mathf.PI));
                target.localScale = Vector3.one * scale;
                var color = renderer.color;
                color.a = Mathf.Lerp(color.a, 0f, t);
                renderer.color = color;
                time += Time.deltaTime;
                yield return null;
            }

            if (target != null)
            {
                Destroy(target.gameObject);
            }
        }

        private IEnumerator AnimateText(Transform target, TextMesh textMesh, float duration)
        {
            var start = target.position;
            var end = start + Vector3.up * 0.58f;
            var time = 0f;
            var startColor = textMesh.color;
            while (time < duration && target != null && textMesh != null)
            {
                var t = time / duration;
                target.position = Vector3.Lerp(start, end, t);
                var color = startColor;
                color.a = Mathf.Lerp(startColor.a, 0f, t);
                textMesh.color = color;
                time += Time.deltaTime;
                yield return null;
            }

            if (target != null)
            {
                Destroy(target.gameObject);
            }
        }

        private IEnumerator AnimateMote(Vector3 start, Vector3 end, Color color, float duration)
        {
            var effect = new GameObject("Effect_Mote");
            effect.transform.SetParent(transform);
            var renderer = effect.AddComponent<SpriteRenderer>();
            renderer.sprite = GetCircleSprite();
            renderer.color = color;
            renderer.sortingOrder = 25;
            effect.transform.localScale = Vector3.one * 0.11f;

            var time = 0f;
            while (time < duration && effect != null)
            {
                var t = time / duration;
                var arc = Mathf.Sin(t * Mathf.PI) * 0.38f;
                effect.transform.position = Vector3.Lerp(start, end, t) + Vector3.up * arc + Vector3.back * 0.35f;
                time += Time.deltaTime;
                yield return null;
            }

            if (effect != null)
            {
                Destroy(effect);
            }
        }

        private IEnumerator AnimateSkillSweep(Vector3 start, Vector3 end, Color color, float duration)
        {
            var effect = new GameObject("Effect_SkillSweep");
            effect.transform.SetParent(transform);
            var renderer = effect.AddComponent<SpriteRenderer>();
            renderer.sprite = GetCircleSprite();
            renderer.color = color;
            renderer.sortingOrder = 24;
            effect.transform.localScale = Vector3.one * 0.14f;

            var time = 0f;
            while (time < duration && effect != null)
            {
                var t = time / duration;
                effect.transform.position = Vector3.Lerp(start, end, t) + Vector3.back * 0.35f;
                time += Time.deltaTime;
                yield return null;
            }

            if (effect != null)
            {
                Destroy(effect);
            }

            SpawnBurst(end, color, 0.34f, 0.30f);
        }

        private static Color SkillColor(EnemySkillType skillType)
        {
            return skillType switch
            {
                EnemySkillType.Freeze => new Color(0.55f, 0.85f, 1f, 0.95f),
                EnemySkillType.Stone => new Color(0.62f, 0.62f, 0.62f, 0.95f),
                EnemySkillType.ColorSwap => new Color(1f, 0.45f, 0.95f, 0.95f),
                _ => Color.white
            };
        }

        private static Sprite GetCircleSprite()
        {
            if (circleSprite != null)
            {
                return circleSprite;
            }

            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            var radius = size * 0.45f;
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var distance = Vector2.Distance(new Vector2(x, y), center);
                    var alpha = Mathf.Clamp01(1f - distance / radius);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha * alpha));
                }
            }

            texture.Apply();
            circleSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            return circleSprite;
        }
    }
}
