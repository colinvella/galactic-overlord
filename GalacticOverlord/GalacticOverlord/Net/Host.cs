using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

namespace GalacticOverlord.Net
{
    public class Host
    {
        public Host(IPEndPoint ipEndpoint, string playerId)
        {
            m_ipEndPoint = ipEndpoint;
            m_playerId = playerId;
            m_lastBroadcast = DateTime.Now;
            m_ipAddress = m_ipEndPoint.Address.ToString().Split(new char[] { ':' })[0];
        }

        public int Id
        {
            get { return m_ipEndPoint.Address.GetHashCode(); }
        }

        public IPEndPoint IPEndPoint
        {
            get { return m_ipEndPoint; }
        }

        public string PlayerId
        {
            get { return m_playerId; }
        }

        public DateTime LastBroadcast
        {
            get { return m_lastBroadcast; }
        }

        public double BroadcastAge
        {
            get { return (DateTime.Now - m_lastBroadcast).TotalSeconds; }
        }

        public string IPAddress
        {
            get { return m_ipAddress; }
        }

        private IPEndPoint m_ipEndPoint;
        private string m_playerId;
        private string m_ipAddress;
        private DateTime m_lastBroadcast;
    }
}
