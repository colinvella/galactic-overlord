using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Content;

namespace GalacticOverlord.Pipeline
{
    public class LevelDefinition
    {
        #region Public Methods

        public LevelDefinition()
        {
        }

        #endregion

        #region Public Properties

        [ContentSerializer(ElementName = "Cutscene", CollectionItemName = "Line")]
        public CutsceneLineDefinition[] Cutscene
        {
            get { return m_cutscene; }
            set { m_cutscene = value; }
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

        #endregion

        #region Private Fields

        private CutsceneLineDefinition[] m_cutscene;
        private EnemyDefinition[] m_enemies;
        private PlanetDefinition[] m_planets;

        #endregion
    }
}
