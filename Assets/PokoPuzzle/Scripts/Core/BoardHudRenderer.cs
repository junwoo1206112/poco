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
            var verticalSize = boardHalfHeight + 2.25f;
            var horizontalSize = boardHalfWidth / Mathf.Max(0.5f, boardCamera.aspect) + 1.5f;
            boardCamera.orthographicSize = Mathf.Max(boardCamera.orthographicSize, 6.6f, verticalSize, horizontalSize);

            var cameraPosition = boardCamera.transform.position;
            boardCamera.transform.position = new Vector3(cameraPosition.x, 0.72f, cameraPosition.z);
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
                scoreText.text = $"Score {score}\nEnemy {enemySpawnIndex + 1}  {timeRemaining}s";
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
            var scoreWidth = Mathf.Min(Screen.width - margin * 2f, 400f * scale);
            var scorePanel = new Rect((Screen.width - scoreWidth) * 0.5f, margin, scoreWidth, 82f * scale);
            var comboText = feverActive ? $"FEVER! ({feverTimerInt}s)" : comboCount > 0 ? $"Combo x{comboCount}" : "";

            DrawHudPanel(scorePanel);
            GUI.Label(
                new Rect(scorePanel.x + 12f * scale, scorePanel.y + 5f * scale, scorePanel.width - 24f * scale, 26f * scale),
                $"Score {score}" + (string.IsNullOrEmpty(comboText) ? "" : $"  |  {comboText}"),
                CreateHudStyle(feverActive ? 24f : 20f, FontStyle.Bold, feverActive ? new Color(1f, 0.4f, 0.1f) : Color.white, scale, TextAnchor.UpperCenter));
            GUI.Label(
                new Rect(scorePanel.x + 12f * scale, scorePanel.y + 32f * scale, scorePanel.width - 24f * scale, 22f * scale),
                $"Target {targetScore}  |  Enemy {enemySpawnIndex}  |  Time {timeRemaining}s",
                CreateHudStyle(15f, FontStyle.Normal, new Color(0.86f, 0.93f, 1f), scale, TextAnchor.UpperCenter));

            if (enemy != null)
            {
                var isBoss = enemy.Wave > 0;
                var badgeWidth = 68f * scale;
                var badgeGap = 6f * scale;
                var maxEnemyWidth = Screen.width - margin * 2f;
                var eBarWidth = isBoss
                    ? Mathf.Min(Mathf.Max(150f * scale, maxEnemyWidth - badgeWidth - badgeGap), 260f * scale)
                    : Mathf.Min(maxEnemyWidth, 260f * scale);
                var eBarHeight = 18f * scale;
                var groupWidth = isBoss ? badgeWidth + badgeGap + eBarWidth : eBarWidth;
                var groupX = (Screen.width - groupWidth) * 0.5f;
                var eBarX = isBoss ? groupX + badgeWidth + badgeGap : groupX;
                var eBarY = scorePanel.yMax + 6f * scale;

                if (isBoss)
                {
                    DrawBossBadge(new Rect(groupX, eBarY - 8f * scale, badgeWidth, eBarHeight + 16f * scale), enemy.Wave, scale);
                }

                DrawHudPanel(new Rect(eBarX - 4f * scale, eBarY - 2f * scale, eBarWidth + 8f * scale, eBarHeight + 4f * scale));
                var eBarColor = enemy.IsDefeated ? new Color(0.5f, 0.5f, 0.5f, 0.5f) : GUI.color;
                GUI.color = eBarColor;
                GUI.Box(new Rect(eBarX, eBarY, eBarWidth * enemy.HpRatio, eBarHeight), GUIContent.none);
                GUI.color = Color.white;
                GUI.Label(
                    new Rect(eBarX, eBarY, eBarWidth, eBarHeight),
                    enemy.IsDefeated ? $"Defeated!" : $"{enemy.Name}  {enemy.CurrentHp}/{enemy.MaxHp}",
                    CreateHudStyle(13f, FontStyle.Bold, Color.white, scale, TextAnchor.MiddleCenter));
            }

            if (rainbowGaugeMax > 0f)
            {
                var gaugeRatio = Mathf.Clamp01(rainbowGauge / rainbowGaugeMax);
                var gBarWidth = Mathf.Min(Screen.width - margin * 2f, 160f * scale);
                var gBarHeight = 10f * scale;
                var gBarX = (Screen.width - gBarWidth) * 0.5f;
                var gBarY = scorePanel.yMax + (enemy != null ? 28f * scale : 6f * scale);

                DrawHudPanel(new Rect(gBarX - 4f * scale, gBarY - 2f * scale, gBarWidth + 8f * scale, gBarHeight + 4f * scale));
                if (gaugeRatio >= 1f)
                {
                    GUI.color = new Color(0.8f, 0.4f, 1f);
                }
                else
                {
                    GUI.color = Color.Lerp(new Color(0.3f, 0.3f, 0.5f), new Color(0.8f, 0.4f, 1f), gaugeRatio);
                }
                GUI.Box(new Rect(gBarX, gBarY, gBarWidth * gaugeRatio, gBarHeight), GUIContent.none);
                GUI.color = Color.white;
                var gaugeLabel = gaugeRatio >= 1f ? "Rainbow Ready!" : $"Rainbow {Mathf.RoundToInt(gaugeRatio * 100f)}%";
                GUI.Label(
                    new Rect(gBarX, gBarY, gBarWidth, gBarHeight),
                    gaugeLabel,
                    CreateHudStyle(9f, FontStyle.Bold, gaugeRatio >= 1f ? new Color(0.8f, 0.4f, 1f) : new Color(0.7f, 0.7f, 0.8f), scale, TextAnchor.MiddleCenter));
            }

            if (!string.IsNullOrWhiteSpace(agentHudText))
            {
                var agentWidth = Mathf.Min(Screen.width - margin * 2f, 390f * scale);
                var agentPanel = new Rect(
                    (Screen.width - agentWidth) * 0.5f,
                    Screen.height - 80f * scale,
                    agentWidth,
                    68f * scale);

                DrawHudPanel(agentPanel);
                GUI.Label(
                    new Rect(agentPanel.x + 12f * scale, agentPanel.y + 8f * scale, agentPanel.width - 24f * scale, agentPanel.height - 12f * scale),
                    agentHudText,
                    CreateHudStyle(13f, FontStyle.Normal, new Color(0.65f, 0.92f, 1f), scale, TextAnchor.UpperLeft));
            }

            if (!string.IsNullOrWhiteSpace(feedbackMessage) && feedbackClearTime > Time.time)
            {
                var feedbackWidth = Mathf.Min(Screen.width - margin * 2f, 340f * scale);
                var feedbackPanel = new Rect(
                    (Screen.width - feedbackWidth) * 0.5f,
                    scorePanel.yMax + (enemy != null ? 30f * scale : 6f * scale),
                    feedbackWidth,
                    32f * scale);

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
                score >= targetScore ? "TARGET CLEAR" : "TIME UP",
                CreateHudStyle(28f, FontStyle.Bold, score >= targetScore ? new Color(0.40f, 1f, 0.55f) : new Color(1f, 0.42f, 0.42f), scale, TextAnchor.UpperCenter));
            GUI.Label(
                new Rect(panel.x + 18f * scale, panel.y + 66f * scale, panel.width - 36f * scale, 34f * scale),
                "60s score attack complete.",
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
