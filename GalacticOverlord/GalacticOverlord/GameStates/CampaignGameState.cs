using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input.Touch;
using Microsoft.Xna.Framework.Input;
using GalacticOverlord.UI;
using GalacticOverlord.Players;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.GamerServices;
using GalacticOverlord.Net;
using System.Net;
using GalacticOverlord.Pipeline;

namespace GalacticOverlord.GameStates
{
    public class CampaignGameState : GameState
    {
        #region Public Methods

        public CampaignGameState(GalacticOverlordGame galacticOverlordGame)
            : base(galacticOverlordGame)
        {
        }

        public override void Initialize()
        {
            // load campaign content here as it is needed before LoadContent is called
            ContentManager contentManager = Game.Content;
            m_campaignDefinition = contentManager.Load<CampaignDefinition>(@"CampaignDefinition");

            m_uiState = UIState.FadeIn;
            m_fadeFactor = 1.0;

            m_touchInterface = new TouchInterface(GalacticOverlordGame);
            ChildComponents.Add(m_touchInterface);

            m_levelButtons = new List<Button>();
            int levelCount = m_campaignDefinition.Levels.Length;
            int level = 0;
            for (int levelY = 0; levelY < LevelsDown; levelY++)
            {
                for (int levelX = 0; levelX < LevelsAcross; levelX++)
                {
                    ++level;

                    if (level > levelCount)
                        break;

                    Button levelButton = new Button(m_touchInterface,
                        new Vector2(
                            LevelButtonOffsetX + levelX * LevelButtonDeltaX,
                            LevelButtonOffsetY + levelY * LevelButtonDeltaY),
                        LevelButtonWidth, level.ToString());
                    levelButton.Tag = level;
                    levelButton.Tapped += OnTappedLevelButton;
                    levelButton.PanIn();
                    m_levelButtons.Add(levelButton);
                    ChildComponents.Add(levelButton);
                }

                if (level > levelCount)
                    break;
            }

            m_nextBattleButton = new Button(m_touchInterface, new Vector2(20, 600), 200.0f, "Next Battle");
            m_nextBattleButton.PanIn();
            m_nextBattleButton.Visible = true;
            if (GalacticOverlordGame.UserProfile.CampaignProgress < m_campaignDefinition.Levels.Length)
                m_nextBattleButton.Tapped += OnTappedNextBattle;
            ChildComponents.Add(m_nextBattleButton);

            m_backButton = new Button(m_touchInterface, new Vector2(260, 600), 200.0f, "Back");
            m_backButton.PanIn();
            m_backButton.Visible = true;
            m_backButton.Tapped += OnTappedBack;
            ChildComponents.Add(m_backButton);

            m_spriteBatch = new SpriteBatch(Game.GraphicsDevice);

            // do base initialise first to get content
            base.Initialize();
        }

        public override void Update(GameTime gameTime)
        {
            double deltaTime = gameTime.ElapsedGameTime.TotalSeconds;
            switch (m_uiState)
            {
                case UIState.FadeIn:
                    m_fadeFactor -= deltaTime * 2.0f;
                    if (m_fadeFactor < 0.0)
                    {
                        m_fadeFactor = 0.0;
                        m_uiState = UIState.Active;
                    }
                    break;
                case UIState.FadeOut:
                    m_fadeFactor += deltaTime * 2.0f;
                    if (m_fadeFactor > 1.0)
                    {
                        m_fadeFactor = 1.0;
                        m_uiState = UIState.Inactive;
                    }
                    break;
            }

            // go to title if Back pressed
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
            {
                OnTappedBack(this, EventArgs.Empty);
            }

            if (m_uiState == UIState.Inactive)
            {
                if (m_tappedButton == m_backButton)
                    SwitchToState(new TitleGameState(GalacticOverlordGame));
            }

            base.Update(gameTime);
        }

        public override void Draw(GameTime gameTime)
        {
            m_spriteBatch.Begin();

            m_spriteBatch.Draw(m_multiplayerBackgroundTexture, Vector2.Zero, Color.White);
            m_spriteBatch.Draw(m_titleBackgroundTexture, Vector2.Zero, new Color(new Vector4((float)m_fadeFactor)));

            float textX = 20 - (float)m_fadeFactor * 480.0f;
            GraphicsUtility.DrawOutlinedText(m_spriteBatch, m_headerFont,
                new Vector2(textX, 20), "Campaign",
                Color.LightCyan, TextAlignment.TopLeft);

            m_spriteBatch.End();

            base.Draw(gameTime);
        }

        public override void Shutdown()
        {
            base.Shutdown();
        }

        #endregion

        #region Protected Methods

        protected override void LoadContent()
        {
            ContentManager contentManager = Game.Content;

            m_titleBackgroundTexture = contentManager.Load<Texture2D>(@"Graphics\TitleBackground");
            m_multiplayerBackgroundTexture = contentManager.Load<Texture2D>(@"Graphics\CampaignBackground");
            m_lockTexture = contentManager.Load<Texture2D>(@"Graphics\Lock");
            m_headerFont = contentManager.Load<SpriteFont>(@"Fonts\HeaderFont");
            m_smallFont = contentManager.Load<SpriteFont>(@"Fonts\PlanetFont");

            // initialise campaign level buttons
            int campaignProgress = GalacticOverlordGame.UserProfile.CampaignProgress;
            for (int levelIndex = campaignProgress + 1; levelIndex < m_campaignDefinition.Levels.Length; levelIndex++)
                m_levelButtons[levelIndex].Image = m_lockTexture;

            m_titleLoop = contentManager.Load<SoundEffect>(@"Audio\TitleLoop");
            AudioManager audioManager = GalacticOverlordGame.AudioManager;
            if (!audioManager.MusicPlaying)
                audioManager.PlayMusic(m_titleLoop);

            base.LoadContent();
        }

        #endregion

        #region Private Methods

        private void OnTappedLevelButton(object sender, EventArgs eventArgs)
        {
            Button levelButton = (Button)sender;
            int levelTag = (int)levelButton.Tag;
            int levelIndex = levelTag - 1;

            // TEMP: progress check disabled for testing
            //if (levelIndex < m_campaignDefinition.Levels.Length && levelIndex <= GalacticOverlordGame.UserProfile.CampaignProgress)
            if (levelIndex < m_campaignDefinition.Levels.Length)
            {
                SwitchToState(new PlayGameState(GalacticOverlordGame,
                    m_campaignDefinition, levelIndex));
            }
        }

        private void OnTappedNextBattle(object sender, EventArgs eventArgs)
        {
            int nextLevelIndex = GalacticOverlordGame.UserProfile.CampaignProgress;
            SwitchToState(new PlayGameState(
                GalacticOverlordGame, m_campaignDefinition, nextLevelIndex));
        }

        private void OnTappedBack(object sender, EventArgs eventArgs)
        {
            foreach (Button levelButton in m_levelButtons)
                levelButton.PanOut();

            m_nextBattleButton.PanOut();
            m_backButton.PanOut();
            m_tappedButton = m_backButton;
            m_uiState = UIState.FadeOut;
        }

        #endregion

        #region Private Constants

        private const int LevelsAcross = 4;
        private const int LevelsDown = 5;

        private const int LevelButtonOffsetX = 20;
        private const int LevelButtonOffsetY = 100;
        private const int LevelButtonDeltaX = 115;
        private const int LevelButtonDeltaY = 80;
        private const int LevelButtonWidth = 95;

        #endregion

        #region Private Fields

        private CampaignDefinition m_campaignDefinition;

        private UIState m_uiState;
        private double m_fadeFactor;

        private TouchInterface m_touchInterface;
        private List<Button> m_levelButtons;
        private Button m_nextBattleButton;
        private Button m_backButton;
        private Button m_tappedButton;

        private Texture2D m_titleBackgroundTexture;
        private Texture2D m_multiplayerBackgroundTexture;
        private Texture2D m_lockTexture;
        private SpriteBatch m_spriteBatch;
        private SpriteFont m_headerFont;
        private SpriteFont m_smallFont;

        private SoundEffect m_titleLoop;

        private enum UIState
        {
            FadeIn,
            Active,
            FadeOut,
            Inactive
        }

        #endregion
    }
}
