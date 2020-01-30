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

namespace GalacticOverlord.GameStates
{
    public class HostGameState: GameState
    {
        #region Public Methods

        public HostGameState(GalacticOverlordGame galacticOverlordGame)
            : base(galacticOverlordGame)
        {
            m_hostManager = new HostManager();
            m_hostManager.PlayerId = GalacticOverlordGame.UserProfile.PlayerId;
            m_hostManager.StartServerGame += OnStartServerGame;
            m_hostManager.StartClientGame += OnStartClientGame;
        }

        public override void Initialize()
        {
            m_hostManager.RegisterHost();
            m_connected = m_hostManager.Connected;

            m_uiState = UIState.FadeIn;
            m_fadeFactor = 1.0;

            m_touchInterface = new TouchInterface(GalacticOverlordGame);
            ChildComponents.Add(m_touchInterface);

            m_playerIdButton = new Button(m_touchInterface, new Vector2(160, 150), 200.0f, "Change");
            m_playerIdButton.PanIn();
            m_playerIdButton.Visible = true;
            m_playerIdButton.Tapped += OnChangePlayerId;
            ChildComponents.Add(m_playerIdButton);

            m_playButton = new Button(m_touchInterface, new Vector2(20, 600), 200.0f, "Play");
            m_playButton.PanIn();
            m_playButton.Visible = true;
            ChildComponents.Add(m_playButton);

            m_backButton = new Button(m_touchInterface, new Vector2(260, 600), 200.0f, "Back");
            m_backButton.PanIn();
            m_backButton.Visible = true;
            m_backButton.Tapped += OnTappedBack;
            ChildComponents.Add(m_backButton);

            m_selectedHostIndex = -1;

            m_spriteBatch = new SpriteBatch(Game.GraphicsDevice);
            base.Initialize();
        }

        public override void Update(GameTime gameTime)
        {
            if (!m_connected)
            {
                Guide.BeginShowMessageBox(
                    "Multiplayer", "Unable to access the network. Please make sure that your Wifi is enabled and connected",
                    new string[] { "OK" }, 0, MessageBoxIcon.Error, null, null);
                SwitchToState(new TitleGameState(GalacticOverlordGame));
            }

            if (!m_hostManager.Connected)
                OnTappedBack(this, EventArgs.Empty);

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

            // host selection
            if (m_selectedHostIndex > m_hostManager.RemoteHosts.Count())
                m_selectedHostIndex = -1;

            if (m_selectedHostIndex < 0)
                m_playButton.Tapped -= OnTappedPlay;

            foreach (GestureSample gestureSample in m_touchInterface.GestureSamples)
            {
                if (gestureSample.GestureType != GestureType.Tap)
                    continue;

                float tapY = gestureSample.Position.Y;
                if (tapY < 300.0f || tapY >= 300.0f + 240.0f)
                    continue;

                int tappedTow = (int)((tapY - 300.0f) / 24.0f);
                m_selectedHostIndex = -1;
                if (tappedTow < m_hostManager.RemoteHosts.Count())
                {
                    m_selectedHostIndex = tappedTow;
                    m_playButton.Tapped += OnTappedPlay;
                }
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
                new Vector2(textX, 20), "Multiplayer",
                Color.LightCyan, TextAlignment.TopLeft);

            GraphicsUtility.DrawOutlinedText(m_spriteBatch, m_headerFont,
                new Vector2(textX, 100), "Net ID", Color.LightSkyBlue, TextAlignment.TopLeft);

            GraphicsUtility.DrawOutlinedText(m_spriteBatch, m_headerFont,
                new Vector2(160 + (float)m_fadeFactor * 480.0f, 100),
                GalacticOverlordGame.UserProfile.PlayerId, Color.Yellow, TextAlignment.TopLeft);

            GraphicsUtility.DrawOutlinedText(m_spriteBatch, m_smallFont, new Vector2(textX, 260),
                m_hostManager.RemoteHosts.Count() == 0
                ? "Scanning local network for players..." : "Select a player and tap Play",
                Color.LightSkyBlue, TextAlignment.TopLeft);

            Vector2 hostPosition = new Vector2(textX, 300);
            int hostCount = 0;
            foreach (Host remoteHost in m_hostManager.RemoteHosts)
            {
                string playerDetails = remoteHost.PlayerId + " (" + remoteHost.IPAddress + ")";
                Color colour = hostCount == m_selectedHostIndex ? Color.LightCyan : Color.DarkCyan;
                GraphicsUtility.DrawOutlinedText(m_spriteBatch, m_smallFont, hostPosition, playerDetails, colour, TextAlignment.TopLeft);
                hostPosition.Y += 24.0f;
                ++hostCount;
                if (hostCount > 10)
                    break;
            }

            m_spriteBatch.End();

            base.Draw(gameTime);
        }

        public override void Shutdown()
        {
            if (m_hostManager.Connected)
                m_hostManager.UnregisterHost();

            base.Shutdown();
        }

        #endregion

        #region Protected Methods

        protected override void LoadContent()
        {
            ContentManager contentManager = Game.Content;
            m_titleBackgroundTexture = contentManager.Load<Texture2D>(@"Graphics\TitleBackground");
            m_multiplayerBackgroundTexture = contentManager.Load<Texture2D>(@"Graphics\MultiplayerBackground");
            m_headerFont = contentManager.Load<SpriteFont>(@"Fonts\HeaderFont");
            m_smallFont = contentManager.Load<SpriteFont>(@"Fonts\PlanetFont");

            base.LoadContent();
        }

        #endregion

        #region Private Methods

        private void OnChangePlayerId(object sender, EventArgs eventArgs)
        {
            Guide.BeginShowKeyboardInput(PlayerIndex.One, "Change Net ID", "Set or change your name as seen by other online players", GalacticOverlordGame.UserProfile.PlayerId,
                result =>
                {
                    string newPlayerId = Guide.EndShowKeyboardInput(result);
                    if (newPlayerId == null)
                        return;
                    newPlayerId = newPlayerId.Trim();
                    if (newPlayerId.Length == 0)
                        return;

                    if (newPlayerId.Length > 12)
                        newPlayerId = newPlayerId.Substring(0, 12);

                    GalacticOverlordGame.UserProfile.PlayerId = newPlayerId;
                    GalacticOverlordGame.UserProfile.StoreProperties();
                    m_hostManager.PlayerId = newPlayerId;
                }, null);
        }

        private void OnTappedPlay(object sender, EventArgs eventArgs)
        {
            Host[] remoteHosts = m_hostManager.RemoteHosts.ToArray();
            if (m_selectedHostIndex >= 0 && m_selectedHostIndex < remoteHosts.Count())
            {
                Host remoteHost = remoteHosts[m_selectedHostIndex];
                m_hostManager.SendPlayRequest(remoteHost);
            }
        }

        private void OnTappedBack(object sender, EventArgs eventArgs)
        {
            m_playerIdButton.PanOut();
            m_playButton.PanOut();
            m_backButton.PanOut();
            m_tappedButton = m_backButton;
            m_uiState = UIState.FadeOut;
        }

        private void OnStartServerGame(object sender, HostEventArgs hostEventArgs)
        {
            Host remoteHost = hostEventArgs.RemoteHost;

            try
            {
                IPEndPoint ipEndPoint = new IPEndPoint(
                    remoteHost.IPEndPoint.Address, HostManager.GameSessionPort);
                Channel channel = new Channel(ipEndPoint);

                SwitchToState(new ServerGameState(GalacticOverlordGame, channel, remoteHost.PlayerId));
            }
            catch
            {
                Guide.BeginShowMessageBox("Multiplayer", "A network error has occured", new string[] { "OK" }, 0, MessageBoxIcon.Error, null, null);
            }
        }

        private void OnStartClientGame(object sender, HostEventArgs hostEventArgs)
        {
            Host remoteHost = hostEventArgs.RemoteHost;

            try
            {
                IPEndPoint ipEndPoint = new IPEndPoint(
                    remoteHost.IPEndPoint.Address, HostManager.GameSessionPort);
                Channel channel = new Channel(ipEndPoint);

                SwitchToState(new ClientGameState(GalacticOverlordGame, channel, remoteHost.PlayerId));
            }
            catch
            {
                Guide.BeginShowMessageBox("Multiplayer", "A network error has occured", new string[] { "OK" }, 0, MessageBoxIcon.Error, null, null);
            }
        }

        #endregion

        #region Private Constants

        #endregion

        #region Private Fields

        private HostManager m_hostManager;
        private bool m_connected;

        private UIState m_uiState;
        private double m_fadeFactor;

        private TouchInterface m_touchInterface;
        private Button m_playerIdButton;
        private Button m_playButton;
        private Button m_backButton;
        private Button m_tappedButton;

        private Texture2D m_titleBackgroundTexture;
        private Texture2D m_multiplayerBackgroundTexture;
        private SpriteBatch m_spriteBatch;
        private SpriteFont m_headerFont;
        private SpriteFont m_smallFont;
        private int m_selectedHostIndex;

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
