using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace GalacticOverlord.Pipeline
{
    public class EnemyDefinition
    {
        public EnemyDefinition()
        {
        }

        public string Id
        {
            get { return m_id; }
            set { m_id = value; }
        }

        public Color Colour
        {
            get { return m_colour; }
            set { m_colour = value; }
        }

        public GameDifficulty Difficulty
        {
            get { return m_gameDifficulty; }
            set { m_gameDifficulty = value; }
        }

        private string m_id;
        private Color m_colour;
        private GameDifficulty m_gameDifficulty;
    }
}
