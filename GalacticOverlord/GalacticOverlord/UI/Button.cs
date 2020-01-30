using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Input.Touch;
using Microsoft.Xna.Framework.Audio;

namespace GalacticOverlord.UI
{
    public class Button: ModularGameComponent
    {
        #region Public Methods

        public Button(TouchInterface touchInterface, Vector2 position, float width, string text)
            : base(touchInterface.GalacticOverlordGame)
        {
            m_touchInterface = touchInterface;
            m_text = text;
            m_tag = null;
            m_position.X = (int)position.X;
            m_position.Y = (int)position.Y;
            m_width = (int)width;
            m_panMode = PanMode.PanOut;
            m_panOffset = PanTime;
        }

        public override void Initialize()
        {
            m_spriteBatch = new SpriteBatch(Game.GraphicsDevice);

            base.Initialize();
        }

        public override void Update(GameTime gameTime)
        {
            float panDelta = (float)gameTime.ElapsedGameTime.TotalSeconds;

            switch (m_panMode)
            {
                case PanMode.PanIn:
                    if (m_panOffset >= panDelta)
                        m_panOffset -= panDelta;
                    else
                        m_panMode = PanMode.Active;
                    break;
                case PanMode.Active:
                    m_panOffset = 0.0f;

                    foreach (GestureSample gestureSample in m_touchInterface.GestureSamples)
                    {
                        if (gestureSample.GestureType != GestureType.Tap)
                            continue;

                        Vector2 tapPosition = gestureSample.Position;

                        if (tapPosition.X >= m_position.X && tapPosition.X < m_position.X + m_width
                            && tapPosition.Y >= m_position.Y && tapPosition.Y < m_position.Y + 64.0f)
                        {
                            GalacticOverlordGame.AudioManager.PlaySound(m_buttonSoundEffect);

                            if (m_tapEventHandler != null)
                                m_tapEventHandler(this, EventArgs.Empty);
                        }
                    }

                    break;
                case PanMode.PanOut:
                    if (m_panOffset < PanTime)
                        m_panOffset += panDelta;
                    break;
            }

            base.Update(gameTime);
        }

        public override void Draw(GameTime gameTime)
        {
            m_spriteBatch.Begin();

            Vector2 buttonPosition = m_position;
            float renderOffset = m_panOffset * (m_panOffset + m_position.Y / 800.0f) * 3200.0f;
            buttonPosition.Y += renderOffset;

            // left button side
            m_spriteBatch.Draw(m_buttonLeftTexture, buttonPosition, Color.White);

            // button middle
            Vector2 segmentPosition = buttonPosition;
            m_middleRectangle.Y = (int)(m_position.Y + renderOffset);
            m_spriteBatch.Draw(m_buttonMiddleTexture, m_middleRectangle, null, Color.White);

            // right button side
            segmentPosition.X += m_width - m_buttonRightTexture.Width;
            m_spriteBatch.Draw(m_buttonRightTexture, segmentPosition, Color.White);

            // text
            Vector2 textPosition = buttonPosition + new Vector2(m_width * 0.5f, 32.0f);
            Color textColour = m_tapEventHandler != null ? Color.LightSkyBlue : Color.Gray;
            GraphicsUtility.DrawOutlinedText(m_spriteBatch, m_buttonFont, textPosition, m_text, textColour, TextAlignment.Centre);

            // optional button image
            if (m_imageTexture != null)
            {
                Vector2 imagePosition = m_position;
                imagePosition.X += (m_width - m_imageTexture.Width) * 0.5f;
                imagePosition.Y += (m_buttonMiddleTexture.Height - m_imageTexture.Height) * 0.5f + renderOffset;
                m_spriteBatch.Draw(m_imageTexture, imagePosition, Color.White);
            }

            m_spriteBatch.End();

            base.Draw(gameTime);
        }

        public void PanIn()
        {
            m_panMode = PanMode.PanIn;
        }

        public void PanOut()
        {
            m_panMode = PanMode.PanOut;
        }

        #endregion

        #region Public Properties

        public string Text
        {
            get { return m_text; }
            set { m_text = value; }
        }

        public Texture2D Image
        {
            get { return m_imageTexture; }
            set { m_imageTexture = value; }
        }

        public object Tag
        {
            get { return m_tag; }
            set { m_tag = value; }
        }

        public bool PannedIn
        {
            get { return m_panMode == PanMode.Active; }
        }

        public bool PannedOut
        {
            get { return m_panOffset >= PanTime; }
        }

        #endregion

        #region Public Events

        public event EventHandler Tapped
        {
            add { m_tapEventHandler += value; }
            remove { m_tapEventHandler -= value; }
        }

        #endregion

        #region Protected Methods

        protected override void LoadContent()
        {
            ContentManager contentManager = Game.Content;
            m_buttonLeftTexture = contentManager.Load<Texture2D>(@"Graphics\ButtonLeft");
            m_buttonMiddleTexture = contentManager.Load<Texture2D>(@"Graphics\ButtonMiddle");
            m_buttonRightTexture = contentManager.Load<Texture2D>(@"Graphics\ButtonRight");
            m_buttonFont = contentManager.Load<SpriteFont>(@"Fonts\ButtonFont");
            m_buttonSoundEffect = contentManager.Load<SoundEffect>(@"Audio\ButtonTap");

            m_middleRectangle = new Rectangle(
                (int)m_position.X + m_buttonLeftTexture.Width, (int)m_position.Y,
                (int)m_width - m_buttonLeftTexture.Width - m_buttonRightTexture.Width,
                m_buttonMiddleTexture.Height);

            base.LoadContent();
        }

        #endregion

        #region Private Constants

        private const float PanTime = 0.5f;

        #endregion

        #region Private Fields

        private TouchInterface m_touchInterface;
        private Vector2 m_position;
        private float m_width;
        private Rectangle m_middleRectangle;
        private string m_text;
        private Texture2D m_imageTexture;
        private object m_tag;
        private PanMode m_panMode;
        private float m_panOffset;

        private event EventHandler m_tapEventHandler;

        private SpriteBatch m_spriteBatch;
        private Texture2D m_buttonLeftTexture;
        private Texture2D m_buttonMiddleTexture;
        private Texture2D m_buttonRightTexture;
        private SpriteFont m_buttonFont;
        private SoundEffect m_buttonSoundEffect;

        #endregion

        #region Private Enums

        private enum PanMode
        {
            PanIn,
            Active,
            PanOut
        }

        #endregion
    }
}
