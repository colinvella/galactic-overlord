using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GalacticOverlord.UI
{
    public class ScreenOverlay : ModularGameComponent
    {
        public ScreenOverlay(GalacticOverlordGame galacticOverlordGame)
            : base(galacticOverlordGame)
        {
            m_textEntries = new List<TextEntry>();
        }

        public override void Initialize()
        {
            m_spriteBatch = new SpriteBatch(Game.GraphicsDevice);
            base.Initialize();
        }

        public override void Draw(GameTime gameTime)
        {
            m_spriteBatch.Begin();
            m_spriteBatch.Draw(m_screenBlindTexture, Vector2.Zero, Color.White);

            foreach (TextEntry textEntry in m_textEntries)
                GraphicsUtility.DrawOutlinedText(m_spriteBatch,
                    textEntry.SpriteFont, textEntry.Position, textEntry.Text, textEntry.Colour, TextAlignment.Centre);

            m_spriteBatch.End();

            base.Draw(gameTime);
        }

        public void ClearText()
        {
            m_textEntries.Clear();
        }

        public void AddText(SpriteFont spriteFont, Vector2 position, Color colour, string text)
        {
            m_textEntries.Add(new TextEntry(spriteFont, position, colour, text));
        }

        protected override void LoadContent()
        {
            m_screenBlindTexture = Game.Content.Load<Texture2D>(@"Graphics\ScreenOverlay");
            base.LoadContent();
        }

        private List<TextEntry> m_textEntries;
        private SpriteBatch m_spriteBatch;
        private Texture2D m_screenBlindTexture;

        private struct TextEntry
        {
            public TextEntry(SpriteFont spriteFont, Vector2 position, Color colour, string text)
            {
                SpriteFont = spriteFont;
                Position = position;
                Colour = colour;
                Text = text;
            }

            public SpriteFont SpriteFont;
            public Vector2 Position;
            public Color Colour;
            public string Text;
        }
    }
}
