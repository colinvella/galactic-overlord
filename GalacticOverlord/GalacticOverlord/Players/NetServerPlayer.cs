using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GalacticOverlord.Net;
using Microsoft.Xna.Framework;
using System.IO;
using GalacticOverlord.Core;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.GamerServices;

namespace GalacticOverlord.Players
{
    public class NetServerPlayer: NetPlayer
    {
        public NetServerPlayer(Channel channel, string remotePlayerId)
            :base(PlayerType.NetServer, Color.Crimson, channel, remotePlayerId)
        {
            m_channel = channel;
            m_remotePlayerId = remotePlayerId;
            m_mapPacketSent = false;
            m_playerEliminatedEventAssigned = false;
            m_planetUpdateCountdown = 0.0;
        }

        public override void Update(GameTime gameTime)
        {
            double deltaTime = gameTime.ElapsedGameTime.TotalSeconds;

            if (!m_playerEliminatedEventAssigned && Space != null)
            {
                Space.PlayerEliminated += OnPlayerEliminated;
                m_playerEliminatedEventAssigned = true;
            }

            if (!m_mapPacketSent)
            {
                SendMessageInitialiseMap();
                m_mapPacketSent = true;
            }

            // periodic planet updates
            m_planetUpdateCountdown -= deltaTime;
            if (m_planetUpdateCountdown < 0.0)
            {
                SendMessageUpdatePlanets();
                m_planetUpdateCountdown += 0.5f;
            }

            byte[] packet = null;

            if (m_channel.IsOpen)
                packet = m_channel.ReceiveMessage(false);

            if (packet == null)
                return;

            PlayPacketType playPacketType = (PlayPacketType)packet[0];

            switch (playPacketType)
            {
                case PlayPacketType.DeployFleets:
                    DecodeMessageDeployFleets(packet);
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
                m_spriteFont = Space.Game.Content.Load<SpriteFont>(@"Fonts\PlanetFont");
            }

            m_spriteBatch.Begin();

            m_spriteBatch.Draw(m_overlayTexture, new Vector2(0, -48), Color.White);

            string localPlayerId = Space.GalacticOverlordGame.UserProfile.PlayerId;
            GraphicsUtility.DrawOutlinedText(m_spriteBatch, m_spriteFont, new Vector2(210, 8),
                localPlayerId, Color.YellowGreen, TextAlignment.TopRight);
            GraphicsUtility.DrawOutlinedText(m_spriteBatch, m_spriteFont, new Vector2(240, 8),
                "vs", Color.LightSkyBlue, TextAlignment.Top);
            GraphicsUtility.DrawOutlinedText(m_spriteBatch, m_spriteFont, new Vector2(270, 8),
                m_remotePlayerId, Color.Crimson, TextAlignment.TopLeft);
            m_spriteBatch.End();
        }

        private void SendMessageInitialiseMap()
        {
            IEnumerable<Planet> planets = Space.Planets;
            MemoryStream memoryStream = new MemoryStream(1 + planets.Count() * 6 + 2);
            BinaryWriter binaryWriter = new BinaryWriter(memoryStream, Encoding.UTF8);
            binaryWriter.Write((byte)PlayPacketType.InitialiseMap);
            int serverPlanetStartIndex = -1, clientPlanetStartIndex = -1;
            int planetIndex = 0;
            foreach (Planet planet in planets)
            {
                binaryWriter.Write((Int16)planet.Position.X);
                binaryWriter.Write((Int16)planet.Position.Y);
                binaryWriter.Write((byte)planet.Radius);
                binaryWriter.Write((byte)planet.Population);
                if (planet.Player != null)
                {
                    if (planet.Player.Type == PlayerType.Human)
                        serverPlanetStartIndex = planetIndex;
                    else if (planet.Player.Type == PlayerType.NetServer)
                        clientPlanetStartIndex = planetIndex;
                }
                ++planetIndex;
            }
            binaryWriter.Write((byte)serverPlanetStartIndex);
            binaryWriter.Write((byte)clientPlanetStartIndex);
            binaryWriter.Flush();

            byte[] message = memoryStream.ToArray();

            m_channel.SendAcknowledgedMessage(message);
        }

        private void SendMessageUpdatePlanets()
        {
            MemoryStream memoryStream = new MemoryStream();
            BinaryWriter binaryWriter = new BinaryWriter(memoryStream, Encoding.UTF8);

            binaryWriter.Write((byte)PlayPacketType.UpdatePlanets);

            foreach (Planet planet in Space.Planets)
            {
                binaryWriter.Write((Int16)planet.Population);

                Player player = planet.Player;
                if (player == null)
                    binaryWriter.Write((byte)PlayerOwnerType.None);
                else if (planet.Player.Type == PlayerType.Human)
                    binaryWriter.Write((byte)PlayerOwnerType.Server);
                else // if (planet.Player.Type == PlayerType.NetServer)
                    binaryWriter.Write((byte)PlayerOwnerType.Client);
            }

            binaryWriter.Flush();

            byte[] message = memoryStream.GetBuffer();
            m_channel.SendMessage(message);
        }

        private void OnPlayerEliminated(object sender, PlayerEventArgs playerEventArgs)
        {
            if (playerEventArgs.Player.Type == PlayerType.Human)
            {
                m_channel.SendAcknowledgedMessage(new byte[] { (byte)PlayPacketType.ClientVictory });
            }
            else
            {
                // other player type
                if (Space.ActivePlayers.Where(x => x.Type != PlayerType.Human).Count() == 0)
                {
                    m_channel.SendAcknowledgedMessage(new byte[] { (byte)PlayPacketType.ServerVictory });
                }
            }
        }

        private Channel m_channel;
        private string m_remotePlayerId;
        private bool m_playerEliminatedEventAssigned;
        private bool m_mapPacketSent;
        private double m_planetUpdateCountdown;

        private SpriteBatch m_spriteBatch;
        private Texture2D m_overlayTexture;
        private SpriteFont m_spriteFont;
    }
}
