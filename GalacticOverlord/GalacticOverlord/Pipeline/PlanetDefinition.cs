using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace GalacticOverlord.Pipeline
{
    public class PlanetDefinition
    {
        public PlanetDefinition()
        {
        }

        public string Name
        {
            get { return m_name; }
            set { m_name = value; }
        }

        public Vector2 Position
        {
            get { return m_position; }
            set { m_position = value; }
        }

        public float Radius
        {
            get { return m_radius; }
            set { m_radius = value; }
        }

        public int Population
        {
            get { return m_population; }
            set { m_population = value; }
        }

        public string Player
        {
            get { return m_player; }
            set { m_player = value; }
        }

        private string m_name;
        private Vector2 m_position;
        private float m_radius;
        private int m_population;
        private string m_player;
    }
}
