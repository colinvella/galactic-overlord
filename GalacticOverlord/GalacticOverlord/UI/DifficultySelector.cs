using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input.Touch;
using GalacticOverlord.Players;
using GalacticOverlord.Pipeline;

namespace GalacticOverlord.UI
{
    public class DifficultySelector: ModularGameComponent
    {
        public DifficultySelector(TouchInterface touchInterface)
            :base(touchInterface.GalacticOverlordGame)
        {
            m_touchInterface = touchInterface;

            m_fadeState = FadeState.Inactive;
            m_opacity = 0.0f;
        }

        public override void Initialize()
        {

            m_spriteBatch = new SpriteBatch(Game.GraphicsDevice);

            base.Initialize();
        }

        public override void Update(GameTime gameTime)
        {
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            switch (m_fadeState)
            {
                case FadeState.Inactive:
                    m_opacity = 0.0f;
                    break;
                case FadeState.FadeIn:
                    m_opacity += deltaTime * 2.0f;
                    if (m_opacity >= 1.0f)
                    {
                        m_opacity = 1.0f;
                        m_fadeState = FadeState.Active;
                    }
                    break;
                case FadeState.Active:
                    m_opacity = 1.0f;

                    foreach (GestureSample gestureSample in m_touchInterface.GestureSamples)
                    {
                        GestureType gestureType = gestureSample.GestureType;
                        if (gestureType != GestureType.HorizontalDrag
                            && gestureType != GestureType.DragComplete)
                            continue;

                        Vector2 location = gestureSample.Position;

                        int textureWidth = m_difficultyRankTexture[0].Width;

                        UserProfile userProfile = GalacticOverlordGame.UserProfile;

                        if (gestureType == GestureType.HorizontalDrag)
                        {
                            if (location.Y < 240 | location.Y > 304)
                                continue;

                            m_slideOffset -= gestureSample.Delta.X;
                            m_slideOffset = MathHelper.Clamp(m_slideOffset, 0.0f, textureWidth * (MaxDifficulty));
                            userProfile.Difficulty = (DifficultyLevel)(int)Math.Round(m_slideOffset / textureWidth);

                            if (m_difficultyChangedEventHandler != null)
                                m_difficultyChangedEventHandler(this, EventArgs.Empty);
                        }
                        else
                        {
                            userProfile.Difficulty = (DifficultyLevel)(int)Math.Round(m_slideOffset / textureWidth);
                            m_slideOffset = (int)userProfile.Difficulty * textureWidth;
                            userProfile.StoreProperties();
                        }
                    }

                    break;
                case FadeState.FadeOut:
                    m_opacity -= deltaTime * 2.0f;
                    if (m_opacity <= 0.0f)
                    {
                        m_opacity = 0.0f;
                        m_fadeState = FadeState.Inactive;
                    }
                    break;
            }

            base.Update(gameTime);
        }

        public override void Draw(GameTime gameTime)
        {
            m_spriteBatch.Begin();
            m_spriteBatch.Draw(m_difficultySelectorTexture, new Vector2(240 - 160, 240), Color.White * m_opacity);

            Vector2 position = new Vector2(240 - 32, 240);
            float width = m_difficultyRankTexture[0].Width;
            position.X -= m_slideOffset;

            for (int difficulty = 0; difficulty <= MaxDifficulty; difficulty++)
            {
                float difficultyOpacity = (m_slideOffset - difficulty * width) * 0.4f / width;
                difficultyOpacity = 1.0f - Math.Abs(MathHelper.Clamp(difficultyOpacity, -1.0f, 1.0f));
                m_spriteBatch.Draw(m_difficultyRankTexture[difficulty], position, Color.White * m_opacity * difficultyOpacity);
                position.X += width;
            }

            UserProfile userProfile = GalacticOverlordGame.UserProfile;

            GraphicsUtility.DrawOutlinedText(m_spriteBatch, m_spriteFont,
                new Vector2(240, 320),
                "Difficulty: " + userProfile.Difficulty, Color.LightSkyBlue * m_opacity, TextAlignment.Centre);

            m_spriteBatch.End();

            base.Draw(gameTime);
        }

        public void FadeIn()
        {
            if (m_fadeState != FadeState.Active)
                m_fadeState = FadeState.FadeIn;
        }

        public void FadeOut()
        {
            if (m_fadeState != FadeState.Inactive)
                m_fadeState = FadeState.FadeOut;
        }

        public event EventHandler DifficultyChanged
        {
            add { m_difficultyChangedEventHandler += value; }
            remove { m_difficultyChangedEventHandler -= value; }
        }

        protected override void LoadContent()
        {
            ContentManager contentManager = Game.Content;
            m_difficultySelectorTexture
                = contentManager.Load<Texture2D>(@"Graphics\DifficultySelector");

            m_difficultyRankTexture = new Texture2D[MaxDifficulty + 1];
            for (int difficulty = 0; difficulty <= MaxDifficulty; difficulty++)
                m_difficultyRankTexture[difficulty]
                    = contentManager.Load<Texture2D>(@"Graphics\Difficulty" + difficulty);

            m_spriteFont = contentManager.Load<SpriteFont>(@"Fonts\ButtonFont");

            UserProfile userProfile = GalacticOverlordGame.UserProfile;
            m_slideOffset = (int)userProfile.Difficulty * m_difficultyRankTexture[0].Width;

            base.LoadContent();
        }

        private const int MaxDifficulty = 5;

        private TouchInterface m_touchInterface;
        private float m_slideOffset;

        private FadeState m_fadeState;
        private float m_opacity;

        private event EventHandler m_difficultyChangedEventHandler;

        private SpriteBatch m_spriteBatch;
        private Texture2D m_difficultySelectorTexture;
        private Texture2D[] m_difficultyRankTexture;
        private SpriteFont m_spriteFont;

        private enum FadeState
        {
            Inactive,
            FadeIn,
            Active,
            FadeOut
        }
    }
}
