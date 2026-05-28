using UnityEngine;

namespace PokoPuzzle.Core
{
    public sealed class BoardHudRenderer
    {
        private readonly Camera boardCamera;
        private readonly TextMesh scoreText;
        private readonly TextMesh agentText;
        private readonly TextMesh feedbackText;
        private readonly bool useScreenHud;
        private readonly int width;
        private readonly int height;
        private readonly float spacing;
        private readonly bool useHexGrid;

        private string feedbackMessage = string.Empty;
        private Color feedbackColor = Color.white;
        private float feedbackClearTime;

        public BoardHudRenderer(Camera boardCamera, TextMesh scoreText, TextMesh agentText,
            TextMesh feedbackText, bool useScreenHud, int width, int height, float spacing, bool useHexGrid)
        {
            this.boardCamera = boardCamera;
            this.scoreText = scoreText;
            this.agentText = agentText;
            this.feedbackText = feedbackText;
            this.useScreenHud = useScreenHud;
            this.width = width;
            this.height = height;
            this.spacing = spacing;
            this.useHexGrid = useHexGrid;
        }

        private void SetLegacyHudVisible(bool visible)
        {
            SetTextMeshVisible(scoreText, visible);
            SetTextMeshVisible(agentText, visible);
            SetTextMeshVisible(feedbackText, visible);
        }

        private static void SetTextMeshVisible(TextMesh textMesh, bool visible)
        {
            if (textMesh != null && textMesh.TryGetComponent<MeshRenderer>(out var renderer))
            {
                renderer.enabled = visible;
            }
        }

        public void FramePlayCamera()
        {
            if (boardCamera == null || !boardCamera.orthographic)
            {
                return;
            }

            var boardHalfHeight = Mathf.Max(1f, (height - 1) * spacing * (useHexGrid ? HexGridUtility.VerticalSpacingRatio : 1f) * 0.5f);
            var boardHalfWidth = Mathf.Max(1f, width * spacing * 0.5f);
            var verticalSize = boardHalfHeight + 3.5f;
            var horizontalSize = boardHalfWidth / Mathf.Max(0.5f, boardCamera.aspect) + 2f;
            boardCamera.orthographicSize = Mathf.Max(boardCamera.orthographicSize, 6.6f, verticalSize, horizontalSize);

            var cameraPosition = boardCamera.transform.position;
            boardCamera.transform.position = new Vector3(cameraPosition.x, 0.95f, cameraPosition.z);
        }

        public void PrepareHud()
        {
            SetLegacyHudVisible(!useScreenHud);

            var boardHalfHeight = Mathf.Max(1f, (height - 1) * spacing * (useHexGrid ? HexGridUtility.VerticalSpacingRatio : 1f) * 0.5f);
            var boardHalfWidth = Mathf.Max(1f, width * spacing * 0.5f);
            var left = -boardHalfWidth + 0.1f;
            var top = boardHalfHeight + 0.85f;
            var bottom = -boardHalfHeight - 0.55f;

            if (scoreText != null)
            {
                scoreText.transform.position = new Vector3(left, top, 0f);
            }

            if (agentText != null)
            {
                agentText.transform.position = new Vector3(left, bottom, 0f);
            }

            if (feedbackText != null)
            {
                feedbackText.transform.position = new Vector3(0f, top + 0.15f, 0f);
            }
        }

        public void ShowFeedback(string message, Color color, float duration)
        {
            feedbackMessage = message;
            feedbackColor = color;
            feedbackClearTime = Time.time + duration;

            if (feedbackText != null)
            {
                feedbackText.text = message;
                feedbackText.color = color;
            }
        }

        public void RefreshTimedFeedback()
        {
            if (feedbackClearTime <= 0f || Time.time < feedbackClearTime)
            {
                return;
            }

            feedbackMessage = string.Empty;
            if (feedbackText != null)
            {
                feedbackText.text = string.Empty;
            }

            feedbackClearTime = 0f;
        }

        public void RefreshHud(int score, int enemySpawnIndex, int timeRemaining)
        {
            if (scoreText != null)
            {
                scoreText.text = $"Score {score}\nTime {timeRemaining}";
            }
        }

        public void SetAgentHudText(string text)
        {
            if (agentText != null)
            {
                agentText.text = text;
            }
        }

        public System.Action OnRestartRequested;

        public void OnGUI(int score, int timeRemaining, int comboCount, bool feverActive,
            int feverTimerInt, string agentHudText, bool gameEnded, int targetScore,
            BoardEnemy enemy, int enemySpawnIndex, float rainbowGauge = 0f, float rainbowGaugeMax = 100f)
        {
            if (!useScreenHud)
            {
                return;
            }

            var scale = Mathf.Clamp(Screen.height / 720f, 0.82f, 1.12f);
            var margin = Mathf.RoundToInt(16f * scale);
            var panelWidth = Mathf.Min(Screen.width - margin * 2f, 360f * scale);
            var panelCenterX = (Screen.width - panelWidth) * 0.5f;

            var topPanel = new Rect(panelCenterX, margin, panelWidth, 72f * scale);
            DrawHudPanel(topPanel);

            var comboText = feverActive ? $"FEVER! ({feverTimerInt}s)" : comboCount > 0 ? $"Combo x{comboCount}" : "";
            var scoreLabel = score >= targetScore ? $"Score {score}" : $"Score {score} / {targetScore}";
            GUI.Label(
                new Rect(topPanel.x + 10f * scale, topPanel.y + 4f * scale, topPanel.width - 20f * scale, 22f * scale),
                string.IsNullOrEmpty(comboText) ? scoreLabel : $"{scoreLabel}  |  {comboText}",
                CreateHudStyle(feverActive ? 22f : 18f, FontStyle.Bold, feverActive ? new Color(1f, 0.4f, 0.1f) : Color.white, scale, TextAnchor.UpperCenter));

            var timerColor = timeRemaining <= 10 ? new Color(1f, 0.3f, 0.3f) : new Color(0.86f, 0.93f, 1f);
            GUI.Label(
                new Rect(topPanel.x + 10f * scale, topPanel.y + 28f * scale, topPanel.width - 20f * scale, 20f * scale),
                $"TIME  {timeRemaining}",
                CreateHudStyle(22f, FontStyle.Bold, timerColor, scale, TextAnchor.UpperCenter));

            var enemyInfoY = topPanel.yMax + 4f * scale;

            if (enemy != null)
            {
                var isBoss = enemy.Wave > 0;
                var eBarWidth = Mathf.Min(Screen.width - margin * 2f, 280f * scale);
                var eBarHeight = 18f * scale;
                var eBarX = (Screen.width - eBarWidth) * 0.5f;
                var eBarY = enemyInfoY;

                DrawHudPanel(new Rect(eBarX - 4f * scale, eBarY - 2f * scale, eBarWidth + 8f * scale, eBarHeight + 4f * scale));

                var fillColor = isBoss ? new Color(0.85f, 0.15f, 0.15f) : new Color(0.2f, 0.6f, 1f);
                if (enemy.IsDefeated)
                {
                    GUI.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
                }
                else
                {
                    GUI.color = fillColor;
                }
                GUI.Box(new Rect(eBarX, eBarY, eBarWidth * enemy.HpRatio, eBarHeight), GUIContent.none);
                GUI.color = Color.white;
                GUI.Label(
                    new Rect(eBarX, eBarY, eBarWidth, eBarHeight),
                    enemy.IsDefeated ? "DEFEATED!" : $"{enemy.Name}  {enemy.CurrentHp}/{enemy.MaxHp}",
                    CreateHudStyle(13f, FontStyle.Bold, Color.white, scale, TextAnchor.MiddleCenter));

                if (isBoss)
                {
                    GUI.Label(
                        new Rect(eBarX, eBarY - 16f * scale, eBarWidth, 14f * scale),
                        $"BOSS WAVE {enemy.Wave}",
                        CreateHudStyle(11f, FontStyle.Bold, new Color(1f, 0.9f, 0.55f), scale, TextAnchor.UpperCenter));
                }

                enemyInfoY = eBarY + eBarHeight + 4f * scale;
            }

            if (rainbowGaugeMax > 0f)
            {
                var gaugeRatio = Mathf.Clamp01(rainbowGauge / rainbowGaugeMax);
                var gBarWidth = Mathf.Min(Screen.width - margin * 2f, 180f * scale);
                var gBarHeight = 10f * scale;
                var gBarX = (Screen.width - gBarWidth) * 0.5f;
                var gBarY = enemyInfoY;

                DrawHudPanel(new Rect(gBarX - 4f * scale, gBarY - 2f * scale, gBarWidth + 8f * scale, gBarHeight + 4f * scale));
                var gaugeColor = gaugeRatio >= 1f ? new Color(0.8f, 0.4f, 1f) : Color.Lerp(new Color(0.3f, 0.3f, 0.5f), new Color(0.8f, 0.4f, 1f), gaugeRatio);
                GUI.color = gaugeColor;
                GUI.Box(new Rect(gBarX, gBarY, gBarWidth * gaugeRatio, gBarHeight), GUIContent.none);
                GUI.color = Color.white;
                var gaugeLabel = gaugeRatio >= 1f ? "RAINBOMB READY" : $"RAINBOW {Mathf.RoundToInt(gaugeRatio * 100f)}%";
                GUI.Label(
                    new Rect(gBarX, gBarY, gBarWidth, gBarHeight),
                    gaugeLabel,
                    CreateHudStyle(9f, FontStyle.Bold, gaugeRatio >= 1f ? new Color(0.8f, 0.4f, 1f) : new Color(0.7f, 0.7f, 0.8f), scale, TextAnchor.MiddleCenter));
            }

            if (!string.IsNullOrWhiteSpace(feedbackMessage) && feedbackClearTime > Time.time)
            {
                var feedbackWidth = Mathf.Min(Screen.width - margin * 2f, 340f * scale);
                var feedbackY = enemyInfoY + (rainbowGaugeMax > 0f ? 22f * scale : 4f * scale);
                var feedbackPanel = new Rect(
                    (Screen.width - feedbackWidth) * 0.5f, feedbackY,
                    feedbackWidth, 32f * scale);

                DrawHudPanel(feedbackPanel);
                GUI.Label(
                    new Rect(feedbackPanel.x + 8f * scale, feedbackPanel.y + 4f * scale, feedbackPanel.width - 16f * scale, feedbackPanel.height - 8f * scale),
                    feedbackMessage,
                    CreateHudStyle(15f, FontStyle.Bold, feedbackColor, scale, TextAnchor.MiddleCenter));
            }

            if (gameEnded)
            {
                DrawEndPanel(scale, score, targetScore);
            }
        }

        private static GUIStyle CreateHudStyle(float fontSize, FontStyle fontStyle, Color color, float scale, TextAnchor alignment = TextAnchor.UpperLeft)
        {
            return new GUIStyle(GUI.skin.label)
            {
                alignment = alignment,
                fontSize = Mathf.Max(11, Mathf.RoundToInt(fontSize * scale)),
                fontStyle = fontStyle,
                normal = { textColor = color },
                wordWrap = true,
                richText = false
            };
        }

        private static void DrawHudPanel(Rect rect)
        {
            var previousColor = GUI.color;
            GUI.color = new Color(0.03f, 0.05f, 0.09f, 0.86f);
            GUI.Box(rect, GUIContent.none);
            GUI.color = previousColor;
        }

        private static void DrawBossBadge(Rect rect, int wave, float scale)
        {
            var previousColor = GUI.color;
            GUI.color = new Color(0.55f, 0.08f, 0.08f, 0.96f);
            GUI.Box(rect, GUIContent.none);
            GUI.color = Color.white;
            GUI.Label(
                new Rect(rect.x + 4f * scale, rect.y + 2f * scale, rect.width - 8f * scale, 16f * scale),
                "BOSS",
                CreateHudStyle(13f, FontStyle.Bold, new Color(1f, 0.9f, 0.55f), scale, TextAnchor.MiddleCenter));
            GUI.Label(
                new Rect(rect.x + 4f * scale, rect.y + 17f * scale, rect.width - 8f * scale, 14f * scale),
                $"W{wave}",
                CreateHudStyle(10f, FontStyle.Bold, Color.white, scale, TextAnchor.MiddleCenter));
            GUI.color = previousColor;
        }

        private void DrawEndPanel(float scale, int score, int targetScore)
        {
            var panelWidth = Mathf.Min(Screen.width - 28f * scale, 360f * scale);
            var panelHeight = 176f * scale;
            var panel = new Rect(
                (Screen.width - panelWidth) * 0.5f,
                (Screen.height - panelHeight) * 0.5f,
                panelWidth,
                panelHeight);

            DrawHudPanel(panel);
            GUI.Label(
                new Rect(panel.x + 18f * scale, panel.y + 18f * scale, panel.width - 36f * scale, 40f * scale),
                score >= targetScore ? "GAME CLEAR" : "TIME UP",
                CreateHudStyle(28f, FontStyle.Bold, score >= targetScore ? new Color(0.40f, 1f, 0.55f) : new Color(1f, 0.42f, 0.42f), scale, TextAnchor.UpperCenter));
            GUI.Label(
                new Rect(panel.x + 18f * scale, panel.y + 66f * scale, panel.width - 36f * scale, 34f * scale),
                $"Score {score} / Target {targetScore}",
                CreateHudStyle(15f, FontStyle.Normal, Color.white, scale, TextAnchor.UpperCenter));

            if (GUI.Button(
                    new Rect(panel.x + 72f * scale, panel.y + 116f * scale, panel.width - 144f * scale, 38f * scale),
                    "Restart Round",
                    CreateButtonStyle(scale)))
            {
                OnRestartRequested?.Invoke();
            }
        }

        private static GUIStyle CreateButtonStyle(float scale)
        {
            return new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.Max(12, Mathf.RoundToInt(16f * scale)),
                fontStyle = FontStyle.Bold,
                richText = false
            };
        }
    }
}
