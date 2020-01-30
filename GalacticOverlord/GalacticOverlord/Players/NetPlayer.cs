using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GalacticOverlord.Net;
using Microsoft.Xna.Framework;
using System.IO;
using GalacticOverlord.Core;

namespace GalacticOverlord.Players
{
    public abstract class NetPlayer: Player
    {
        public NetPlayer(PlayerType playerType, Color colour, Channel channel, string remotePlayerId)
            :base(playerType, colour, false)
        {
            m_channel = channel;
            m_remotePlayerId = remotePlayerId;
        }

        public override void Update(GameTime gameTime)
        {
        }

        public override void Draw(GameTime gameTime)
        {
        }

        public void RequestDisconnect()
        {
            byte[] message = new byte[] { (byte)PlayPacketType.Disconnect };
            try
            {
                m_channel.SendAcknowledgedMessage(message);
            }
            catch (TimeoutException)
            {
            }
        }

        public event EventHandler RemoteDisconnect
        {
            add { m_remoteDisconnect += value; }
            remove { m_remoteDisconnect -= value; }
        }

        protected void DecodeMessageDeployFleets(byte[] message)
        {
            MemoryStream memoryStream = new MemoryStream(message);
            BinaryReader binaryReader = new BinaryReader(memoryStream, Encoding.UTF8);

            // skip packet type
            binaryReader.ReadByte();

            int sourcePlanetCount = binaryReader.ReadByte();
            List<Planet> sourcePlanets = new List<Planet>(sourcePlanetCount);
            while (sourcePlanetCount-- > 0)
            {
                Int16 prePopulation = binaryReader.ReadInt16();
                int sourcePlanetIndex = binaryReader.ReadByte();
                Planet sourcePlanet = Space.GetPlanet(sourcePlanetIndex);

                // correct population so that fleet size is also correct
                if (Type == PlayerType.NetClient)
                    sourcePlanet.Population = prePopulation;

                sourcePlanets.Add(sourcePlanet);
            }

            int targetPlanetIndex = binaryReader.ReadByte();
            Planet targetPlanet = Space.GetPlanet(targetPlanetIndex);

            int ratioPercentage = binaryReader.ReadByte();
            float ratio = (float)ratioPercentage / 100.0f;

            SendFleets(sourcePlanets, targetPlanet, ratio);
        }

        private Channel m_channel;
        private string m_remotePlayerId;

        protected EventHandler m_remoteDisconnect;
    }

    public enum PlayPacketType
    {
        InitialiseMap,
        UpdatePlanets,
        UpdatePlanet,
        DeployFleets,
        ServerVictory,
        ClientVictory,
        Disconnect
    }

    public enum PlayerOwnerType
    {
        None,
        Client,
        Server
    }
}
