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
using GalacticOverlord.Net;
using System.Threading;
using Microsoft.Xna.Framework.GamerServices;

namespace GalacticOverlord.GameStates
{
    public class ClientGameState: GameState
    {
        public ClientGameState(GalacticOverlordGame galacticOverlordGame, Channel channel, string remotePlayerId)
            : base(galacticOverlordGame)
        {
            m_channel = channel;
            m_remotePlayerId = remotePlayerId;
        }

        public override void Initialize()
        {
            m_space = new Space(GalacticOverlordGame);

            m_humanPlayer = new HumanPlayer(Color.Crimson, m_channel);

            m_netClientPlayer = new NetClientPlayer(m_channel, m_remotePlayerId);
            m_netClientPlayer.ServerVictory += OnServerVictory;
            m_netClientPlayer.ClientVictory += OnClientVictory;
            m_netClientPlayer.RemoteDisconnect += OnRemoteDisconnect;

            MapGenerator.GenerateDuelMap(m_space, m_humanPlayer, m_netClientPlayer);
            m_space.BackupConfiguration();
            ChildComponents.Add(m_space);

            m_screenOverlay = new ScreenOverlay(GalacticOverlordGame);
            m_screenOverlay.Visible = false;
            ChildComponents.Add(m_screenOverlay);

            m_touchInterface = new TouchInterface(GalacticOverlordGame);
            ChildComponents.Add(m_touchInterface);

            m_continueButton = new Button(m_touchInterface, new Vector2(140.0f, 480.0f), 200.0f, "Continue");
            m_continueButton.Tapped += OnTappedContinue;
            ChildComponents.Add(m_continueButton);

            m_disconnectButton = new Button(m_touchInterface, new Vector2(140.0f, 560.0f), 200.0f, "Disconnect");
            m_disconnectButton.Tapped += OnTappedDisconnect;
            ChildComponents.Add(m_disconnectButton);

            TouchPanel.EnabledGestures = GestureType.Tap /*| GestureType.DoubleTap*/
                | GestureType.FreeDrag | GestureType.DragComplete;

            // hook for thombstoning
            Game.Deactivated += OnGameDeactivated;

            base.Initialize();
        }

        public override void Update(GameTime gameTime)
        {
            try
            {
                if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
                {
                    if (m_continueButton.PannedOut && m_disconnectButton.PannedOut)
                    {
                        m_humanPlayer.InputEnabled = false;

                        m_screenOverlay.ClearText();
                        m_screenOverlay.AddText(m_headerFont, new Vector2(240, 300), Color.LightCyan, "Net Game Options");
                        m_screenOverlay.Visible = true;

                        m_continueButton.PanIn();
                        m_disconnectButton.PanIn();
                    }
                    else
                    {
                        OnTappedDisconnect(this, EventArgs.Empty);
                    }
                }

                base.Update(gameTime);
            }
            catch (TimeoutException)
            {
                m_humanPlayer.InputEnabled = false;

                m_screenOverlay.ClearText();
                m_screenOverlay.AddText(m_headerFont, new Vector2(240, 320), Color.Red,
                    "Network error");
                m_screenOverlay.Visible = true;

                if (!m_continueButton.PannedOut)
                    m_continueButton.PanOut();

                m_disconnectButton.Text = "OK";
                m_disconnectButton.PanIn();
            }
        }

        public override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);
        }

        public override void Shutdown()
        {
            // hook for thombstoning
            Game.Deactivated -= OnGameDeactivated;

            GalacticOverlordGame.AudioManager.StopMusic();

            base.Shutdown();
        }

        protected override void LoadContent()
        {
            ContentManager contentManager = Game.Content;

            m_headerFont = contentManager.Load<SpriteFont>(@"Fonts\HeaderFont");
            m_planetFont = contentManager.Load<SpriteFont>(@"Fonts\PlanetFont");

            m_playMusic = contentManager.Load<SoundEffect>(@"Audio\PlayLoop");
            m_successMusic = contentManager.Load<SoundEffect>(@"Audio\Success");
            GalacticOverlordGame.AudioManager.PlayMusic(m_playMusic);

            base.LoadContent();
        }

        private void OnServerVictory(object sender, EventArgs eventArgs)
        {
            // local client human player eliminated
            m_humanPlayer.InputEnabled = false;

            m_screenOverlay.ClearText();
            m_screenOverlay.AddText(m_headerFont, new Vector2(240, 320), Color.LightCyan, "Failure!");
            m_screenOverlay.Visible = true;

            if (!m_continueButton.PannedOut)
                m_continueButton.PanOut();

            m_disconnectButton.PanIn();
        }

        private void OnClientVictory(object sender, EventArgs eventArgs)
        {
            // remote server player eliminated
            m_humanPlayer.InputEnabled = false;

            m_screenOverlay.ClearText();
            m_screenOverlay.AddText(m_headerFont, new Vector2(240, 320), Color.LightCyan, "Victory!");
            m_screenOverlay.Visible = true;

            if (!m_continueButton.PannedOut)
                m_continueButton.PanOut();

            m_disconnectButton.PanIn();

            // play success music
            GalacticOverlordGame.AudioManager.PlayMusic(m_successMusic);

            // update profile stats
            UserProfile userProfile = GalacticOverlordGame.UserProfile;
            ++userProfile.MultiplayerVictories;
            userProfile.StoreProperties();
        }

        private void OnTappedContinue(object sender, EventArgs eventArgs)
        {
            m_humanPlayer.InputEnabled = true;

            m_screenOverlay.Visible = false;
            m_continueButton.PanOut();
            m_disconnectButton.PanOut();
        }

        private void OnTappedDisconnect(object sender, EventArgs eventArgs)
        {
            if (m_channel.IsOpen)
            {
                m_netClientPlayer.RequestDisconnect();
                Thread.Sleep(1000);
                m_channel.Close();
            }

            SwitchToState(new HostGameState(GalacticOverlordGame));
        }

        private void OnRemoteDisconnect(object sender, EventArgs eventArgs)
        {
            // remote server has disconnected
            m_humanPlayer.InputEnabled = false;

            m_screenOverlay.ClearText();
            m_screenOverlay.AddText(m_headerFont, new Vector2(240, 320), Color.YellowGreen,
                m_remotePlayerId );
            m_screenOverlay.AddText(m_headerFont, new Vector2(240, 360), Color.LightCyan,
                "has left the game");
            m_screenOverlay.Visible = true;

            if (!m_continueButton.PannedOut)
                m_continueButton.PanOut();

            m_disconnectButton.Text = "OK";
            m_disconnectButton.PanIn();
        }

        private void OnGameDeactivated(object sender, EventArgs eventArgs)
        {
            if (m_channel.IsOpen)
            {
                m_netClientPlayer.RequestDisconnect();
                Thread.Sleep(1000);
                m_channel.Close();
            }

            SwitchToState(new TitleGameState(GalacticOverlordGame));
        }

        private Channel m_channel;
        private string m_remotePlayerId;
        private HumanPlayer m_humanPlayer;
        private NetClientPlayer m_netClientPlayer;
        private Space m_space;
        private ScreenOverlay m_screenOverlay;
        private TouchInterface m_touchInterface;
        private Button m_continueButton;
        private Button m_disconnectButton;

        private SpriteFont m_headerFont;
        private SpriteFont m_planetFont;
        private SoundEffect m_playMusic;
        private SoundEffect m_successMusic;
    }
}
