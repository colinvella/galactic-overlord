using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GalacticOverlord.Net;
using Microsoft.Xna.Framework;
using System.IO;
using GalacticOverlord.Core;
using Microsoft.Xna.Framework.Graphics;

namespace GalacticOverlord.Players
{
    public class NetClientPlayer: NetPlayer
    {
        public NetClientPlayer(Channel channel, string remotePlayerId)
            :base(PlayerType.NetClient, Color.YellowGreen, channel, remotePlayerId)
        {
            m_channel = channel;
            m_remotePlayerId = remotePlayerId;
            m_mapPacketReceived = false;
            m_gameStarted = DateTime.Now;
        }

        public override void Update(GameTime gameTime)
        {
            if (!m_mapPacketReceived
                && (DateTime.Now - m_gameStarted).TotalMilliseconds > 5000)
                throw new TimeoutException();

            byte[] message = null;

            message = m_channel.ReceiveMessage(false);
            if (message == null)
                return;

            m_gameStarted = DateTime.Now;

            PlayPacketType playPacketType = (PlayPacketType)message[0];

            switch (playPacketType)
            {
                case PlayPacketType.InitialiseMap:
                    if (!m_mapPacketReceived)
                    {
                        if (DecodeMessageInitialiseMap(message))
                            m_mapPacketReceived = true;
                    }
                    break;
                case PlayPacketType.UpdatePlanets:
                    DecodeMessageUpdatePlanets(message);
                    break;
                case PlayPacketType.DeployFleets:
                    DecodeMessageDeployFleets(message);
                    break;
                case PlayPacketType.ServerVictory:
                    if (m_serverVictoryEventHandler != null)
                        m_serverVictoryEventHandler(this, EventArgs.Empty);
                    break;
                case PlayPacketType.ClientVictory:
                    if (m_clientVictoryEventHandler != null)
                        m_clientVictoryEventHandler(this, EventArgs.Empty);
                    break;
                case PlayPacketType.Disconnect:
                    if (m_remoteDisconnect != null)
                        m_remoteDisconnect(this, EventArgs.Empty);
                    break;
            }
        }

        public override void Draw(GameTime gameTime)
        {
            if (m_spriteBatch == null)
            {
                m_spriteBatch = new SpriteBatch(Space.GalacticOverlordGame.GraphicsDevice);
                m_overlayTexture = Space.Game.Content.Load<Texture2D>(@"Graphics\HeaderOverlay");
                m_spriteFont = Space.GalacticOverlordGame.Content.Load<SpriteFont>(@"Fonts\PlanetFont");
            }

            m_spriteBatch.Begin();

            if (!m_mapPacketReceived)
            {
                Space.GalacticOverlordGame.GraphicsDevice.Clear(Color.Black);
                GraphicsUtility.DrawOutlinedText(m_spriteBatch, m_spriteFont,
                    new Vector2(240, 400), "Connecting...", Color.LightSkyBlue, TextAlignment.Centre);
            }
            else
            {
                m_spriteBatch.Draw(m_overlayTexture, new Vector2(0, -48), Color.White);

                string localPlayerId = Space.GalacticOverlordGame.UserProfile.PlayerId;
                GraphicsUtility.DrawOutlinedText(m_spriteBatch, m_spriteFont, new Vector2(210, 8),
                    localPlayerId, Color.Crimson, TextAlignment.TopRight);
                GraphicsUtility.DrawOutlinedText(m_spriteBatch, m_spriteFont, new Vector2(240, 8),
                    "vs", Color.LightSkyBlue, TextAlignment.Top);
                GraphicsUtility.DrawOutlinedText(m_spriteBatch, m_spriteFont, new Vector2(270, 8),
                    m_remotePlayerId, Color.YellowGreen, TextAlignment.TopLeft);
            }

            m_spriteBatch.End();
        }

        public event EventHandler ServerVictory
        {
            add { m_serverVictoryEventHandler += value; }
            remove { m_serverVictoryEventHandler -= value; }
        }

        public event EventHandler ClientVictory
        {
            add { m_clientVictoryEventHandler += value; }
            remove { m_clientVictoryEventHandler -= value; }
        }

        private bool DecodeMessageInitialiseMap(byte[] message)
        {
            MemoryStream memoryStream = new MemoryStream(message);
            BinaryReader binaryReader = new BinaryReader(memoryStream, Encoding.UTF8);

            // skip packet type
            binaryReader.ReadByte();

            foreach (Planet planet in Space.Planets)
            {
                planet.Position = new Vector2(binaryReader.ReadInt16(), binaryReader.ReadInt16());
                planet.Radius = binaryReader.ReadByte();
                planet.Population = binaryReader.ReadByte();
                planet.Player = null;
            }

            int serverPlayerPlanetIndex = binaryReader.ReadByte();
            int clientPlayerPlanetIndex = binaryReader.ReadByte();

            HumanPlayer humanPlayer = (HumanPlayer)Space.Players.Where(x => x.Type == PlayerType.Human).First();
            Space.GetPlanet(serverPlayerPlanetIndex).Player = this;
            Space.GetPlanet(clientPlayerPlanetIndex).Player = humanPlayer;
            humanPlayer.MarkStartingPlanets();

            return true;
        }

        private void DecodeMessageUpdatePlanets(byte[] message)
        {
            MemoryStream memoryStream = new MemoryStream(message);
            BinaryReader binaryReader = new BinaryReader(memoryStream, Encoding.UTF8);

            // skip packet type
            binaryReader.ReadByte();

            Player humanPlayer = Space.Players.Where(x => x.Type == PlayerType.Human).First();
            foreach (Planet planet in Space.Planets)
            {
                int population = binaryReader.ReadInt16();
                PlayerOwnerType playerOwnerType = (PlayerOwnerType)binaryReader.ReadByte();

                planet.Population = population;

                switch (playerOwnerType)
                {
                    case PlayerOwnerType.None:
                        planet.Player = null;
                        break;
                    case PlayerOwnerType.Client:
                        planet.Player = humanPlayer;
                        break;
                    case PlayerOwnerType.Server:
                        planet.Player = this;
                        break;
                }
            }
        }

        private Channel m_channel;
        private string m_remotePlayerId;
        private bool m_mapPacketReceived;
        private DateTime m_gameStarted;
        private EventHandler m_serverVictoryEventHandler;
        private EventHandler m_clientVictoryEventHandler;

        private SpriteBatch m_spriteBatch;
        private Texture2D m_overlayTexture;
        private SpriteFont m_spriteFont;
    }
}
