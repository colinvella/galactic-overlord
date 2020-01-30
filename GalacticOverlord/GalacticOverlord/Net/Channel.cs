using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Threading;

namespace GalacticOverlord.Net
{
    public class Channel
    {
        public Channel(IPEndPoint ipEndPoint)
        {
            m_ipEndPoint = ipEndPoint;
            m_joined = false;
            m_closeRequested = false;
            m_outgoingPackets = new List<byte[]>();
            m_incomingMessages = new List<byte[]>();
            m_timeout = 5000;

            m_localLoSequence = 0x00;
            m_localHiSequence = 0x00;
            m_remoteLoSequence = 0xFF;
            m_remoteHiSequence = 0xFF;
            m_lastAcknowledged = 0xFF;

            m_udpClient = new UdpAnySourceMulticastClient(IPAddress.Parse(MulticastAddress), MulticastPort);

            m_joined = false;
            m_udpClient.BeginJoinGroup(
                result =>
                {
                    m_udpClient.EndJoinGroup(result);
                    m_udpClient.MulticastLoopback = false;
                    m_joined = true;
                },
                null);

            while (!m_joined)
                Thread.Sleep(0);

            m_sendThread = new Thread(SendingProcess);
            m_sendThread.IsBackground = true;

            m_receiveThread = new Thread(ReceivingProcess);
            m_receiveThread.IsBackground = true;

            m_sendThread.Start();
            m_receiveThread.Start();
        }

        public void Close()
        {
            m_closeRequested = true;
            while (m_sendThread.IsAlive || m_receiveThread.IsAlive)
                Thread.Sleep(50);

            m_joined = false;
            if (m_udpClient != null)
            {
                m_udpClient.Dispose();
                m_udpClient = null;
            }
            m_closeRequested = false;

            m_outgoingPackets.Clear();
            m_incomingMessages.Clear();
        }

        public void SendMessage(byte[] message)
        {
            if (m_backgroundException != null)
            {
                Exception exception = m_backgroundException;
                m_backgroundException = null;
                throw exception;
            }

            byte[] packet = WrapData(message, false);

            lock (m_outgoingPackets)
                m_outgoingPackets.Add(packet);
        }

        public void SendAcknowledgedMessage(byte[] message)
        {
            if (m_backgroundException != null)
            {
                Exception exception = m_backgroundException;
                m_backgroundException = null;
                throw exception;
            }

            byte[] packet = WrapData(message, true);

            lock (m_outgoingPackets)
                m_outgoingPackets.Add(packet);
        }

        public byte[] ReceiveMessage(bool wait)
        {
            if (m_backgroundException != null)
            {
                Exception exception = m_backgroundException;
                m_backgroundException = null;
                throw exception;
            }

            byte[] message = null;

            DateTime timestamp = DateTime.Now;
            while (true)
            {
                lock (m_incomingMessages)
                {
                    if (m_incomingMessages.Count > 0)
                    {
                        message = m_incomingMessages[0];
                        m_incomingMessages.RemoveAt(0);
                    }
                }

                if (!wait || message != null)
                    break;

                if ((DateTime.Now - timestamp).TotalMilliseconds > m_timeout)
                    throw new TimeoutException("Network timeout occured");
            }

            return message;
        }

        public bool IsOpen
        {
            get { return m_joined; }
        }

        public long Timeout
        {
            get { return m_timeout; }
            set { m_timeout = Math.Max(value, 100); }
        }

        private void SendingProcess()
        {
            while (!m_closeRequested)
            {
                byte[] nextPacket = null;
                lock (m_outgoingPackets)
                {
                    if (m_outgoingPackets.Count > 0)
                    {
                        nextPacket = m_outgoingPackets[0];
                        m_outgoingPackets.RemoveAt(0);
                    }
                }

                if (nextPacket != null)
                {
                    bool needAck = nextPacket[ProtocolId.Length] == PT_DataHi;

                    if (needAck)
                    {
                        byte packetSequence = nextPacket[ProtocolId.Length + 1];
                        long timer = m_timeout;
                        while (MoreRecent(packetSequence, m_lastAcknowledged))
                        {
                            if (m_closeRequested)
                                return;

                            SendPacket(nextPacket);
                            Thread.Sleep(10);
                            timer -= 10;
                            if (timer < 0)
                            {
                                m_backgroundException = new TimeoutException("Network timeout occured");
                                break;
                            }
                        }

                    }
                    else
                    {
                        SendPacket(nextPacket);
                    }
                }

                Thread.Sleep(10);
            }
        }

        private void ReceivingProcess()
        {
            byte[] incomingBuffer = new byte[512];
            while (!m_closeRequested)
            {
                bool receiving = true;
                m_udpClient.BeginReceiveFromGroup(incomingBuffer, 0, incomingBuffer.Length,
                    result =>
                    {
                        if (m_closeRequested || !m_joined || m_udpClient == null)
                        {
                            receiving = false;
                            return;
                        }

                        IPEndPoint ipEndPoint = null;

                        m_udpClient.EndReceiveFromGroup(result, out ipEndPoint);

                        // filter as in single source multicast
                        if (!ipEndPoint.Equals(m_ipEndPoint))
                        {
                            receiving = false;
                            return;
                        }

                        byte[] datagram = incomingBuffer;

                        if (datagram[0] != ProtocolId[0] || datagram[1] != ProtocolId[1])
                        {
                            receiving = false;
                            return;
                        }

                        switch (datagram[ProtocolId.Length])
                        {
                            case PT_DataLo:
                                {
                                    byte packetSequence = datagram[ProtocolId.Length + 1];
                                    if (MoreRecent(packetSequence, m_remoteLoSequence))
                                    {
                                        m_remoteLoSequence = packetSequence;
                                        UInt16 messageLength = (UInt16)(datagram[ProtocolId.Length + 3] * 0x100 + datagram[ProtocolId.Length + 2]);
                                        byte[] message = new byte[messageLength];
                                        Array.Copy(datagram, ProtocolId.Length + 4, message, 0, message.Length);

                                        lock (m_incomingMessages)
                                            m_incomingMessages.Add(message);
                                    }
                                }
                                break;
                            case PT_DataHi:
                                {
                                    byte packetSequence = datagram[ProtocolId.Length + 1];

                                    byte[] dataAckBuffer = new byte[ProtocolId.Length + 2];
                                    Array.Copy(ProtocolId, dataAckBuffer, ProtocolId.Length);
                                    dataAckBuffer[ProtocolId.Length] = PT_DataAck;
                                    dataAckBuffer[ProtocolId.Length + 1] = packetSequence;

                                    SendPacket(dataAckBuffer);

                                    if (MoreRecent(packetSequence, m_remoteHiSequence))
                                    {
                                        m_remoteHiSequence = packetSequence;
                                        UInt16 messageLength = (UInt16)(datagram[ProtocolId.Length + 3] * 0x100 + datagram[ProtocolId.Length + 2]);
                                        byte[] message = new byte[messageLength];
                                        Array.Copy(datagram, ProtocolId.Length + 4, message, 0, message.Length);

                                        lock (m_incomingMessages)
                                            m_incomingMessages.Add(message);
                                    }
                                }
                                break;
                            case PT_DataAck:
                                byte ackSequence = datagram[ProtocolId.Length + 1];
                                if (MoreRecent(ackSequence, m_lastAcknowledged))
                                    m_lastAcknowledged = ackSequence;
                                break;
                        }

                        receiving = false;
                    },
                    null);

                while (receiving && !m_closeRequested)
                    Thread.Sleep(0);

                Thread.Sleep(10);
            }
        }

        private byte[] WrapData(byte[] message, bool needAcknowledge)
        {
            UInt16 messageLength = (UInt16) message.Length;

            byte[] buffer = new byte[ProtocolId.Length + 4 + message.Length];
            Array.Copy(ProtocolId, buffer, ProtocolId.Length);
            buffer[ProtocolId.Length] = needAcknowledge ? PT_DataHi : PT_DataLo;
            buffer[ProtocolId.Length + 1] = needAcknowledge ? m_localHiSequence : m_localLoSequence;
            buffer[ProtocolId.Length + 2] = (byte)(messageLength & 0xFF);
            buffer[ProtocolId.Length + 3] = (byte)(messageLength >> 8);
            Array.Copy(message, 0, buffer, ProtocolId.Length + 4, message.Length);

            if (needAcknowledge)
                ++m_localHiSequence;
            else
                ++m_localLoSequence;
            
            return buffer;
        }

        private void SendPacket(byte[] buffer)
        {
            lock (m_sendLock)
            {
                if (m_udpClient == null)
                    return;

                bool sending = true;
                m_udpClient.BeginSendToGroup(buffer, 0, buffer.Length,
                    result =>
                    {
                        if (m_udpClient != null)
                            m_udpClient.EndSendToGroup(result);
                        sending = false;
                    }
                    , null);

                while (sending && !m_closeRequested)
                    Thread.Sleep(0);
            }
        }

        private bool MoreRecent(byte newIndex, byte oldIndex)
        {
            return (newIndex > oldIndex && newIndex - oldIndex < 128)
                || (newIndex < oldIndex && oldIndex - newIndex >= 128);
        }

        private const string MulticastAddress = "239.5.6.7";
        private const int MulticastPort = 52275;

        private const byte PT_DataLo  = 2;
        private const byte PT_DataHi  = 3;
        private const byte PT_DataAck = 4;

        private static readonly byte[] ProtocolId = new byte[] { 0x13, 0x37 };

        private IPEndPoint m_ipEndPoint;
        private UdpAnySourceMulticastClient m_udpClient;
        private bool m_joined;
        private bool m_closeRequested;
        private byte m_localLoSequence;
        private byte m_localHiSequence;
        private byte m_remoteLoSequence;
        private byte m_remoteHiSequence;
        private byte m_lastAcknowledged;
        private Thread m_sendThread;
        private Thread m_receiveThread;
        private List<byte[]> m_outgoingPackets;
        private List<byte[]> m_incomingMessages;
        private long m_timeout;
        private Exception m_backgroundException;
        private object m_sendLock = new object();
    }
}
