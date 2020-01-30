using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Content;

namespace GalacticOverlord.Pipeline
{
    public class CampaignDefinition
    {
        public CampaignDefinition()
        {
        }

        [ContentSerializer(ElementName = "Levels", CollectionItemName = "Level")]
        public LevelDefinition[] Levels
        {
            get { return m_levels; }
            set { m_levels = value; }
        }

        private LevelDefinition[] m_levels;
    }
}
