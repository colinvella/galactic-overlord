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
using Microsoft.Phone.Tasks;
using GalacticOverlord.Pipeline;
using GalacticOverlord.Core;

namespace GalacticOverlord.GameStates
{
    public class TitleGameState: GameState
    {
        #region Public Methods

        public TitleGameState(GalacticOverlordGame galacticOverlordGame)
            : base(galacticOverlordGame)
        {
        }

        public override void Initialize()
        {
            m_uiState = UIState.FadeIn;
            m_fadeFactor = 0.0f;
            m_demoCountdown = DemoCountdownInitialValue;

            m_touchInterface = new TouchInterface(GalacticOverlordGame);
            ChildComponents.Add(m_touchInterface);

            m_difficultySelector = new DifficultySelector(m_touchInterface);
            m_difficultySelector.DifficultyChanged += OnDifficultyChanged;
            ChildComponents.Add(m_difficultySelector);

            InitialiseMenus();

            OpenMainMenu();

            m_spriteBatch = new SpriteBatch(Game.GraphicsDevice);

            // 3d starship positions
            m_random = new Random();
            m_starshipPosition = new Matrix[StarshipCount];
            m_starshipOrientation = new float[StarshipCount];
            m_starshipRotation = new float[StarshipCount];
            for (int index = 0; index < StarshipCount; index++)
            {
                m_starshipPosition[index] = Matrix.CreateRotationY(MathHelper.PiOver2);
                m_starshipPosition[index].Translation = new Vector3(0, 0, index * -StarshipDistance / StarshipCount);
                m_starshipRotation[index] = -MathHelper.PiOver4 + (float)m_random.NextDouble() * MathHelper.PiOver2;
            }

            // explosion positions
            m_explosionPosition = new Vector2[ExplosionCount];
            m_explosionLife = new float[ExplosionCount];
            for (int index = 0; index < ExplosionCount; index++)
            {
                m_explosionPosition[index] = GenerateExplosionPosition();
                m_explosionLife[index] = index * 1.0f / ExplosionCount;
            }

            // states for 3d rendering
            m_depthStencilStateAlpha = new DepthStencilState();
            m_depthStencilStateAlpha.DepthBufferEnable = true;

            m_depthStencilStateAdditive = new DepthStencilState();
            m_depthStencilStateAdditive.DepthBufferEnable = true;
            m_depthStencilStateAdditive.DepthBufferWriteEnable = false;

            m_samplerState = new SamplerState();
            m_samplerState.Filter = TextureFilter.Linear;

            base.Initialize();
        }

        public override void Update(GameTime gameTime)
        {
            // remove Buy button if returning from marketplace as bought
            if (m_buyButton != null && !m_buyButton.PannedOut && !GalacticOverlordGame.UserProfile.IsTrialMode)
                m_buyButton.PanOut();

            // exit game if Back pressed here
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
            {
                // reset demo countdown
                m_demoCountdown = DemoCountdownInitialValue;

                switch (m_menuState)
                {
                    case MenuState.Main:
                        Game.Exit();
                        break;
                    case MenuState.SinglePlayerSkirmish:
                        m_tappedButton = m_singlePlayerSkirmishBackButton;
                        CloseSinglePlayerSkirmishMenu();
                        break;
                    case MenuState.Settings:
                        m_tappedButton = m_settingsBackButton;
                        CloseSettingsMenu();;
                        break;
                }
            }

            // test for FB and twitter
            CheckSocialMediaIcons();

            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // switch to demo if idle for some time
            m_demoCountdown -= deltaTime;
            if (m_demoCountdown <= 0.0f)
                SwitchToState(new DemoGameState(GalacticOverlordGame));

            switch(m_uiState)
            {
                case UIState.FadeIn:
                    m_fadeFactor += deltaTime * 2.0f;
                    if (m_fadeFactor >= 1.0f)
                    {
                        m_fadeFactor = 1.0f;
                        m_uiState = UIState.Active;
                    }
                    break;
                case UIState.Active:
                    m_fadeFactor = 1.0f;
                    break;
                case UIState.FadeOut:
                    m_fadeFactor = Math.Max(0.0f, m_fadeFactor - deltaTime * 2.0f);
                    break;
            }

            if (m_singlePlayerButton.PannedOut
                && m_multiPlayerButton.PannedOut
                && m_settingsButton.PannedOut

                && m_singlePlayerCampaignButton.PannedOut
                && m_singlePlayerSkirmishButton.PannedOut
                && m_singlePlayerBackButton.PannedOut

                && m_singlePlayerSkirmishDuelButton.PannedOut
                && m_singlePlayerSkirmishThreeWayButton.PannedOut
                && m_singlePlayerSkirmishAsteroidsButton.PannedOut
                && m_singlePlayerSkirmishBackButton.PannedOut
                && m_settingsMusicButton.PannedOut
                && m_settingsSfxButton.PannedOut
                && m_settingsBackButton.PannedOut)
            {
                DifficultyLevel difficultyLevel = GalacticOverlordGame.UserProfile.Difficulty;

                // reset demo countdown
                if (m_tappedButton != null)
                    m_demoCountdown = DemoCountdownInitialValue;

                if (m_tappedButton == m_singlePlayerButton)
                {
                    m_tappedButton = null;
                    OpenSinglePlayerMenu();
                }
                else if (m_tappedButton == m_multiPlayerButton)
                {
                    m_tappedButton = null;
                    SwitchToState(new HostGameState(GalacticOverlordGame));
                }
                else if (m_tappedButton == m_settingsButton)
                {
                    m_tappedButton = null;
                    OpenSettingsMenu();
                }
                else if (m_tappedButton == m_singlePlayerCampaignButton)
                {
                    m_tappedButton = null;
                    SwitchToState(new CampaignGameState(GalacticOverlordGame));
                }
                else if (m_tappedButton == m_singlePlayerSkirmishButton)
                {
                    m_tappedButton = null;
                    OpenSinglePlayerSkirmishMenu();
                }
                else if (m_tappedButton == m_singlePlayerBackButton)
                {
                    m_tappedButton = null;
                    OpenMainMenu();
                }
                else if (m_tappedButton == m_singlePlayerSkirmishDuelButton)
                {
                    m_tappedButton = null;
                    SwitchToState(new PlayGameState(GalacticOverlordGame, SkirmishMode.Duel, difficultyLevel));
                }
                else if (m_tappedButton == m_singlePlayerSkirmishThreeWayButton)
                {
                    m_tappedButton = null;
                    SwitchToState(new PlayGameState(GalacticOverlordGame, SkirmishMode.ThreeWay, difficultyLevel));
                }
                else if (m_tappedButton == m_singlePlayerSkirmishCloakedButton)
                {
                    m_tappedButton = null;
                    SwitchToState(new PlayGameState(GalacticOverlordGame, SkirmishMode.Cloaked, difficultyLevel));
                }
                else if (m_tappedButton == m_singlePlayerSkirmishAsteroidsButton)
                {
                    m_tappedButton = null;
                    SwitchToState(new PlayGameState(GalacticOverlordGame, SkirmishMode.Asteroids, difficultyLevel));
                }
                else if (m_tappedButton == m_singlePlayerSkirmishBackButton)
                {
                    m_tappedButton = null;
                    OpenSinglePlayerMenu();
                }
                else if (m_tappedButton == m_settingsBackButton)
                {
                    m_tappedButton = null;
                    OpenMainMenu();
                }
            }

            // planet blast animations
            for (int index = 0; index < ExplosionCount; index++)
            {
                m_explosionLife[index] += deltaTime;
                if (m_explosionLife[index] >= 1.0f)
                {
                    m_explosionPosition[index] = GenerateExplosionPosition();
                    m_explosionLife[index] -= 1.0f;
                }
            }

            // 3d ship animation
            for (int index = 0; index < StarshipCount; index++)
            {
                Vector3 shipPosition = m_starshipPosition[index].Translation;
                shipPosition.Z -= deltaTime * StarshipSpeed;
                m_starshipOrientation[index] += m_starshipRotation[index] * deltaTime;
                if (shipPosition.Z < -StarshipDistance)
                {
                    shipPosition.Z = 2.0f + (float)m_random.NextDouble() * 8.0f;
                    shipPosition.X = -1.0f + 2.0f * (float)m_random.NextDouble();
                    shipPosition.Y = -1.0f + 2.0f * (float)m_random.NextDouble();
                    m_starshipRotation[index] = -MathHelper.PiOver4 + (float)m_random.NextDouble() * MathHelper.PiOver2;
                }
                m_starshipPosition[index].Translation = shipPosition;
            }

            base.Update(gameTime);
        }

        public override void Draw(GameTime gameTime)
        {
            // background
            m_spriteBatch.Begin();
            m_spriteBatch.Draw(m_titleBackgroundTexture, Vector2.Zero, Color.White);
            m_spriteBatch.End();

            // explosion positions
            m_spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive);
            for (int index = 0; index < ExplosionCount; index++)
            {
                m_spriteBatch.Draw(m_explosionTexture, m_explosionPosition[index], null,
                    Color.White * (1.0f - m_explosionLife[index]), 0.0f,
                    m_explosionCentre, m_explosionLife[index] * 2.0f, SpriteEffects.None, 0.0f);
            }
            m_spriteBatch.End();

            // ship models
            GraphicsDevice.BlendState = BlendState.AlphaBlend;
            GraphicsDevice.SamplerStates[0] = m_samplerState;
            GraphicsDevice.DepthStencilState = m_depthStencilStateAlpha;
            for (int index = 0; index < StarshipCount; index++)
            {
                foreach (ModelMesh mesh in m_starshipHullModel.Meshes)
                {
                    foreach (BasicEffect effect in mesh.Effects)
                        effect.World = m_starshipPosition[index] * Matrix.CreateRotationZ(m_starshipOrientation[index]);
                    mesh.Draw();
                }
            }

            // jet models
            GraphicsDevice.DepthStencilState = m_depthStencilStateAdditive;
            for (int index = 0; index < StarshipCount; index++)
            {
                foreach (ModelMesh mesh in m_starshipJetModel.Meshes)
                {
                    foreach (BasicEffect effect in mesh.Effects)
                        effect.World = m_starshipPosition[index] * Matrix.CreateRotationZ(m_starshipOrientation[index]);
                    mesh.Draw();
                }
            }

            m_spriteBatch.Begin();

#if AD_DUPLEX
            // free edition text
            GraphicsUtility.DrawOutlinedText(m_spriteBatch, m_spriteFont, new Vector2(24.0f - (1.0f - m_fadeFactor) * 256.0f, 200.0f), "Free Edition", Color.LightSkyBlue);
#endif

            // facebook and twitter icons
            Vector2 iconOffset = new Vector2((1.0f - m_fadeFactor) * 400.0f, 0.0f);
            m_spriteBatch.Draw(m_facebookIconTexture, FacebookPosition + iconOffset, Color.White);
            m_spriteBatch.Draw(m_twitterIconTexture, TwitterPosition + iconOffset, Color.White);

            // title text
            Color titleColour = new Color(m_fadeFactor, m_fadeFactor, m_fadeFactor, m_fadeFactor);
            Vector2 origin = new Vector2(200.0f, 104.0f);
            m_spriteBatch.Draw(m_titleTextTexture, origin, null, titleColour, 0.0f, origin, 4.0f - m_fadeFactor * 3.0f, SpriteEffects.None, 0.0f);

            // version text
            GraphicsUtility.DrawOutlinedText(m_spriteBatch, m_spriteFont, new Vector2(456.0f + (1.0f - m_fadeFactor) * 256, 200), "Version 1.4", Color.LightSkyBlue, TextAlignment.TopRight);

            // trial mode text
            if (GalacticOverlordGame.UserProfile.IsTrialMode)
                GraphicsUtility.DrawOutlinedText(m_spriteBatch, m_spriteFont, new Vector2(464 + (1.0f - m_fadeFactor) * 256, 784), "Trial Mode", Color.LightSkyBlue, TextAlignment.BottomRight);

            m_spriteBatch.End();

            base.Draw(gameTime);
        }

        #endregion

        #region Protected Methods

        protected override void LoadContent()
        {
            ContentManager contentManager = Game.Content;

            m_titleBackgroundTexture = contentManager.Load<Texture2D>(@"Graphics\TitleBackground");
            m_titleTextTexture = contentManager.Load<Texture2D>(@"Graphics\TitleText");

            m_facebookIconTexture = contentManager.Load<Texture2D>(@"Graphics\FacebookIcon");
            m_twitterIconTexture = contentManager.Load<Texture2D>(@"Graphics\TwitterIcon");

            m_spriteFont = contentManager.Load<SpriteFont>(@"Fonts\PlanetFont");

            // planet explosions
            m_explosionTexture = contentManager.Load<Texture2D>(@"Graphics\TitlePlanetBlast");
            m_explosionCentre = new Vector2(m_explosionTexture.Width, m_explosionTexture.Height) * 0.5f;

            // load model and set fixed effect properties
            m_starshipHullModel = contentManager.Load<Model>(@"Models\StarshipHull");
            SetModelEffects(m_starshipHullModel);

            m_starshipJetModel = contentManager.Load<Model>(@"Models\StarshipJet");
            SetModelEffects(m_starshipJetModel);

            // music
            m_titleLoop = contentManager.Load<SoundEffect>(@"Audio\TitleLoop");
            AudioManager audioManager = GalacticOverlordGame.AudioManager;
            if (!audioManager.MusicPlaying)
                audioManager.PlayMusic(m_titleLoop);

            base.LoadContent();
        }

        #endregion

        #region Private Methods

        private void InitialiseMenus()
        {
            // main menu
            m_singlePlayerButton = new Button(m_touchInterface, new Vector2(80.0f, 320.0f), 320.0f, "Single Player");
            m_singlePlayerButton.Tapped += OnTapSinglePlayer;
            ChildComponents.Add(m_singlePlayerButton);

            m_multiPlayerButton = new Button(m_touchInterface, new Vector2(80.0f, 400.0f), 320.0f, "Multi Player");
            m_multiPlayerButton.Tapped += OnTapMultiPlayer;
            ChildComponents.Add(m_multiPlayerButton);

            m_settingsButton = new Button(m_touchInterface, new Vector2(80.0f, 480.0f), 320.0f, "Settings");
            m_settingsButton.Tapped += OnTapSettings;
            ChildComponents.Add(m_settingsButton);

            m_rateButton = new Button(m_touchInterface, new Vector2(80.0f, 560.0f), 320.0f, "Rate Me");
            m_rateButton.Tapped += OnTapRate;
            ChildComponents.Add(m_rateButton);

            if (GalacticOverlordGame.UserProfile.IsTrialMode)
            {
                m_buyButton = new Button(m_touchInterface, new Vector2(80.0f, 640.0f), 320.0f, "Buy Full Version");
                m_buyButton.Tapped += OnTapBuy;
                ChildComponents.Add(m_buyButton);
            }

            // single player menu
            m_singlePlayerCampaignButton = new Button(m_touchInterface, new Vector2(80.0f, 320.0f), 320.0f, "Campaign");
            m_singlePlayerCampaignButton.Tapped += OnTapSinglePlayerCampaign;
            ChildComponents.Add(m_singlePlayerCampaignButton);

            m_singlePlayerSkirmishButton = new Button(m_touchInterface, new Vector2(80.0f, 400.0f), 320.0f, "Skirmish");
            m_singlePlayerSkirmishButton.Tapped += OnTapSinglePlayerSkirmish;
            ChildComponents.Add(m_singlePlayerSkirmishButton);

            m_singlePlayerBackButton = new Button(m_touchInterface, new Vector2(80.0f, 480.0f), 320.0f, "Back");
            m_singlePlayerBackButton.Tapped += OnTapSinglePlayerBack;
            ChildComponents.Add(m_singlePlayerBackButton);

            // single player skirmish menu
            m_singlePlayerSkirmishDuelButton = new Button(m_touchInterface, new Vector2(20.0f, 400.0f), 210.0f, "Duel");
            m_singlePlayerSkirmishDuelButton.Tapped += OnTapSinglePlayerSkirmishDuel;
            ChildComponents.Add(m_singlePlayerSkirmishDuelButton);

            m_singlePlayerSkirmishThreeWayButton = new Button(m_touchInterface, new Vector2(250.0f, 400.0f), 210.0f, "Three Way");
            m_singlePlayerSkirmishThreeWayButton.Tapped += OnTapSinglePlayerSkirmishThreeWay;
            ChildComponents.Add(m_singlePlayerSkirmishThreeWayButton);

            m_singlePlayerSkirmishCloakedButton = new Button(m_touchInterface, new Vector2(20.0f, 480.0f), 210.0f, "Cloaked");
            m_singlePlayerSkirmishCloakedButton.Tapped += OnTapSinglePlayerSkirmishCloaked;
            ChildComponents.Add(m_singlePlayerSkirmishCloakedButton);

            m_singlePlayerSkirmishAsteroidsButton = new Button(m_touchInterface, new Vector2(250.0f, 480.0f), 210.0f, "Asteroids");
            m_singlePlayerSkirmishAsteroidsButton.Tapped += OnTapSinglePlayerSkirmishAsteroids;
            ChildComponents.Add(m_singlePlayerSkirmishAsteroidsButton);

            m_singlePlayerSkirmishBackButton = new Button(m_touchInterface, new Vector2(80.0f, 640.0f), 320.0f, "Back");
            m_singlePlayerSkirmishBackButton.Tapped += OnTapSinglePlayerSkirmishBack;
            ChildComponents.Add(m_singlePlayerSkirmishBackButton);

            // settings menu
            UserProfile userProfile = GalacticOverlordGame.UserProfile;

            string musicToggleText = userProfile.MusicEnabled ? OptionsMusicOn : OptionsMusicOff;
            m_settingsMusicButton = new Button(m_touchInterface, new Vector2(80.0f, 400.0f), 320.0f, musicToggleText);
            m_settingsMusicButton.Tapped += OnTapSettingsMusic;
            ChildComponents.Add(m_settingsMusicButton);

            string sfxToggleText = userProfile.SfxEnabled ? OptionsSfxOn : OptionsSfxOff;
            m_settingsSfxButton = new Button(m_touchInterface, new Vector2(80.0f, 480.0f), 320.0f, sfxToggleText);
            m_settingsSfxButton.Tapped += OnTapSettingsSfx;
            ChildComponents.Add(m_settingsSfxButton);

            m_settingsBackButton = new Button(m_touchInterface, new Vector2(80.0f, 560.0f), 320.0f, "Back");
            m_settingsBackButton.Tapped += OnTapSettingsBack;
            ChildComponents.Add(m_settingsBackButton);
        }

        private void OpenMainMenu()
        {
            m_menuState = MenuState.Main;
            m_singlePlayerButton.PanIn();
            m_multiPlayerButton.PanIn();
            m_settingsButton.PanIn();
            m_rateButton.PanIn();
            if (m_buyButton != null)
                m_buyButton.PanIn();

            TouchPanel.EnabledGestures = GestureType.Tap;
        }

        private void CloseMainMenu()
        {
            m_singlePlayerButton.PanOut();
            m_multiPlayerButton.PanOut();
            m_settingsButton.PanOut();
            m_rateButton.PanOut();
            if (m_buyButton != null)
                m_buyButton.PanOut();
        }

        private void OpenSinglePlayerMenu()
        {
            m_menuState = MenuState.SinglePlayer;

            m_singlePlayerCampaignButton.PanIn();
            m_singlePlayerSkirmishButton.PanIn();
            m_singlePlayerBackButton.PanIn();
        }

        private void CloseSinglePlayerMenu()
        {
            m_menuState = MenuState.SinglePlayer;

            m_singlePlayerCampaignButton.PanOut();
            m_singlePlayerSkirmishButton.PanOut();
            m_singlePlayerBackButton.PanOut();
        }

        private void OpenSinglePlayerSkirmishMenu()
        {
            m_menuState = MenuState.SinglePlayerSkirmish;

            m_difficultySelector.FadeIn();

            m_singlePlayerSkirmishDuelButton.PanIn();
            m_singlePlayerSkirmishThreeWayButton.PanIn();
            m_singlePlayerSkirmishCloakedButton.PanIn();
            m_singlePlayerSkirmishAsteroidsButton.PanIn();
            m_singlePlayerSkirmishBackButton.PanIn();

            TouchPanel.EnabledGestures = GestureType.Tap
                | GestureType.HorizontalDrag | GestureType.DragComplete;
        }

        private void CloseSinglePlayerSkirmishMenu()
        {
            m_difficultySelector.FadeOut();

            m_singlePlayerSkirmishDuelButton.PanOut();
            m_singlePlayerSkirmishThreeWayButton.PanOut();
            m_singlePlayerSkirmishCloakedButton.PanOut();
            m_singlePlayerSkirmishAsteroidsButton.PanOut();
            m_singlePlayerSkirmishBackButton.PanOut();
        }

        private void OpenSettingsMenu()
        {
            m_menuState = MenuState.Settings;
            m_settingsMusicButton.PanIn();
            m_settingsSfxButton.PanIn();
            m_settingsBackButton.PanIn();

            TouchPanel.EnabledGestures = GestureType.Tap;
        }

        private void CloseSettingsMenu()
        {
            m_settingsMusicButton.PanOut();
            m_settingsSfxButton.PanOut();
            m_settingsBackButton.PanOut();
        }

        private void CheckSocialMediaIcons()
        {
            foreach (GestureSample gestureSample in m_touchInterface.GestureSamples)
            {
                if (gestureSample.GestureType != GestureType.Tap)
                    continue;
                Vector2 tapPosition = gestureSample.Position;

                if (tapPosition.X >= FacebookPosition.X && tapPosition.X < FacebookPosition.X + m_facebookIconTexture.Width
                    && tapPosition.Y >= FacebookPosition.Y && tapPosition.Y < FacebookPosition.Y + m_facebookIconTexture.Height)
                {
                    WebBrowserTask webBrowserTask = new WebBrowserTask();
                    webBrowserTask.Uri = new Uri(FacebookLink);
                    webBrowserTask.Show();
                }
                else if (tapPosition.X >= TwitterPosition.X && tapPosition.X < TwitterPosition.X + m_twitterIconTexture.Width
                    && tapPosition.Y >= TwitterPosition.Y && tapPosition.Y < TwitterPosition.Y + m_twitterIconTexture.Height)
                {
                    WebBrowserTask webBrowserTask = new WebBrowserTask();
                    webBrowserTask.Uri = new Uri(TwitterLink);
                    webBrowserTask.Show();
                }
            }
        }

        private void ShowPurchasePrompt()
        {
            Guide.BeginShowMessageBox(
                "Galactic Overlord",
                "This game mode is available only in the full version.",
                new string[] { "Buy Now", "Maybe Later..." }, 1, MessageBoxIcon.Alert, OnShowPurchasePromptClosed, null);
        }

        private void SetModelEffects(Model model)
        {
            foreach (ModelMesh mesh in model.Meshes)
            {
                foreach (BasicEffect basicEffect in mesh.Effects)
                {
                    basicEffect.LightingEnabled = true;
                    basicEffect.DirectionalLight0.Direction = new Vector3(-1, -1, 1);

                    basicEffect.FogEnabled = true;
                    basicEffect.FogStart = StarshipDistance * 0.1f;
                    basicEffect.FogEnd = StarshipDistance * 0.9f;
                    basicEffect.FogColor = new Vector3(0.53f, 0.45f, 0.12f);

                    basicEffect.View = Matrix.CreateLookAt(new Vector3(-1, -1, 0),
                        new Vector3(1, 5, -20), Vector3.Up);
                    basicEffect.Projection = Matrix.CreatePerspectiveFieldOfView(
                        MathHelper.ToRadians(45.0f), Game.GraphicsDevice.Viewport.AspectRatio,
                        1.0f, 10000.0f);
                }
            }
        }

        private Vector2 GenerateExplosionPosition()
        {
            return new Vector2(
                (float)m_random.NextDouble() * Space.PlayAreaSize.X,
                (0.80f + (float)m_random.NextDouble() * 0.20f) * Space.PlayAreaSize.Y);
        }

        private void OnShowPurchasePromptClosed(IAsyncResult asyncResult)
        {
            int? buttonIndex = Guide.EndShowMessageBox(asyncResult);
            if (buttonIndex == 0)
                Guide.ShowMarketplace(PlayerIndex.One);
        }

        private void OnTapSinglePlayer(object sender, EventArgs eventArgs)
        {
            m_tappedButton = m_singlePlayerButton;
            CloseMainMenu();
        }

        private void OnTapMultiPlayer(object sender, EventArgs eventArgs)
        {
            if (GalacticOverlordGame.UserProfile.IsTrialMode)
                ShowPurchasePrompt();
            else
            {
                m_tappedButton = m_multiPlayerButton;
                CloseMainMenu();
                m_uiState = UIState.FadeOut;
            }
        }

        private void OnTapSettings(object sender, EventArgs eventArgs)
        {
            m_tappedButton = m_settingsButton;
            CloseMainMenu();
        }

        private void OnTapBuy(object sender, EventArgs eventArgs)
        {
            m_tappedButton = m_buyButton;

            Guide.ShowMarketplace(PlayerIndex.One);
        }

        private void OnTapRate(object sender, EventArgs eventArgs)
        {
            m_tappedButton = m_rateButton;

            MarketplaceReviewTask marketplaceReviewTask = new MarketplaceReviewTask();
            marketplaceReviewTask.Show();
        }

        private void OnTapSinglePlayerCampaign(object sender, EventArgs eventArgs)
        {
            m_tappedButton = m_singlePlayerCampaignButton;
            CloseSinglePlayerMenu();
            m_uiState = UIState.FadeOut;
        }

        private void OnTapSinglePlayerSkirmish(object sender, EventArgs eventArgs)
        {
            m_tappedButton = m_singlePlayerSkirmishButton;
            CloseSinglePlayerMenu();
        }

        private void OnTapSinglePlayerBack(object sender, EventArgs eventArgs)
        {
            m_tappedButton = m_singlePlayerBackButton;
            CloseSinglePlayerMenu();
        }

        private void OnTapSinglePlayerSkirmishDuel(object sender, EventArgs eventArgs)
        {
            m_tappedButton = m_singlePlayerSkirmishDuelButton;
            CloseSinglePlayerSkirmishMenu();
            m_uiState = UIState.FadeOut;
        }

        private void OnTapSinglePlayerSkirmishThreeWay(object sender, EventArgs eventArgs)
        {
            if (GalacticOverlordGame.UserProfile.IsTrialMode)
                ShowPurchasePrompt();
            else
            {
                m_tappedButton = m_singlePlayerSkirmishThreeWayButton;
                CloseSinglePlayerSkirmishMenu();
                m_uiState = UIState.FadeOut;
            }
        }

        private void OnTapSinglePlayerSkirmishCloaked(object sender, EventArgs eventArgs)
        {
            if (GalacticOverlordGame.UserProfile.IsTrialMode)
                ShowPurchasePrompt();
            else
            {
                m_tappedButton = m_singlePlayerSkirmishCloakedButton;
                CloseSinglePlayerSkirmishMenu();
                m_uiState = UIState.FadeOut;
            }
        }

        private void OnTapSinglePlayerSkirmishAsteroids(object sender, EventArgs eventArgs)
        {
            if (GalacticOverlordGame.UserProfile.IsTrialMode)
                ShowPurchasePrompt();
            else
            {
                m_tappedButton = m_singlePlayerSkirmishAsteroidsButton;
                CloseSinglePlayerSkirmishMenu();
                m_uiState = UIState.FadeOut;
            }
        }

        private void OnTapSinglePlayerSkirmishBack(object sender, EventArgs eventArgs)
        {
            m_tappedButton = m_singlePlayerSkirmishBackButton;
            CloseSinglePlayerSkirmishMenu();
        }

        private void OnTapSettingsMusic(object sender, EventArgs eventArgs)
        {
            UserProfile userProfile = GalacticOverlordGame.UserProfile;
            userProfile.MusicEnabled = !userProfile.MusicEnabled;
            userProfile.StoreProperties();
            m_settingsMusicButton.Text = userProfile.MusicEnabled ? OptionsMusicOn : OptionsMusicOff;
            m_tappedButton = m_settingsMusicButton;

            AudioManager audioManager = GalacticOverlordGame.AudioManager;
            if (userProfile.MusicEnabled)
                audioManager.PlayMusic(m_titleLoop);
            else
                audioManager.StopMusic();

            m_demoCountdown = DemoCountdownInitialValue;
        }

        private void OnTapSettingsSfx(object sender, EventArgs eventArgs)
        {
            UserProfile userProfile = GalacticOverlordGame.UserProfile;
            userProfile.SfxEnabled = !userProfile.SfxEnabled;
            userProfile.StoreProperties();
            m_settingsSfxButton.Text = userProfile.SfxEnabled ? OptionsSfxOn : OptionsSfxOff;
            m_tappedButton = m_settingsSfxButton;

            m_demoCountdown = DemoCountdownInitialValue;
        }

        private void OnTapSettingsBack(object sender, EventArgs eventArgs)
        {
            m_tappedButton = m_settingsBackButton;
            CloseSettingsMenu();
        }

        private void OnDifficultyChanged(object sender, EventArgs eventArgs)
        {
            m_demoCountdown = DemoCountdownInitialValue;
        }

        #endregion

        #region Private Constants

        private const float DemoCountdownInitialValue = 20.0f;
        private const float StarshipSpeed = 30.0f;
        private const float StarshipDistance = 200.0f;
        private const int StarshipCount = 4;
        private const int ExplosionCount = 3;

        private string OptionsMusicOn = "Music On";
        private string OptionsMusicOff = "Music Off";

        private string OptionsSfxOn = "Sfx On";
        private string OptionsSfxOff = "Sfx Off";

        const string FacebookLink = @"http://www.facebook.com/galacticoverlordgame";
        const string TwitterLink = @"http://twitter.com/home?status=I%27m%20playing%20%23GalacticOverlord%20on%20my%20%23WP7%20phone!%20%40GalacticOvrlord";
        private readonly Vector2 FacebookPosition = new Vector2(340.0f, 240.0f);
        private readonly Vector2 TwitterPosition = new Vector2(400.0f, 240.0f);

        #endregion

        #region Private Fields

        private UIState m_uiState;
        private MenuState m_menuState;
        private float m_fadeFactor;
        private float m_demoCountdown;

        private Texture2D m_titleBackgroundTexture;
        private Texture2D m_titleTextTexture;
        private Texture2D m_facebookIconTexture;
        private Texture2D m_twitterIconTexture;
        private SpriteBatch m_spriteBatch;
        private SpriteFont m_spriteFont;

        // fields for explosion animations
        private Vector2 m_explosionCentre;
        private Vector2[] m_explosionPosition;
        private float[] m_explosionLife;
        private Texture2D m_explosionTexture;

        // fields for 3d starship 
        private Random m_random;
        private Matrix[] m_starshipPosition;
        private float[] m_starshipOrientation;
        private float[] m_starshipRotation;
        private DepthStencilState m_depthStencilStateAlpha;
        private DepthStencilState m_depthStencilStateAdditive;
        private SamplerState m_samplerState;
        private Model m_starshipHullModel;
        private Model m_starshipJetModel;

        // ui fields
        private TouchInterface m_touchInterface;
        private DifficultySelector m_difficultySelector;
        private Button m_singlePlayerButton;
        private Button m_multiPlayerButton;
        private Button m_settingsButton;
        private Button m_rateButton;
        private Button m_buyButton;

        private Button m_singlePlayerCampaignButton;
        private Button m_singlePlayerSkirmishButton;
        private Button m_singlePlayerBackButton;

        private Button m_singlePlayerSkirmishDuelButton;
        private Button m_singlePlayerSkirmishThreeWayButton;
        private Button m_singlePlayerSkirmishCloakedButton;
        private Button m_singlePlayerSkirmishAsteroidsButton;
        private Button m_singlePlayerSkirmishBackButton;

        private Button m_settingsMusicButton;
        private Button m_settingsSfxButton;
        private Button m_settingsBackButton;

        private Button m_tappedButton;

        private SoundEffect m_titleLoop;

        #endregion

        private enum UIState
        {
            FadeIn,
            Active,
            FadeOut
        }

        private enum MenuState
        {
            Main,
            SinglePlayer,
            SinglePlayerSkirmish,
            Settings
        }
            
    }
}
