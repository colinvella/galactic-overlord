using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Content;

namespace GalacticOverlord.Pipeline
{
    public class CampaignDefinition
    {
        #region Public Methods

        public CampaignDefinition()
        {
        }

        #endregion

        #region Public Properties

        [ContentSerializer(ElementName = "Levels", CollectionItemName = "Level")]
        public LevelDefinition[] Levels
        {
            get { return m_levels; }
            set { m_levels = value; }
        }

        #endregion

        #region Private Fields

        private LevelDefinition[] m_levels;

        #endregion
    }
}
