using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Input.Touch;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Input;
using GalacticOverlord.UI;
using GalacticOverlord.Players;
using GalacticOverlord.Core;
using GalacticOverlord.Pipeline;

namespace GalacticOverlord.GameStates
{
    public class PlayGameState: GameState
    {
        public PlayGameState(GalacticOverlordGame galacticOverlordGame, CampaignDefinition campaignDefinition, int campaignLevelIndex)
            : base(galacticOverlordGame)
        {
            m_campaignMode = true;
            m_campaignDefinition = campaignDefinition;
            m_campaignLevelIndex = campaignLevelIndex;
            m_cutsceneActive = true;
        }

        public PlayGameState(GalacticOverlordGame galacticOverlordGame, SkirmishMode skirmishMode, DifficultyLevel difficultyLevel)
            : base(galacticOverlordGame)
        {
            m_campaignMode = false;
            m_campaignDefinition = null;
            m_campaignLevelIndex = -1;
            m_cutsceneActive = false;

            m_skirmishMode = skirmishMode;
            m_difficultyLevel = difficultyLevel;
        }

        public override void Initialize()
        {
            m_space = new Space(GalacticOverlordGame);
            m_humanPlayer = new HumanPlayer(Color.YellowGreen);

            InitialiseSpace();
            m_space.PlayerEliminated += OnPlayerEliminated;
            ChildComponents.Add(m_space);

            m_touchInterface = new TouchInterface(GalacticOverlordGame);
            ChildComponents.Add(m_touchInterface);

            if (m_cutsceneActive)
            {
                m_cutscenePanel = new CutscenePanel(m_touchInterface,
                    m_campaignDefinition.Levels[m_campaignLevelIndex].Cutscene);
                ChildComponents.Add(m_cutscenePanel);
                m_space.Enabled = false;
            }

            m_screenOverlay = new ScreenOverlay(GalacticOverlordGame);
            m_screenOverlay.Visible = false;
            ChildComponents.Add(m_screenOverlay);

            m_retryButton = new Button(m_touchInterface, new Vector2(140.0f, 400.0f), 200.0f, "Retry");
            m_retryButton.Tapped += OnTappedRetry;
            ChildComponents.Add(m_retryButton);

            m_newMapButton = new Button(m_touchInterface, new Vector2(140.0f, 480.0f), 200.0f, "New Map");
            m_newMapButton.Tapped += OnTappedNewMap;
            ChildComponents.Add(m_newMapButton);

            m_continueButton = new Button(m_touchInterface, new Vector2(140.0f, 480.0f), 200.0f, "Continue");
            m_continueButton.Tapped += OnTappedContinue;
            ChildComponents.Add(m_continueButton);

            m_nextBattleButton = new Button(m_touchInterface, new Vector2(140.0f, 480.0f), 200.0f, "Next Battle");
            m_nextBattleButton.Tapped += OnTappedNextBattle;
            ChildComponents.Add(m_nextBattleButton);

            m_menuButton = new Button(m_touchInterface, new Vector2(140.0f, 560.0f), 200.0f, "Menu");
            m_menuButton.Tapped += OnTappedMenu;
            ChildComponents.Add(m_menuButton);

            TouchPanel.EnabledGestures = GestureType.Tap /*| GestureType.DoubleTap*/
                | GestureType.FreeDrag | GestureType.DragComplete;

            base.Initialize();
        }

        public override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
            {
                if (m_retryButton.PannedOut && m_continueButton.PannedOut && m_menuButton.PannedOut)
                {
                    if (m_cutsceneActive)
                    {
                        m_cutscenePanel.Enabled = false;
                    }
                    else
                    {
                        m_humanPlayer.InputEnabled = false;
                        m_space.Enabled = false;
                    }

                    m_screenOverlay.ClearText();
                    m_screenOverlay.AddText(m_headerFont, new Vector2(240, 300), Color.LightCyan, "Game Paused");
                    m_screenOverlay.Visible = true;

                    m_retryButton.PanIn();
                    m_continueButton.PanIn();
                    m_menuButton.PanIn();
                }
                else // back from pause menu
                {
                    if (m_campaignMode)
                        SwitchToState(new CampaignGameState(GalacticOverlordGame));
                    else
                        SwitchToState(new TitleGameState(GalacticOverlordGame));
                }
            }

            // enable playing if cutscene finished
            if (m_campaignMode && m_cutsceneActive && m_cutscenePanel.NarrativeComplete)
            {
                m_cutsceneActive = false;
                ChildComponents.Remove(m_cutscenePanel);
                m_space.Enabled = true;
                m_humanPlayer.InputEnabled = true;
            }

            base.Update(gameTime);
        }

        public override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);
        }

        public override void Shutdown()
        {
            GalacticOverlordGame.AudioManager.StopMusic();

            base.Shutdown();
        }

        protected override void LoadContent()
        {
            ContentManager contentManager = Game.Content;

            m_headerFont = contentManager.Load<SpriteFont>(@"Fonts\HeaderFont");

            m_playMusic = contentManager.Load<SoundEffect>(@"Audio\PlayLoop");
            m_successMusic = contentManager.Load<SoundEffect>(@"Audio\Success");
            GalacticOverlordGame.AudioManager.PlayMusic(m_playMusic);

            base.LoadContent();
        }

        private void InitialiseSpace()
        {
            if (m_campaignMode)
            {
                MapGenerator.GenerateCampaignMap(m_space, m_humanPlayer, m_campaignDefinition, m_campaignLevelIndex);
                m_humanPlayer.InputEnabled = false;
            }
            else
            {
                switch (m_skirmishMode)
                {
                    case SkirmishMode.Duel:
                        {
                            Player computerPlayer = new ComputerPlayer(m_difficultyLevel, Color.Crimson, false);
                            MapGenerator.GenerateDuelMap(m_space, m_humanPlayer, computerPlayer);
                        }
                        break;
                    case SkirmishMode.ThreeWay:
                        {
                            Player computerPlayerOne = new ComputerPlayer(m_difficultyLevel, Color.Crimson, false);
                            Player computerPlayerTwo = new ComputerPlayer(m_difficultyLevel, Color.DeepSkyBlue, false);
                            MapGenerator.GenerateThreeWayMap(m_space,
                                m_humanPlayer, computerPlayerOne, computerPlayerTwo);
                        }
                        break;
                    case SkirmishMode.Cloaked:
                        {
                            Player computerPlayer = new ComputerPlayer(m_difficultyLevel, Color.Indigo, true);
                            MapGenerator.GenerateDuelMap(m_space, m_humanPlayer, computerPlayer);
                        }
                        break;
                    case SkirmishMode.Asteroids:
                        {
                            Player computerPlayer = new ComputerPlayer(m_difficultyLevel, Color.Crimson, false);
                            MapGenerator.GenerateAsteroidsMap(m_space,
                                m_humanPlayer, computerPlayer);
                        }
                        break;
                }
            }

            m_space.BackupConfiguration();
            m_humanPlayer.MarkStartingPlanets();
        }

        private void OnPlayerEliminated(object sender, PlayerEventArgs playerEventArgs)
        {
            if (playerEventArgs.Player.Type == PlayerType.Human)
            {
                // human player eliminated
                m_humanPlayer.InputEnabled = false;

                m_screenOverlay.ClearText();
                m_screenOverlay.AddText(m_headerFont, new Vector2(240, 320), Color.LightCyan, "Failure!");
                m_screenOverlay.Visible = true;

                m_retryButton.PanIn();
                if (!m_campaignMode)
                    m_newMapButton.PanIn();
                m_menuButton.PanIn();
            }
            else
            {
                // other player type
                if (m_space.ActivePlayers.Where(x => x.Type != PlayerType.Human).Count() == 0)
                {
                    // all enemy players eliminated
                    m_humanPlayer.InputEnabled = false;

                    m_screenOverlay.ClearText();
                    m_screenOverlay.AddText(m_headerFont, new Vector2(240, 320), Color.LightCyan, "Victory!");
                    m_screenOverlay.Visible = true;

                    m_retryButton.PanIn();
                    if (m_campaignMode)
                        m_nextBattleButton.PanIn();
                    else
                        m_newMapButton.PanIn();
                    m_menuButton.PanIn();

                    // play success music
                    GalacticOverlordGame.AudioManager.PlayMusic(m_successMusic);

                    // update profile data
                    UserProfile userProfile = GalacticOverlordGame.UserProfile;
                    if (m_campaignMode)
                    {
                        // campaign progress
                        if (m_campaignLevelIndex <= userProfile.CampaignProgress)
                        {
                            ++userProfile.CampaignProgress;
                            userProfile.StoreProperties();
                        }
                    }
                    else
                    {
                        // skirmish stats
                        userProfile.AddSkirmishVictory(m_skirmishMode, m_difficultyLevel);
                        userProfile.StoreProperties();
                    }
                }
            }
        }

        private void OnTappedRetry(object sender, EventArgs eventArgs)
        {
            if (m_cutsceneActive)
            {
                m_cutscenePanel.Reset();
                m_cutscenePanel.Enabled = true;
            }
            else
            {
                m_space.RestoreConfiguration();
                m_humanPlayer.MarkStartingPlanets();
                m_humanPlayer.InputEnabled = true;
                m_space.Enabled = true;
            }

            m_screenOverlay.Visible = false;
            m_retryButton.PanOut();
            m_newMapButton.PanOut();
            m_continueButton.PanOut();
            m_nextBattleButton.PanOut();
            m_menuButton.PanOut();

            GalacticOverlordGame.AudioManager.PlayMusic(m_playMusic);
        }

        private void OnTappedContinue(object sender, EventArgs eventArgs)
        {
            if (m_cutsceneActive)
            {
                m_cutscenePanel.Enabled = true;
            }
            else
            {
                m_humanPlayer.InputEnabled = true;
                m_space.Enabled = true;
            }

            m_screenOverlay.Visible = false;
            m_retryButton.PanOut();
            m_continueButton.PanOut();
            m_menuButton.PanOut();
        }

        private void OnTappedNewMap(object sender, EventArgs eventArgs)
        {
            InitialiseSpace();
            m_humanPlayer.InputEnabled = true;
            m_space.Enabled = true;

            m_screenOverlay.Visible = false;
            m_retryButton.PanOut();
            m_newMapButton.PanOut();
            m_continueButton.PanOut();
            m_menuButton.PanOut();

            GalacticOverlordGame.AudioManager.PlayMusic(m_playMusic);
        }

        private void OnTappedNextBattle(object sender, EventArgs eventArgs)
        {
            int nextLevelIndex = m_campaignLevelIndex + 1;
            if (nextLevelIndex < m_campaignDefinition.Levels.Length)
                SwitchToState(new PlayGameState(
                    GalacticOverlordGame, m_campaignDefinition, nextLevelIndex));
        }

        private void OnTappedMenu(object sender, EventArgs eventArgs)
        {
            if (m_campaignMode)
                SwitchToState(new  CampaignGameState(GalacticOverlordGame));
            else
                SwitchToState(new TitleGameState(GalacticOverlordGame));
        }

        private bool m_campaignMode;

        private CampaignDefinition m_campaignDefinition;
        private int m_campaignLevelIndex;
        private CutscenePanel m_cutscenePanel;
        private bool m_cutsceneActive;

        private SkirmishMode m_skirmishMode;
        private DifficultyLevel m_difficultyLevel;
        private HumanPlayer m_humanPlayer;
        private Space m_space;
        private ScreenOverlay m_screenOverlay;
        private TouchInterface m_touchInterface;
        private Button m_retryButton;
        private Button m_newMapButton;
        private Button m_continueButton;
        private Button m_nextBattleButton;
        private Button m_menuButton;

        private SpriteFont m_headerFont;
        private SoundEffect m_playMusic;
        private SoundEffect m_successMusic;
    }

    public enum SkirmishMode
    {
        Duel,
        ThreeWay,
        Cloaked,
        Asteroids
    }
}
