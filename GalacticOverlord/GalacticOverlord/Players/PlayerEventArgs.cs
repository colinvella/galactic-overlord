using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GalacticOverlord.Players
{
    public class PlayerEventArgs: EventArgs
    {
        public PlayerEventArgs(Player player)
        {
            m_player = player;
        }

        public Player Player
        {
            get { return m_player; }
        }

        private Player m_player;
    }
}
