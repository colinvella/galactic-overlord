using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Microsoft.Xna.Framework.GamerServices;

namespace GalacticOverlord.Net
{
    public class HostManager
    {
        public HostManager()
        {
            m_remoteHosts = new Dictionary<int, Host>();
            m_joinedMulticast = false;
            PlayerId = "Player" + new Random().Next(100000, 1000000);
        }

        public void RegisterHost()
        {
            bool registerAttempted = false;
            m_multicastClient = new UdpAnySourceMulticastClient(IPAddress.Parse(MulticastAddress), MulticastPort);
            m_multicastClient.BeginJoinGroup(result =>
                {
                    try
                    {
                        m_multicastClient.EndJoinGroup(result);
                        m_multicastClient.MulticastLoopback = false;
                        m_joinedMulticast = true;

                        m_thread = new Thread(ProcessNetData);
                        m_thread.IsBackground = true;
                        m_thread.Start();
                    }
                    catch
                    {
                    }
                    registerAttempted = true;
                }
                , null);

            while (!registerAttempted)
                Thread.Sleep(10);
        }

        public void UnregisterHost()
        {
            m_joinedMulticast = false;
            while (m_thread.IsAlive)
                Thread.Sleep(10);
            m_multicastClient.Dispose();
            m_multicastClient = null;

            m_remoteHosts.Clear();
            m_challengedHost = null;
        }

        public void SendPlayRequest(Host remoteHost)
        {
            bool validHost = false;
            lock (m_remoteHosts)
            {
                validHost = m_remoteHosts.ContainsKey(remoteHost.Id);
            }

            if (!validHost)
                return;

            // prepare play request packet
            byte[] remotePlayerIdBytes = Encoding.UTF8.GetBytes(remoteHost.PlayerId);
            byte[] playRequestPacket = new byte[ProtocolId.Length + 1 + remotePlayerIdBytes.Length + 1];
            Array.Copy(ProtocolId, playRequestPacket, ProtocolId.Length);
            playRequestPacket[ProtocolId.Length] = PT_PlayRequest;
            playRequestPacket[ProtocolId.Length + 1] = (byte)remotePlayerIdBytes.Length;
            Array.Copy(remotePlayerIdBytes, 0, playRequestPacket, ProtocolId.Length + 2, remotePlayerIdBytes.Length);

            // send packet
            BroadcastPacket(playRequestPacket);

            m_challengedHost = remoteHost;
        }

        public bool Connected
        {
            get { return m_joinedMulticast; }
        }

        public string PlayerId
        {
            get { return m_playerId; }
            set
            {
                m_playerId = value;

                // prepare host advertising packet
                byte[] playerIdBytes = Encoding.UTF8.GetBytes(m_playerId);
                m_advertiseHostPacket = new byte[ProtocolId.Length + 1 + playerIdBytes.Length + 1];
                Array.Copy(ProtocolId, m_advertiseHostPacket, ProtocolId.Length);
                m_advertiseHostPacket[ProtocolId.Length] = PT_AdvertiseHost;
                m_advertiseHostPacket[ProtocolId.Length + 1] = (byte)playerIdBytes.Length;
                Array.Copy(playerIdBytes, 0, m_advertiseHostPacket, ProtocolId.Length + 2, playerIdBytes.Length);
            }
        }

        public event EventHandler<HostEventArgs> StartServerGame
        {
            add { m_startServerGame += value; }
            remove { m_startServerGame -= value; }
        }

        public event EventHandler<HostEventArgs> StartClientGame
        {
            add { m_startClientGame += value; }
            remove { m_startClientGame -= value; }
        }

        private void BroadcastPacket(byte[] packet)
        {
            m_multicastClient.BeginSendToGroup(packet, 0, packet.Length,
                result =>
                {
                    m_multicastClient.EndSendToGroup(result);
                }
                , null);
        }

        private void ProcessNetData()
        {
            while (m_joinedMulticast)
            {
                try
                {
                    // advertise host
                    BroadcastPacket(m_advertiseHostPacket);

                    // listen for other hosts
                    byte[] incomingBuffer = new byte[64];
                    m_multicastClient.BeginReceiveFromGroup(incomingBuffer, 0, incomingBuffer.Length,
                        result =>
                        {
                            IPEndPoint ipEndPoint = null;

                            if (m_multicastClient == null)
                                return;

                            m_multicastClient.EndReceiveFromGroup(result, out ipEndPoint);

                            if (incomingBuffer[0] == ProtocolId[0]
                                && incomingBuffer[1] == ProtocolId[1])
                            {
                                byte packetType = incomingBuffer[ProtocolId.Length];

                                if (packetType == PT_AdvertiseHost)
                                {
                                    int remotePlayerIdLength = incomingBuffer[ProtocolId.Length + 1];
                                    byte[] remotePlayerIdBytes = new byte[remotePlayerIdLength];
                                    string remotePlayerId = Encoding.UTF8.GetString(incomingBuffer, ProtocolId.Length + 2, remotePlayerIdLength);

                                    Host remoteHost = new Host(ipEndPoint, remotePlayerId);

                                    lock (m_remoteHosts)
                                    {
                                        m_remoteHosts[remoteHost.Id] = remoteHost;
                                    }
                                }
                                else if (packetType == PT_PlayRequest || packetType == PT_PlayAcknowledge)
                                {
                                    // get player id within packet
                                    int packetPlayerIdLength = incomingBuffer[ProtocolId.Length + 1];
                                    byte[] packetPlayerIdBytes = new byte[packetPlayerIdLength];
                                    string packetPlayerId = Encoding.UTF8.GetString(incomingBuffer, ProtocolId.Length + 2, packetPlayerIdLength);

                                    if (packetType == PT_PlayRequest && packetPlayerId == m_playerId)
                                    {
                                        // prepare play ack packet
                                        byte[] playerIdBytes = Encoding.UTF8.GetBytes(m_playerId);
                                        byte[] playAckPacket = new byte[ProtocolId.Length + 1 + playerIdBytes.Length + 1];
                                        Array.Copy(ProtocolId, playAckPacket, ProtocolId.Length);
                                        playAckPacket[ProtocolId.Length] = PT_PlayAcknowledge;
                                        playAckPacket[ProtocolId.Length + 1] = (byte)playerIdBytes.Length;
                                        Array.Copy(playerIdBytes, 0, playAckPacket, ProtocolId.Length + 2, playerIdBytes.Length);

                                        // send ack
                                        BroadcastPacket(playAckPacket);

                                        // start client side net game
                                        if (m_startClientGame != null)
                                        {
                                            Host remoteHost = null;
                                            lock (m_remoteHosts)
                                            {
                                                remoteHost = m_remoteHosts.Values.Where(
                                                    x => x.IPEndPoint.ToString() == ipEndPoint.ToString()).First();
                                            }
                                            m_startClientGame(this, new HostEventArgs(remoteHost));
                                        }
                                    }
                                    else if (packetType == PT_PlayAcknowledge && m_challengedHost != null
                                        && m_challengedHost.IPEndPoint.ToString() == ipEndPoint.ToString())
                                    {
                                        // challenge accepted by remote host and acknowledged back
                                        // can start server side net game
                                        if (m_startServerGame != null)
                                            m_startServerGame(this, new HostEventArgs(m_challengedHost));
                                    }
                                }
                            }
                        }
                        , null);

                    // remove stale hosts
                    lock (m_remoteHosts)
                    {
                        IEnumerable<int> staleIds = m_remoteHosts.Where(x => x.Value.BroadcastAge > 2.0).Select(x => x.Key).ToArray();
                        foreach (int staleId in staleIds)
                            m_remoteHosts.Remove(staleId);
                    }
                }
                catch (InvalidOperationException)
                {
                    m_joinedMulticast = false;
                }

                Thread.Sleep(100);
            }

        }

        public IEnumerable<Host> RemoteHosts
        {
            get
            {
                IEnumerable<Host> remoteHosts = null;
                lock (m_remoteHosts)
                {
                    remoteHosts = m_remoteHosts.Values.ToArray();
                }
                return remoteHosts;
            }
        }

        public const int GameSessionPort = 52275;

        private const string MulticastAddress = "239.5.6.7";
        private const int MulticastPort = 52274;

        private readonly byte[] ProtocolId = new byte[] { 0x13, 0x37};
        private const byte PT_AdvertiseHost = 0;
        private const byte PT_PlayRequest = 1;
        private const byte PT_PlayAcknowledge = 2;

        private UdpAnySourceMulticastClient m_multicastClient;
        private Thread m_thread;
        private bool m_joinedMulticast;
        private string m_playerId;
        private byte[] m_advertiseHostPacket;
        private Dictionary<int, Host> m_remoteHosts;
        private Host m_challengedHost;

        private EventHandler<HostEventArgs> m_startServerGame;
        private EventHandler<HostEventArgs> m_startClientGame;
    }
}
