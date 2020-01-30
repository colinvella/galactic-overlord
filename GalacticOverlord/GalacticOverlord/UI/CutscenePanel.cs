using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using GalacticOverlord.Pipeline;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Input.Touch;
using GalacticOverlord.Core;

namespace GalacticOverlord.UI
{
    public class CutscenePanel: ModularGameComponent
    {
        public CutscenePanel(TouchInterface touchInterface, CutsceneLineDefinition[] cutscene)
            :base(touchInterface.GalacticOverlordGame)
        {
            m_touchInterface = touchInterface;
            m_cutscene = cutscene;
            m_cutsceneIndex = 0;
            m_cutsceneTop = true;
            m_panTop = 1.0f;
            m_panBottom = 1.0f;
            m_tapCountdown = 1.0f;
        }

        public override void Initialize()
        {
            m_spriteBatch = new SpriteBatch(Game.GraphicsDevice);
            TouchPanel.EnabledGestures |= GestureType.Hold;

            UpdateCutscene();

            base.Initialize();
        }

        public override void Update(GameTime gameTime)
        {
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            foreach (GestureSample gestureSample in m_touchInterface.GestureSamples)
            {
                switch (gestureSample.GestureType)
                {
                    case GestureType.Tap:
                        if (!NarrativeComplete)
                        {
                            ++m_cutsceneIndex;
                            m_cutsceneTop = !m_cutsceneTop;
                            m_tapCountdown = 1.0f;
                            UpdateCutscene();
                        }
                        break;
                    case GestureType.Hold:
                        m_cutsceneIndex = m_cutscene.Length;
                        break;
                }
            }

            m_tapCountdown = Math.Max(0.0f, m_tapCountdown - deltaTime);

            float panDelta = deltaTime * 4.0f;
            if (m_cutsceneTop)
            {
                m_panTop = Math.Max(0.0f, m_panTop - panDelta);
                m_panBottom = Math.Min(1.0f, m_panBottom + panDelta);
            }
            else
            {
                m_panTop = Math.Min(1.0f, m_panTop + panDelta);
                m_panBottom = Math.Max(0.0f, m_panBottom - panDelta);
            }

            base.Update(gameTime);
        }

        public override void Draw(GameTime gameTime)
        {
            if (m_cutsceneIndex >= m_cutscene.Length)
                return;

            m_spriteBatch.Begin();

            // draw overlay
            m_spriteBatch.Draw(m_screenOverlayTexture, Vector2.Zero, Color.White);

            // draw top character image with panning
            if (m_currentCharacterTextureTop != null)
                m_spriteBatch.Draw(m_currentCharacterTextureTop, new Vector2(m_panTop * -Space.PlayAreaSize.X ,40.0f), Color.White);

            // draw bottom character image with panning
            if (m_currentCharacterTextureBottom != null)
                m_spriteBatch.Draw(m_currentCharacterTextureBottom, new Vector2(m_panBottom * Space.PlayAreaSize.X, 400.0f), Color.White);

            // draw top cutscene lines image with panning
            if (m_currentCharacterTextureTop != null)
                m_spriteBatch.Draw(m_currentCharacterTextureTop, new Vector2(m_panTop * -Space.PlayAreaSize.X, 40.0f), Color.White);
            if (m_currentLinesTop != null)
            {
                Vector2 linePosition = new Vector2(120.0f - Space.PlayAreaSize.X * m_panTop, 40.0f);
                foreach (string line in m_currentLinesTop)
                {
                    GraphicsUtility.DrawOutlinedText(m_spriteBatch, m_spriteFont, linePosition, line, Color.YellowGreen);
                    linePosition.Y += m_spriteFont.LineSpacing;
                }
            }

            // draw bottom cutscene lines image with panning
            if (m_currentLinesBottom != null)
            {
                Vector2 linePosition = new Vector2(120.0f + Space.PlayAreaSize.X * m_panBottom, 400.0f);
                foreach (string line in m_currentLinesBottom)
                {
                    GraphicsUtility.DrawOutlinedText(m_spriteBatch, m_spriteFont, linePosition, line, Color.YellowGreen);
                    linePosition.Y += m_spriteFont.LineSpacing;
                }
            }

            // draw tap prompt if due
            if (m_tapCountdown == 0.0f)
            {
                bool tapOn = (int)gameTime.TotalGameTime.TotalSeconds % 2 == 0;
                Vector2 textPosition = m_cutsceneTop
                    ? new Vector2(240.0f, Space.PlayAreaSize.Y * 0.75f)
                    : new Vector2(240.0f, Space.PlayAreaSize.Y * 0.25f);
                Vector2 textOffset = new Vector2(0.0f, m_spriteFont.LineSpacing * 0.5f);

                if (tapOn)
                {
                    GraphicsUtility.DrawOutlinedText(
                        m_spriteBatch, m_spriteFont, textPosition - textOffset,
                        "Tap To Continue", Color.LightCyan, TextAlignment.Centre);
                    GraphicsUtility.DrawOutlinedText(
                        m_spriteBatch, m_spriteFont, textPosition + textOffset,
                        "Hold To Skip Story", Color.LightCyan, TextAlignment.Centre);
                }
            }

            m_spriteBatch.End();

            base.Draw(gameTime);
        }

        public void Reset()
        {
            m_cutsceneIndex = 0;
            m_cutsceneTop = true;
            UpdateCutscene();
        }

        public bool NarrativeComplete
        {
            get { return m_cutsceneIndex >= m_cutscene.Length; }
        }

        protected override void LoadContent()
        {
            ContentManager contentManager = Game.Content;

            // overlay
            m_screenOverlayTexture = contentManager.Load<Texture2D>(@"Graphics\ScreenOverlay");

            // font
            m_spriteFont = contentManager.Load<SpriteFont>(@"Fonts\ButtonFont");

            // cache character portraits
            foreach (CutsceneLineDefinition cutsceneLineDefinition in m_cutscene)
            {
                contentManager.Load<Texture2D>(
                    GetCharacterAsset(cutsceneLineDefinition.CharacterId));
            }

            base.LoadContent();
        }

        private void UpdateCutscene()
        {
            if (m_cutsceneIndex >= m_cutscene.Length)
                return;

            CutsceneLineDefinition cutsceneLineDefinition = m_cutscene[m_cutsceneIndex];

            ContentManager contentManager = Game.Content;

            string characterAsset = GetCharacterAsset(cutsceneLineDefinition.CharacterId);
            Texture2D currentCharacterTexture = contentManager.Load<Texture2D>(characterAsset);

            IEnumerable<string> currentLines = GraphicsUtility.WrapText(m_spriteFont, 360.0f,
                cutsceneLineDefinition.Text);

            if (m_cutsceneTop)
            {
                m_currentCharacterTextureTop = currentCharacterTexture;
                m_currentLinesTop = currentLines;
            }
            else
            {
                m_currentCharacterTextureBottom = currentCharacterTexture;
                m_currentLinesBottom = currentLines;
            }
        }

        private string GetCharacterAsset(string characterId)
        {
            return @"Graphics\Cutscene" + characterId;
        }

        private CutsceneLineDefinition[] m_cutscene;
        private int m_cutsceneIndex;

        private bool m_cutsceneTop;

        private Texture2D m_currentCharacterTextureTop;
        private IEnumerable<string> m_currentLinesTop;
        private float m_panTop;

        private Texture2D m_currentCharacterTextureBottom;
        private IEnumerable<string> m_currentLinesBottom;
        private float m_panBottom;
        private float m_tapCountdown;

        private TouchInterface m_touchInterface;
        private SpriteBatch m_spriteBatch;
        private Texture2D m_screenOverlayTexture;
        private SpriteFont m_spriteFont;
    }
}
