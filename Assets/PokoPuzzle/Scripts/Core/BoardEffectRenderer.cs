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
        private static Sprite ringSprite;
        private static Sprite shardSprite;
        private Coroutine shakeCoroutine;

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
            PlayCameraShake(feverActive ? 0.22f : 0.16f, feverActive ? 0.09f : 0.055f);
            foreach (var position in positions)
            {
                SpawnBurst(position, color, feverActive ? 0.42f : 0.32f, 0.28f);
                SpawnHexShards(position, color, feverActive ? 7 : 4);
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
            PlayCameraShake(0.28f, 0.08f);
        }

        public void PlayTimeBonus(Vector3 position, int seconds)
        {
            var color = new Color(1f, 0.86f, 0.18f, 1f);
            SpawnFloatingText($"TIME +{seconds}s", position + Vector3.up * 0.28f, color, 0.72f, 0.9f);
            SpawnBurst(position, Color.white, 0.42f, 0.1f);
            SpawnBurst(position, color, 0.5f, 0.22f);
            SpawnExpandingRing(position, color, 0.22f, 0.72f, 0.26f, 0f);
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

        public void PlayRainbowDetonation(Vector3 origin, IReadOnlyList<Vector3> positions, PokoTileType targetType, int gainedScore)
        {
            if (positions == null || positions.Count == 0)
            {
                return;
            }

            var targetColor = TileEffectColor(targetType);
            PlayCameraShake(0.34f, 0.085f);
            SpawnFloatingText($"{targetType} clear +{gainedScore}", origin + Vector3.up * 0.42f, new Color(0.95f, 0.62f, 1f, 1f), 0.82f, 0.95f);
            SpawnBurst(origin, Color.white, 0.92f, 0.16f);
            SpawnBurst(origin, new Color(0.85f, 0.35f, 1f, 0.88f), 0.95f, 0.32f);
            SpawnExpandingRing(origin, Color.white, 0.22f, 1.05f, 0.26f, 0.16f);
            SpawnExpandingRing(origin, new Color(0.95f, 0.45f, 1f, 0.92f), 0.34f, 1.28f, 0.36f, 0.26f);
            SpawnRainbowSweep(origin, 0.05f);
            SpawnRainbowSweep(origin, 0.18f);

            for (var index = 0; index < positions.Count; index++)
            {
                var rainbowColor = Color.HSVToRGB(Mathf.Repeat(index * 0.13f, 1f), 0.78f, 1f);
                rainbowColor.a = 0.9f;
                StartCoroutine(AnimatePrismZap(origin, positions[index], rainbowColor, 0.02f + index * 0.006f));
                StartCoroutine(AnimatePullMote(positions[index], origin, rainbowColor, 0.26f + index * 0.004f, 0.15f));
                SpawnTilePulse(positions[index], targetColor, 0.34f, 0.24f);
                StartCoroutine(AnimateRainbowDissolve(positions[index], rainbowColor, 0.34f + index * 0.004f));
            }
        }

        public void PlayBombPull(Vector3 origin, IReadOnlyList<Vector3> positions, BombType bombType, int gainedScore)
        {
            if (bombType == BombType.Red)
            {
                PlayRedBombEffect(origin, positions, gainedScore);
            }
            else
            {
                PlayBlueBombEffect(origin, positions, gainedScore);
            }
        }

        private void PlayRedBombEffect(Vector3 origin, IReadOnlyList<Vector3> positions, int gainedScore)
        {
            var color = new Color(1f, 0.32f, 0.18f, 1f);
            PlayCameraShake(0.28f, 0.07f);
            SpawnFloatingText(gainedScore > 0 ? $"RED BOMB +{gainedScore}" : "RED BOMB",
                origin + Vector3.up * 0.42f, color, 0.78f, 0.92f);

            SpawnBurst(origin, Color.white, 0.78f, 0.12f);
            SpawnBurst(origin, new Color(1f, 0.48f, 0.08f, 0.92f), 0.82f, 0.22f);
            SpawnExpandingRing(origin, new Color(1f, 0.8f, 0.16f, 0.88f), 0.2f, 0.86f, 0.18f, 0.02f);
            SpawnExpandingRing(origin, color, 0.32f, 1.04f, 0.25f, 0.1f);
            SpawnHexShards(origin, new Color(1f, 0.6f, 0.1f, 1f), 14);

            if (positions != null && positions.Count > 0)
            {
                StartCoroutine(AnimateRedFireLines(origin, positions, color));
            }
        }

        private void PlayBlueBombEffect(Vector3 origin, IReadOnlyList<Vector3> positions, int gainedScore)
        {
            var color = new Color(0.22f, 0.62f, 1f, 1f);
            PlayCameraShake(0.3f, 0.07f);
            SpawnFloatingText(gainedScore > 0 ? $"BLUE BOMB +{gainedScore}" : "BLUE BOMB",
                origin + Vector3.up * 0.42f, color, 0.82f, 0.78f);

            SpawnBurst(origin, Color.white, 0.65f, 0.15f);
            SpawnContractingRing(origin, new Color(0.45f, 0.85f, 1f, 0.85f), 1.35f, 0.38f, 0.28f, 0.02f);
            SpawnContractingRing(origin, new Color(0.1f, 0.45f, 1f, 0.7f), 1.65f, 0.24f, 0.4f, 0.12f);

            if (positions != null && positions.Count > 0)
            {
                StartCoroutine(AnimateBlueVortex(origin, positions, color));
            }
        }

        private IEnumerator AnimateRedFireLines(Vector3 origin, IReadOnlyList<Vector3> positions, Color color)
        {
            var rays = new List<List<Vector3>>();
            for (var index = 0; index < positions.Count; index++)
            {
                var diff = positions[index] - origin;
                var dist = diff.magnitude;
                if (dist < 0.01f)
                {
                    continue;
                }

                var angle = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;
                var bucket = Mathf.RoundToInt(angle / 60f);
                while (bucket < 0)
                {
                    bucket += 6;
                }

                bucket %= 6;
                while (rays.Count <= bucket)
                {
                    rays.Add(new List<Vector3>());
                }

                rays[bucket].Add(positions[index]);
            }

            var running = 0;
            foreach (var ray in rays)
            {
                if (ray.Count == 0)
                {
                    continue;
                }

                ray.Sort((left, right) => Vector3.Distance(left, origin).CompareTo(Vector3.Distance(right, origin)));
                running++;
                StartCoroutine(AnimateSingleFireLine(origin, ray, color, () => running--));
            }

            while (running > 0)
            {
                yield return null;
            }

            SpawnBurst(origin, Color.white, 0.76f, 0.14f);
            SpawnBurst(origin, new Color(1f, 0.7f, 0.1f, 0.9f), 0.82f, 0.24f);
            SpawnHexShards(origin, new Color(1f, 0.7f, 0.15f, 1f), 18);
            PlayCameraShake(0.18f, 0.055f);
        }

        private IEnumerator AnimateSingleFireLine(Vector3 origin, IReadOnlyList<Vector3> ray, Color color, System.Action onDone)
        {
            yield return new WaitForSeconds(0.02f);

            for (var index = 0; index < ray.Count; index++)
            {
                var pos = ray[index];
                var diff = pos - origin;
                var dist = diff.magnitude;
                var dir = diff.normalized;
                var brightColor = Color.Lerp(new Color(1f, 0.95f, 0.3f, 1f), color, Mathf.Clamp01(dist * 0.3f));

                SpawnFireSegment(pos, dir, brightColor);
                SpawnBurst(pos, Color.white, 0.42f, 0.07f);
                SpawnBurst(pos, brightColor, 0.48f, 0.15f);
                SpawnHexShards(pos, brightColor, 6);

                yield return new WaitForSeconds(0.028f);
            }

            onDone?.Invoke();
        }

        private IEnumerator AnimateBlueVortex(Vector3 origin, IReadOnlyList<Vector3> positions, Color color)
        {
            var count = positions.Count;
            if (count == 0)
            {
                yield break;
            }

            var targets = new List<(Vector3 pos, float dist)>();
            for (var index = 0; index < count; index++)
            {
                var d = Vector3.Distance(positions[index], origin);
                if (d < 0.01f) continue;
                targets.Add((positions[index], d));
            }

            if (targets.Count == 0)
            {
                yield break;
            }

            SpawnBurst(origin, Color.white, 0.7f, 0.15f);
            yield return new WaitForSeconds(0.04f);

            SpawnBurst(origin, new Color(0.3f, 0.7f, 1f, 0.3f), 0.9f, 0.2f);
            StartCoroutine(AnimateBlueSpiral(origin, color, 0.38f));

            var pullSpeed = 8.5f;
            var maxDuration = 0f;

            for (var index = 0; index < targets.Count; index++)
            {
                var startPos = targets[index].pos;
                var dist = targets[index].dist;
                var shakeDuration = 0.045f + dist * 0.015f;
                var pullDuration = dist / pullSpeed;
                maxDuration = Mathf.Max(maxDuration, shakeDuration + pullDuration);
                StartCoroutine(AnimatePullToCenter(startPos, origin, color, shakeDuration, pullDuration));
            }

            yield return new WaitForSeconds(0.06f + maxDuration * 0.6f);

            PlayCameraShake(0.2f, 0.06f);
            SpawnBurst(origin, Color.white, 0.7f, 0.18f);
            SpawnBurst(origin, new Color(0.5f, 0.85f, 1f, 1f), 0.65f, 0.32f);
            SpawnHexShards(origin, new Color(0.45f, 0.75f, 1f, 1f), 20);
        }

        private IEnumerator AnimateBlueSpiral(Vector3 origin, Color color, float duration)
        {
            const int moteCount = 14;
            for (var index = 0; index < moteCount; index++)
            {
                var angle = Mathf.PI * 2f * index / moteCount;
                var radius = 0.95f;
                var start = origin + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius;
                StartCoroutine(AnimateSpiralMote(start, origin, color, duration, index * 0.012f));
            }

            yield return null;
        }

        private IEnumerator AnimateSpiralMote(Vector3 start, Vector3 end, Color color, float duration, float delay)
        {
            yield return new WaitForSeconds(delay);

            var effect = new GameObject("Effect_BlueSpiral");
            effect.transform.SetParent(transform);
            var renderer = effect.AddComponent<SpriteRenderer>();
            renderer.sprite = GetCircleSprite();
            renderer.color = new Color(color.r, color.g, color.b, 0.82f);
            renderer.sortingOrder = 28;
            effect.transform.localScale = Vector3.one * 0.13f;

            var time = 0f;
            while (time < duration && effect != null)
            {
                var t = time / duration;
                var eased = 1f - Mathf.Pow(1f - t, 2.2f);
                var angle = t * Mathf.PI * 5f;
                var radius = Mathf.Lerp(Vector3.Distance(start, end), 0.04f, eased);
                var swirl = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius * 0.22f;
                effect.transform.position = Vector3.Lerp(start, end, eased) + swirl + Vector3.back * 0.35f;
                effect.transform.localScale = Vector3.one * Mathf.Lerp(0.13f, 0.03f, t);
                time += Time.deltaTime;
                yield return null;
            }

            if (effect != null)
            {
                Destroy(effect);
            }
        }

        private IEnumerator AnimatePullToCenter(Vector3 start, Vector3 end, Color color,
            float shakeDuration, float pullDuration)
        {
            var effect = new GameObject("Effect_BluePull");
            effect.transform.SetParent(transform);
            var renderer = effect.AddComponent<SpriteRenderer>();
            renderer.sprite = GetCircleSprite();
            renderer.color = color;
            renderer.sortingOrder = 27;
            var startScale = Random.Range(0.22f, 0.3f);
            effect.transform.localScale = Vector3.one * startScale;
            effect.transform.position = new Vector3(start.x, start.y, -0.35f);

            var time = 0f;
            while (time < shakeDuration && effect != null)
            {
                var tremor = new Vector3(
                    Mathf.Sin(Time.time * 40f + Random.Range(0f, 6f)) * 0.05f,
                    Mathf.Cos(Time.time * 32f + Random.Range(0f, 6f)) * 0.05f,
                    0f);
                effect.transform.position = new Vector3(start.x, start.y, -0.35f) + tremor;
                var pulse = 1f + Mathf.Sin(Time.time * 25f) * 0.14f;
                effect.transform.localScale = Vector3.one * startScale * pulse;
                time += Time.deltaTime;
                yield return null;
            }

            time = 0f;
            while (time < pullDuration && effect != null)
            {
                var t = time / pullDuration;
                var eased = 1f - Mathf.Pow(1f - t, 2.5f);
                var spiral = new Vector3(
                    Mathf.Sin(t * Mathf.PI * 12f) * 0.09f * (1f - t),
                    Mathf.Cos(t * Mathf.PI * 9f) * 0.09f * (1f - t),
                    0f);
                effect.transform.position = Vector3.Lerp(start, end, eased) + spiral + Vector3.back * 0.35f;
                effect.transform.localScale = Vector3.one * Mathf.Lerp(startScale, 0.02f, t);
                time += Time.deltaTime;
                yield return null;
            }

            if (effect != null)
            {
                Destroy(effect);
            }

            SpawnBurst(end, Color.white, 0.28f, 0.08f);
            SpawnBurst(end, new Color(0.5f, 0.85f, 1f, 0.8f), 0.3f, 0.14f);
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

        private void SpawnTilePulse(Vector3 position, Color color, float maxScale, float duration)
        {
            SpawnBurst(position, Color.white, maxScale * 0.78f, duration * 0.45f);
            SpawnExpandingRing(position, color, 0.2f, maxScale, duration, 0f);
        }

        private void SpawnFireSegment(Vector3 position, Vector3 direction, Color color)
        {
            var effect = new GameObject("Effect_RedFireLine");
            effect.transform.SetParent(transform);
            effect.transform.position = new Vector3(position.x, position.y, -0.39f);
            effect.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
            effect.transform.localScale = new Vector3(0.46f, 0.13f, 1f);

            var renderer = effect.AddComponent<SpriteRenderer>();
            renderer.sprite = GetCircleSprite();
            renderer.color = new Color(color.r, color.g, color.b, 0.86f);
            renderer.sortingOrder = 28;
            StartCoroutine(AnimateFireSegment(effect.transform, renderer, 0.16f));
        }

        private void SpawnRainbowSweep(Vector3 position, float delay)
        {
            for (var index = 0; index < 6; index++)
            {
                var color = Color.HSVToRGB(index / 6f, 0.76f, 1f);
                color.a = 0.62f;
                SpawnExpandingRing(position, color, 0.18f + index * 0.04f, 1.45f + index * 0.16f, 0.42f, delay + index * 0.025f);
            }
        }

        private void SpawnExpandingRing(Vector3 position, Color color, float startScale, float endScale, float duration, float delay)
        {
            var effect = new GameObject("Effect_Ring");
            effect.transform.SetParent(transform);
            effect.transform.position = new Vector3(position.x, position.y, -0.37f);
            var renderer = effect.AddComponent<SpriteRenderer>();
            renderer.sprite = GetRingSprite();
            renderer.color = color;
            renderer.sortingOrder = 29;
            effect.transform.localScale = Vector3.one * startScale;
            StartCoroutine(AnimateRing(effect.transform, renderer, startScale, endScale, duration, delay));
        }

        private void SpawnContractingRing(Vector3 position, Color color, float startScale, float endScale, float duration, float delay)
        {
            var effect = new GameObject("Effect_ContractRing");
            effect.transform.SetParent(transform);
            effect.transform.position = new Vector3(position.x, position.y, -0.37f);
            var renderer = effect.AddComponent<SpriteRenderer>();
            renderer.sprite = GetRingSprite();
            renderer.color = color;
            renderer.sortingOrder = 29;
            effect.transform.localScale = Vector3.one * startScale;
            StartCoroutine(AnimateRing(effect.transform, renderer, startScale, endScale, duration, delay));
        }

        private void SpawnHexShards(Vector3 position, Color color, int count)
        {
            for (var index = 0; index < count; index++)
            {
                var effect = new GameObject("Effect_HexShard");
                effect.transform.SetParent(transform);
                effect.transform.position = new Vector3(position.x, position.y, -0.42f);
                effect.transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
                effect.transform.localScale = Vector3.one * Random.Range(0.07f, 0.12f);

                var renderer = effect.AddComponent<SpriteRenderer>();
                renderer.sprite = GetShardSprite();
                renderer.color = new Color(color.r, color.g, color.b, 0.92f);
                renderer.sortingOrder = 26;

                var angle = (Mathf.PI * 2f * index / Mathf.Max(1, count)) + Random.Range(-0.28f, 0.28f);
                var velocity = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * Random.Range(0.55f, 0.95f);
                StartCoroutine(AnimateShard(effect.transform, renderer, velocity, 0.34f));
            }
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

        private IEnumerator AnimateRing(Transform target, SpriteRenderer renderer,
            float startScale, float endScale, float duration, float delay)
        {
            if (delay > 0f)
            {
                yield return new WaitForSeconds(delay);
            }

            var time = 0f;
            var startColor = renderer.color;
            while (time < duration && target != null && renderer != null)
            {
                var t = time / duration;
                var eased = 1f - Mathf.Pow(1f - t, 2.4f);
                target.localScale = Vector3.one * Mathf.Lerp(startScale, endScale, eased);
                var color = startColor;
                color.a = Mathf.Lerp(startColor.a, 0f, t);
                renderer.color = color;
                time += Time.deltaTime;
                yield return null;
            }

            if (target != null)
            {
                Destroy(target.gameObject);
            }
        }

        private IEnumerator AnimateFireSegment(Transform target, SpriteRenderer renderer, float duration)
        {
            var time = 0f;
            var startColor = renderer.color;
            var startScale = target.localScale;
            while (time < duration && target != null && renderer != null)
            {
                var t = time / duration;
                target.localScale = new Vector3(
                    Mathf.Lerp(startScale.x, startScale.x * 1.35f, t),
                    Mathf.Lerp(startScale.y, 0.02f, t),
                    1f);
                var color = startColor;
                color.a = Mathf.Lerp(startColor.a, 0f, t);
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

        private IEnumerator AnimateShard(Transform target, SpriteRenderer renderer, Vector3 velocity, float duration)
        {
            var time = 0f;
            var startColor = renderer.color;
            var spin = Random.Range(-360f, 360f);

            while (time < duration && target != null && renderer != null)
            {
                var t = time / duration;
                target.position += velocity * Time.deltaTime;
                target.Rotate(0f, 0f, spin * Time.deltaTime);
                target.localScale = Vector3.one * Mathf.Lerp(target.localScale.x, 0.02f, t * 0.65f);

                var color = startColor;
                color.a = Mathf.Lerp(startColor.a, 0f, t);
                renderer.color = color;

                velocity = Vector3.Lerp(velocity, Vector3.down * 0.25f, Time.deltaTime * 1.8f);
                time += Time.deltaTime;
                yield return null;
            }

            if (target != null)
            {
                Destroy(target.gameObject);
            }
        }

        private void PlayCameraShake(float duration, float strength)
        {
            if (boardCamera == null)
            {
                return;
            }

            if (shakeCoroutine != null)
            {
                StopCoroutine(shakeCoroutine);
            }

            shakeCoroutine = StartCoroutine(ShakeCamera(duration, strength));
        }

        private IEnumerator ShakeCamera(float duration, float strength)
        {
            var target = boardCamera.transform;
            var basePosition = target.position;
            var time = 0f;

            while (time < duration && target != null)
            {
                var t = time / duration;
                var damp = 1f - t;
                var offset = new Vector3(
                    Random.Range(-strength, strength) * damp,
                    Random.Range(-strength, strength) * damp,
                    0f);
                target.position = basePosition + offset;
                time += Time.deltaTime;
                yield return null;
            }

            if (target != null)
            {
                target.position = basePosition;
            }

            shakeCoroutine = null;
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

        private IEnumerator AnimatePullMote(Vector3 start, Vector3 end, Color color, float duration, float startScale)
        {
            var effect = new GameObject("Effect_PullMote");
            effect.transform.SetParent(transform);
            var renderer = effect.AddComponent<SpriteRenderer>();
            renderer.sprite = GetCircleSprite();
            renderer.color = color;
            renderer.sortingOrder = 27;
            effect.transform.localScale = Vector3.one * startScale;

            var time = 0f;
            while (time < duration && effect != null)
            {
                var t = time / duration;
                var eased = 1f - Mathf.Pow(1f - t, 3f);
                var wobble = new Vector3(Mathf.Sin(t * Mathf.PI * 2f) * 0.045f, Mathf.Cos(t * Mathf.PI * 2f) * 0.045f, 0f);
                effect.transform.position = Vector3.Lerp(start, end, eased) + wobble + Vector3.back * 0.35f;
                effect.transform.localScale = Vector3.one * Mathf.Lerp(startScale, 0.055f, t);
                time += Time.deltaTime;
                yield return null;
            }

            if (effect != null)
            {
                Destroy(effect);
            }

            SpawnBurst(end, color, 0.2f, 0.16f);
        }

        private IEnumerator AnimatePrismZap(Vector3 start, Vector3 end, Color color, float delay)
        {
            yield return new WaitForSeconds(delay);

            var diff = end - start;
            var distance = diff.magnitude;
            if (distance < 0.01f)
            {
                yield break;
            }

            var effect = new GameObject("Effect_PrismZap");
            effect.transform.SetParent(transform);
            var renderer = effect.AddComponent<SpriteRenderer>();
            renderer.sprite = GetCircleSprite();
            renderer.color = color;
            renderer.sortingOrder = 28;

            var angle = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;
            effect.transform.rotation = Quaternion.Euler(0f, 0f, angle);

            var duration = 0.16f;
            var time = 0f;
            while (time < duration && effect != null)
            {
                var t = time / duration;
                var head = Vector3.Lerp(start, end, 1f - Mathf.Pow(1f - t, 2f));
                effect.transform.position = head + Vector3.back * 0.36f;
                effect.transform.localScale = new Vector3(Mathf.Lerp(0.18f, 0.04f, t), 0.06f, 1f);
                var zapColor = color;
                zapColor.a = Mathf.Lerp(color.a, 0f, t);
                renderer.color = zapColor;
                time += Time.deltaTime;
                yield return null;
            }

            if (effect != null)
            {
                Destroy(effect);
            }

            SpawnBurst(end, Color.white, 0.34f, 0.08f);
            SpawnBurst(end, color, 0.3f, 0.16f);
        }

        private IEnumerator AnimateRainbowDissolve(Vector3 position, Color color, float delay)
        {
            yield return new WaitForSeconds(delay);

            for (var index = 0; index < 5; index++)
            {
                var shardColor = Color.HSVToRGB(Mathf.Repeat(index * 0.18f + delay, 1f), 0.72f, 1f);
                shardColor.a = 0.92f;
                var offset = new Vector3(
                    Random.Range(-0.08f, 0.08f),
                    Random.Range(-0.08f, 0.08f),
                    0f);
                SpawnBurst(position + offset, index % 2 == 0 ? Color.white : shardColor, 0.2f + index * 0.04f, 0.12f);
            }

            SpawnHexShards(position, color, 9);
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

        private static Color TileEffectColor(PokoTileType tileType)
        {
            return tileType switch
            {
                PokoTileType.Red => new Color(1f, 0.25f, 0.25f, 0.82f),
                PokoTileType.Blue => new Color(0.22f, 0.58f, 1f, 0.82f),
                PokoTileType.Green => new Color(0.25f, 0.9f, 0.38f, 0.82f),
                PokoTileType.Yellow => new Color(1f, 0.78f, 0.14f, 0.82f),
                PokoTileType.Purple => new Color(0.72f, 0.34f, 1f, 0.82f),
                _ => new Color(1f, 0.52f, 0.18f, 0.82f)
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

        private static Sprite GetRingSprite()
        {
            if (ringSprite != null)
            {
                return ringSprite;
            }

            const int size = 96;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            var outerRadius = size * 0.46f;
            var innerRadius = size * 0.34f;
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var distance = Vector2.Distance(new Vector2(x, y), center);
                    var outerAlpha = Mathf.Clamp01(1f - Mathf.Abs(distance - outerRadius) / 4.5f);
                    var innerCut = distance < innerRadius ? 0f : 1f;
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, outerAlpha * innerCut));
                }
            }

            texture.Apply();
            ringSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            return ringSprite;
        }

        private static Sprite GetShardSprite()
        {
            if (shardSprite != null)
            {
                return shardSprite;
            }

            const int size = 48;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            var radius = size * 0.42f;
            var vertices = new Vector2[6];

            for (var index = 0; index < 6; index++)
            {
                var angle = Mathf.Deg2Rad * (60f * index + 30f);
                vertices[index] = new Vector2(
                    center.x + radius * Mathf.Cos(angle),
                    center.y + radius * Mathf.Sin(angle));
            }

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    texture.SetPixel(x, y, IsInsideHex(new Vector2(x, y), vertices)
                        ? Color.white
                        : new Color(0f, 0f, 0f, 0f));
                }
            }

            texture.Apply();
            shardSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            return shardSprite;
        }

        private static bool IsInsideHex(Vector2 point, Vector2[] vertices)
        {
            for (var index = 0; index < 6; index++)
            {
                var next = (index + 1) % 6;
                var edge = vertices[next] - vertices[index];
                var toPoint = point - vertices[index];
                var cross = edge.x * toPoint.y - edge.y * toPoint.x;
                if (cross < 0f)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
