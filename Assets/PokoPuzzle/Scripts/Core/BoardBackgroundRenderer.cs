using System.Collections.Generic;
using UnityEngine;

namespace PokoPuzzle.Core
{
    public sealed class BoardBackgroundRenderer : MonoBehaviour
    {
        [SerializeField] private float transitionDuration = 0.65f;
        [SerializeField] private Color fallbackColor = new(0.08f, 0.10f, 0.14f, 1f);

        private readonly Dictionary<string, Sprite> spriteCache = new();
        private Camera boardCamera;
        private SpriteRenderer activeRenderer;
        private SpriteRenderer nextRenderer;
        private float transitionTimer;
        private bool transitioning;

        public void Configure(Camera camera)
        {
            boardCamera = camera;
            EnsureRenderers();
            FitRenderer(activeRenderer);
            FitRenderer(nextRenderer);
        }

        public void ShowBackground(string backgroundPath)
        {
            EnsureRenderers();
            var sprite = LoadSprite(backgroundPath);
            if (sprite == null)
            {
                activeRenderer.sprite = null;
                activeRenderer.color = fallbackColor;
                nextRenderer.color = new Color(fallbackColor.r, fallbackColor.g, fallbackColor.b, 0f);
                transitioning = false;
                return;
            }

            if (activeRenderer.sprite == sprite && !transitioning)
            {
                return;
            }

            nextRenderer.sprite = sprite;
            nextRenderer.color = new Color(1f, 1f, 1f, 0f);
            FitRenderer(nextRenderer);
            transitionTimer = 0f;
            transitioning = true;
        }

        private void LateUpdate()
        {
            if (boardCamera != null)
            {
                FitRenderer(activeRenderer);
                FitRenderer(nextRenderer);
            }

            if (!transitioning)
            {
                return;
            }

            transitionTimer += Time.deltaTime;
            var progress = transitionDuration > 0f ? Mathf.Clamp01(transitionTimer / transitionDuration) : 1f;
            activeRenderer.color = new Color(activeRenderer.color.r, activeRenderer.color.g, activeRenderer.color.b, 1f - progress);
            nextRenderer.color = new Color(1f, 1f, 1f, progress);

            if (progress < 1f)
            {
                return;
            }

            (activeRenderer, nextRenderer) = (nextRenderer, activeRenderer);
            nextRenderer.sprite = null;
            nextRenderer.color = Color.clear;
            transitioning = false;
        }

        private void EnsureRenderers()
        {
            if (activeRenderer == null)
            {
                activeRenderer = CreateRenderer("Active Background", -102);
            }

            if (nextRenderer == null)
            {
                nextRenderer = CreateRenderer("Next Background", -101);
            }
        }

        private SpriteRenderer CreateRenderer(string rendererName, int sortingOrder)
        {
            var rendererObject = new GameObject(rendererName);
            rendererObject.transform.SetParent(transform, false);
            rendererObject.transform.localPosition = Vector3.forward * 8f;
            var renderer = rendererObject.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = sortingOrder;
            renderer.color = Color.clear;
            return renderer;
        }

        private Sprite LoadSprite(string backgroundPath)
        {
            if (string.IsNullOrWhiteSpace(backgroundPath))
            {
                return null;
            }

            if (spriteCache.TryGetValue(backgroundPath, out var cached))
            {
                return cached;
            }

            var sprite = Resources.Load<Sprite>(backgroundPath);
            if (sprite == null)
            {
                var texture = Resources.Load<Texture2D>(backgroundPath);
                if (texture != null)
                {
                    sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
                }
            }

            spriteCache[backgroundPath] = sprite;
            return sprite;
        }

        private void FitRenderer(SpriteRenderer renderer)
        {
            if (renderer == null || boardCamera == null || renderer.sprite == null)
            {
                return;
            }

            var height = boardCamera.orthographicSize * 2f;
            var width = height * boardCamera.aspect;
            renderer.transform.position = new Vector3(boardCamera.transform.position.x, boardCamera.transform.position.y, 8f);
            var bounds = renderer.sprite.bounds;
            var scale = Mathf.Max(width / bounds.size.x, height / bounds.size.y);
            renderer.transform.localScale = new Vector3(scale, scale, 1f);
        }
    }
}
