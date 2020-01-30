using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GalacticOverlord.Core
{
    public class PlanetEventArgs: EventArgs
    {
        public PlanetEventArgs(Planet planet)
        {
            m_planet = planet;
        }

        public Planet Planet
        {
            get { return m_planet; }
        }

        private Planet m_planet;
    }
}
