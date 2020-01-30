using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using GalacticOverlord.Core;

namespace GalacticOverlord.Players
{
    public abstract class Player
    {
        #region Public Methods

        public Player(PlayerType playerType, Color colour, bool cloaked)
        {
            m_space = null;
            m_playerType = playerType;
            m_colour = colour;
            m_cloaked = cloaked;
        }

        public abstract void Update(GameTime gameTime);

        public abstract void Draw(GameTime gameTime);

        #endregion

        #region Public Properties

        public Space Space
        {
            get { return m_space; }
            set { m_space = value; }
        }

        public PlayerType Type
        {
            get { return m_playerType; }
        }

        public Color Colour
        {
            get { return m_colour; }
        }

        public bool Cloaked
        {
            get { return m_cloaked; }
        }

        #endregion

        #region Protected Methods

        protected void SendFleet(Planet sourcePlanet, Planet targetPlanet, float ratio)
        {
            if (sourcePlanet.Player != this)
                return;
            Fleet fleet = sourcePlanet.LaunchFleet(ratio, targetPlanet);
            if (fleet != null)
                Space.AddFleet(fleet);
        }

        protected void SendFleets(IEnumerable<Planet> sourcePlanets, 
            Planet targetPlanet, float ratio)
        {
            foreach (Planet sourcePlanet in sourcePlanets)
                SendFleet(sourcePlanet, targetPlanet, ratio);
        }

        #endregion

        #region Private Fields

        private Space m_space;
        private PlayerType m_playerType;
        private Color m_colour;
        private bool m_cloaked;

        #endregion
    }

    public enum PlayerType
    {
        Human,
        Computer,
        NetServer,
        NetClient
    }
}
