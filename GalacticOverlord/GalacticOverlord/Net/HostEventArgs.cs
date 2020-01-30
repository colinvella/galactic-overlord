using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GalacticOverlord.Net
{
    public class HostEventArgs: EventArgs
    {
        public HostEventArgs(Host remoteHost)
        {
            m_remoteHost = remoteHost;
        }

        public Host RemoteHost
        {
            get { return m_remoteHost; }
        }

        private Host m_remoteHost;
    }
}
