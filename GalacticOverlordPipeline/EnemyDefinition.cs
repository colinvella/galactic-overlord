using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace GalacticOverlord.Pipeline
{
    public class EnemyDefinition
    {
        #region Public Methods

        public EnemyDefinition()
        {
        }

        public Color GetColour()
        {
            switch (m_colour)
            {
                case EnemyColour.Green: return Color.YellowGreen;
                case EnemyColour.Red: return Color.Crimson;
                case EnemyColour.Blue: return Color.SkyBlue;
                case EnemyColour.Brown: return Color.SandyBrown;
                case EnemyColour.White: return Color.White;
                case EnemyColour.Purple: return Color.Purple;
                default: return Color.Black;
            }
        }

        #endregion

        #region Public Properties

        public PlayerId Id
        {
            get { return m_id; }
            set { m_id = value; }
        }

        public EnemyColour Colour
        {
            get { return m_colour; }
            set { m_colour = value; }
        }

        public DifficultyLevel Difficulty
        {
            get { return m_difficultyLevel; }
            set { m_difficultyLevel = value; }
        }

        public bool Cloaked
        {
            get { return m_cloaked; }
            set { m_cloaked = value; }
        }

        #endregion

        #region Private Fields

        private PlayerId m_id;
        private EnemyColour m_colour;
        private DifficultyLevel m_difficultyLevel;
        private bool m_cloaked;

        #endregion
    }
}
