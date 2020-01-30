using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Content;

namespace GalacticOverlord.Pipeline
{
    public class LevelDefinition
    {
        public LevelDefinition()
        {
        }

        [ContentSerializer(ElementName = "Enemies", CollectionItemName = "Enemy")]
        public EnemyDefinition[] Enemies
        {
            get { return m_enemies; }
            set { m_enemies = value; }
        }

        [ContentSerializer(ElementName = "Planets", CollectionItemName = "Planet")]
        public PlanetDefinition[] Planets
        {
            get { return m_planets; }
            set { m_planets = value; }
        }

        private EnemyDefinition[] m_enemies;
        private PlanetDefinition[] m_planets;
    }
}
