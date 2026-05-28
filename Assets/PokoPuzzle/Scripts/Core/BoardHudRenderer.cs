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
        private float damagePulseEndTime;
        private float bossPulseEndTime;
        private float skillPulseEndTime;
        private float bossHpIntroStartTime;
        private float bossHpIntroEndTime;
        private Color skillPulseColor = Color.white;
        private BoardEnemy lastEnemy;

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

        public void PlayDamagePulse(bool boss)
        {
            damagePulseEndTime = Time.time + (boss ? 0.34f : 0.24f);
        }

        public void PlayBossPulse()
        {
            bossPulseEndTime = Time.time + 0.55f;
        }

        public void PlaySkillPulse(Color color)
        {
            skillPulseColor = color;
            skillPulseEndTime = Time.time + 0.38f;
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
            BoardEnemy enemy, int enemySpawnIndex, float feverGauge = 0f, float feverGaugeMax = 100f)
        {
            if (!useScreenHud)
            {
                return;
            }

            var scale = Mathf.Clamp(Screen.height / 720f, 0.82f, 1.12f);
            var margin = Mathf.RoundToInt(16f * scale);
            DrawSkillPulseOverlay();
            var comboText = feverActive ? $"FEVER! ({feverTimerInt}s)" : comboCount > 0 ? $"Combo x{comboCount}" : "";
            var scoreLabel = score >= targetScore ? score.ToString() : $"{score}/{targetScore}";
            var scoreWidth = Mathf.Min(170f * scale, Screen.width * 0.34f);
            var timeWidth = Mathf.Min(92f * scale, Screen.width * 0.22f);
            var gaugeWidth = Mathf.Min(170f * scale, Screen.width * 0.34f);
            var topY = margin;
            var scorePanel = new Rect(margin, topY, scoreWidth, 54f * scale);
            var timePanel = new Rect((Screen.width - timeWidth) * 0.5f, topY, timeWidth, 54f * scale);
            var gaugePanel = new Rect(Screen.width - margin - gaugeWidth, topY, gaugeWidth, 54f * scale);

            DrawHudPanel(scorePanel);
            GUI.Label(
                new Rect(scorePanel.x, scorePanel.y + 4f * scale, scorePanel.width, 18f * scale),
                "SCORE",
                CreateHudStyle(10f, FontStyle.Bold, new Color(0.74f, 0.88f, 1f), scale, TextAnchor.UpperCenter));
            GUI.Label(
                new Rect(scorePanel.x, scorePanel.y + 22f * scale, scorePanel.width, 26f * scale),
                scoreLabel,
                CreateHudStyle(15f, FontStyle.Bold, Color.white, scale, TextAnchor.UpperCenter));

            var timerColor = timeRemaining <= 10 ? new Color(1f, 0.3f, 0.3f) : new Color(0.86f, 0.93f, 1f);
            DrawHudPanel(timePanel);
            GUI.Label(
                new Rect(timePanel.x + 6f * scale, timePanel.y + 4f * scale, timePanel.width - 12f * scale, 15f * scale),
                "TIME",
                CreateHudStyle(10f, FontStyle.Bold, new Color(0.74f, 0.88f, 1f), scale, TextAnchor.UpperCenter));
            GUI.Label(
                new Rect(timePanel.x + 6f * scale, timePanel.y + 17f * scale, timePanel.width - 12f * scale, 32f * scale),
                timeRemaining.ToString(),
                CreateHudStyle(25f, FontStyle.Bold, timerColor, scale, TextAnchor.UpperCenter));

            if (feverActive)
            {
                var glow = 0.18f + Mathf.Sin(Time.time * 8f) * 0.06f;
                DrawHudPanel(ExpandRect(gaugePanel, 4f * scale), new Color(1f, 0.55f, 0.06f, glow));
            }

            DrawHudPanel(gaugePanel);
            var gaugeRatio = feverGaugeMax > 0f ? Mathf.Clamp01(feverGauge / feverGaugeMax) : 0f;
            GUI.Label(
                new Rect(gaugePanel.x, gaugePanel.y + 4f * scale, gaugePanel.width, 16f * scale),
                "FEVER",
                CreateHudStyle(10f, FontStyle.Bold, new Color(1f, 0.78f, 0.38f), scale, TextAnchor.UpperCenter));
            var gaugeBarRect = new Rect(gaugePanel.x + 8f * scale, gaugePanel.y + 22f * scale, gaugePanel.width - 16f * scale, 18f * scale);
            var feverFill = feverActive
                ? Color.Lerp(new Color(1f, 0.45f, 0.12f), new Color(1f, 0.95f, 0.22f), 0.5f + Mathf.Sin(Time.time * 10f) * 0.5f)
                : new Color(1f, 0.45f, 0.12f);
            DrawGaugeBar(gaugeBarRect, feverActive ? 1f : gaugeRatio, feverFill);
            GUI.Label(
                gaugeBarRect,
                feverActive ? $"{feverTimerInt}s" : $"{Mathf.RoundToInt(gaugeRatio * 100f)}%",
                CreateHudStyle(11f, FontStyle.Bold, Color.white, scale, TextAnchor.MiddleCenter));

            if (!string.IsNullOrEmpty(comboText))
            {
                var comboWidth = Mathf.Min(Screen.width - margin * 2f, 170f * scale);
                var comboPanel = new Rect((Screen.width - comboWidth) * 0.5f, topY + 58f * scale, comboWidth, 24f * scale);
                DrawHudPanel(comboPanel);
                GUI.Label(
                    new Rect(comboPanel.x + 8f * scale, comboPanel.y + 3f * scale, comboPanel.width - 16f * scale, comboPanel.height - 6f * scale),
                    comboText,
                    CreateHudStyle(13f, FontStyle.Bold, feverActive ? new Color(1f, 0.4f, 0.1f) : new Color(1f, 0.9f, 0.45f), scale, TextAnchor.MiddleCenter));
            }

            var enemyInfoY = topY + 86f * scale;

            if (enemy != null)
            {
                var isBoss = enemy.Wave > 0;
                if (!ReferenceEquals(lastEnemy, enemy))
                {
                    lastEnemy = enemy;
                    if (isBoss)
                    {
                        bossHpIntroStartTime = Time.time;
                        bossHpIntroEndTime = Time.time + 0.35f;
                    }
                    else
                    {
                        bossHpIntroStartTime = 0f;
                        bossHpIntroEndTime = 0f;
                    }
                }

                var combatWidth = Mathf.Min(Screen.width - margin * 2f, 380f * scale);
                var badgeWidth = isBoss ? 64f * scale : 48f * scale;
                var badgeGap = 6f * scale;
                var eBarWidth = combatWidth - badgeWidth - badgeGap;
                var eBarHeight = 22f * scale;
                var combatX = (Screen.width - combatWidth) * 0.5f;
                var eBarX = combatX + badgeWidth + badgeGap;
                var eBarY = enemyInfoY;
                var bossPulse = Time.time < bossPulseEndTime;
                var damagePulse = Time.time < damagePulseEndTime;

                if (isBoss)
                {
                    var badgeRect = new Rect(combatX, eBarY - 7f * scale, badgeWidth, eBarHeight + 14f * scale);
                    if (bossPulse)
                    {
                        DrawHudPanel(ExpandRect(badgeRect, 4f * scale), new Color(1f, 0.65f, 0.12f, 0.78f));
                    }

                    DrawBossBadge(badgeRect, enemy.Wave, scale);
                }
                else
                {
                    DrawEnemyBadge(new Rect(combatX, eBarY - 5f * scale, badgeWidth, eBarHeight + 10f * scale), scale);
                }

                var hpPanelRect = new Rect(eBarX - 4f * scale, eBarY - 2f * scale, eBarWidth + 8f * scale, eBarHeight + 4f * scale);
                DrawHudPanel(hpPanelRect, damagePulse ? new Color(1f, 0.36f, 0.12f, 0.72f) : new Color(0.03f, 0.05f, 0.09f, 0.86f));

                var fillColor = isBoss ? new Color(0.85f, 0.15f, 0.15f) : new Color(0.2f, 0.6f, 1f);
                if (damagePulse)
                {
                    fillColor = Color.Lerp(fillColor, Color.white, 0.35f);
                }

                var hpRatio = enemy.IsDefeated ? 0f : enemy.HpRatio;
                if (isBoss && Time.time < bossHpIntroEndTime)
                {
                    var introRatio = Mathf.InverseLerp(bossHpIntroStartTime, bossHpIntroEndTime, Time.time);
                    hpRatio *= introRatio;
                }

                DrawGaugeBar(new Rect(eBarX, eBarY, eBarWidth, eBarHeight), hpRatio, fillColor);
                GUI.Label(
                    new Rect(eBarX, eBarY, eBarWidth, eBarHeight),
                    enemy.IsDefeated ? "DEFEATED!" : $"{enemy.Name}  {enemy.CurrentHp}/{enemy.MaxHp}",
                    CreateHudStyle(12f, FontStyle.Bold, Color.white, scale, TextAnchor.MiddleCenter));

                enemyInfoY = eBarY + eBarHeight + 8f * scale;
            }

            if (!string.IsNullOrWhiteSpace(feedbackMessage) && feedbackClearTime > Time.time)
            {
                var feedbackWidth = Mathf.Min(Screen.width - margin * 2f, 340f * scale);
                var feedbackY = enemyInfoY + (feverGaugeMax > 0f ? 22f * scale : 4f * scale);
                var feedbackPanel = new Rect(
                    (Screen.width - feedbackWidth) * 0.5f, feedbackY,
                    feedbackWidth, 32f * scale);

                DrawHudPanel(feedbackPanel);
                GUI.Label(
                    new Rect(feedbackPanel.x + 8f * scale, feedbackPanel.y + 4f * scale, feedbackPanel.width - 16f * scale, feedbackPanel.height - 8f * scale),
                    feedbackMessage,
                    CreateHudStyle(15f, FontStyle.Bold, feedbackColor, scale, TextAnchor.MiddleCenter));
            }

            if (!string.IsNullOrWhiteSpace(agentHudText) && !gameEnded)
            {
                var agentWidth = Mathf.Min(Screen.width - margin * 2f, 360f * scale);
                var agentPanel = new Rect((Screen.width - agentWidth) * 0.5f, Screen.height - 52f * scale, agentWidth, 38f * scale);
                DrawHudPanel(agentPanel);
                GUI.Label(
                    new Rect(agentPanel.x + 10f * scale, agentPanel.y + 5f * scale, agentPanel.width - 20f * scale, agentPanel.height - 10f * scale),
                    agentHudText,
                    CreateHudStyle(10f, FontStyle.Normal, new Color(0.65f, 0.92f, 1f), scale, TextAnchor.UpperLeft));
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
            DrawHudPanel(rect, new Color(0.03f, 0.05f, 0.09f, 0.86f));
        }

        private static void DrawHudPanel(Rect rect, Color color)
        {
            var previousColor = GUI.color;
            GUI.color = color;
            GUI.Box(rect, GUIContent.none);
            GUI.color = previousColor;
        }

        private void DrawSkillPulseOverlay()
        {
            if (Time.time >= skillPulseEndTime)
            {
                return;
            }

            var remaining = Mathf.Clamp01((skillPulseEndTime - Time.time) / 0.38f);
            var previousColor = GUI.color;
            GUI.color = new Color(skillPulseColor.r, skillPulseColor.g, skillPulseColor.b, 0.12f * remaining);
            GUI.Box(new Rect(0f, 0f, Screen.width, Screen.height), GUIContent.none);
            GUI.color = previousColor;
        }

        private static Rect ExpandRect(Rect rect, float amount)
        {
            return new Rect(rect.x - amount, rect.y - amount, rect.width + amount * 2f, rect.height + amount * 2f);
        }

        private static void DrawGaugeBar(Rect rect, float ratio, Color fillColor)
        {
            var previousColor = GUI.color;
            GUI.color = new Color(0.08f, 0.10f, 0.16f, 0.95f);
            GUI.Box(rect, GUIContent.none);
            GUI.color = fillColor;
            GUI.Box(new Rect(rect.x, rect.y, rect.width * Mathf.Clamp01(ratio), rect.height), GUIContent.none);
            GUI.color = previousColor;
        }

        private static void DrawEnemyBadge(Rect rect, float scale)
        {
            var previousColor = GUI.color;
            GUI.color = new Color(0.08f, 0.22f, 0.34f, 0.96f);
            GUI.Box(rect, GUIContent.none);
            GUI.color = Color.white;
            GUI.Label(
                new Rect(rect.x + 4f * scale, rect.y + 4f * scale, rect.width - 8f * scale, rect.height - 8f * scale),
                "FOE",
                CreateHudStyle(12f, FontStyle.Bold, new Color(0.78f, 0.92f, 1f), scale, TextAnchor.MiddleCenter));
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
