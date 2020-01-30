using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Input.Touch;
using Microsoft.Xna.Framework;

namespace GalacticOverlord.UI
{
    public class TouchInterface: ModularGameComponent
    {
        public TouchInterface(GalacticOverlordGame galacticOverlordGame)
            : base(galacticOverlordGame)
        {
            m_gestureSamples = new List<GestureSample>();
        }

        public override void Initialize()
        {
            base.Initialize();
        }

        public override void Update(GameTime gameTime)
        {

            m_gestureSamples.Clear();
            while (TouchPanel.IsGestureAvailable)
            {
                m_gestureSamples.Add(TouchPanel.ReadGesture());
            }

            base.Update(gameTime);
        }

        public IEnumerable<GestureSample> GestureSamples
        {
            get { return m_gestureSamples; }
        }

        private List<GestureSample> m_gestureSamples;
    }
}
