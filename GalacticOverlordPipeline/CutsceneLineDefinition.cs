using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GalacticOverlord.Pipeline
{
    public class CutsceneLineDefinition
    {
        #region Public Methods

        public CutsceneLineDefinition()
        {
        }

        #endregion

        #region Public Properties

        public string CharacterId
        {
            get { return m_characterId; }
            set { m_characterId = value; }
        }

        public string Text
        {
            get { return m_text; }
            set { m_text = value; }
        }

        #endregion

        #region Private Properties

        private string m_characterId;
        private string m_text;

        #endregion
    }
}
