using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;


namespace GalacticOverlord
{
    /// <summary>
    /// This is a game component that implements IUpdateable.
    /// </summary>
    public class ModularGameComponent : DrawableGameComponent
    {
        public ModularGameComponent(GalacticOverlordGame galacticOverlordGame)
            : base(galacticOverlordGame)
        {
            m_galacticOverlordGame = galacticOverlordGame;
            m_childComponents = new List<ModularGameComponent>();
        }

        /// <summary>
        /// Allows the game component to perform any initialization it needs to before starting
        /// to run.  This is where it can query for any required services and load content.
        /// </summary>
        public override void Initialize()
        {
            // TODO: Add your initialization code here
            base.Initialize();

            foreach (ModularGameComponent childCOmponent in m_childComponents)
                childCOmponent.Initialize();
        }

        public virtual void Shutdown()
        {
            foreach (ModularGameComponent childCOmponent in m_childComponents)
                childCOmponent.Shutdown();
        }

        protected override void LoadContent()
        {
            base.LoadContent();

            foreach (ModularGameComponent childCOmponent in m_childComponents)
                childCOmponent.LoadContent();
        }

        protected override void UnloadContent()
        {
            foreach (ModularGameComponent childCOmponent in m_childComponents)
                childCOmponent.UnloadContent();

            base.UnloadContent();
        }

        /// <summary>
        /// Allows the game component to update itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        public override void Update(GameTime gameTime)
        {
            // TODO: Add your update code here
            base.Update(gameTime);

            foreach (ModularGameComponent childCOmponent in m_childComponents)
                if (childCOmponent.Enabled)
                    childCOmponent.Update(gameTime);
        }

        public override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);

            foreach (ModularGameComponent childCOmponent in m_childComponents)
                if (childCOmponent.Visible)
                    childCOmponent.Draw(gameTime);
        }

        public GalacticOverlordGame GalacticOverlordGame
        {
            get { return m_galacticOverlordGame;  }
        }

        public List<ModularGameComponent> ChildComponents
        {
            get { return m_childComponents; }
        }

        private GalacticOverlordGame m_galacticOverlordGame;
        private List<ModularGameComponent> m_childComponents;
    }
}
