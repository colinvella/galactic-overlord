using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using Microsoft.Xna.Framework.Media;
using GalacticOverlord.GameStates;

#if AD_DUPLEX
using AdDuplex.Xna;
#endif

namespace GalacticOverlord
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class GalacticOverlordGame : Microsoft.Xna.Framework.Game
    {
        #region Public Methods

        public GalacticOverlordGame()
        {
#if DEBUG
            Guide.SimulateTrialMode = true;
#endif

            m_userProfile = new UserProfile();

            m_graphicsDeviceManager = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";

            // Try to set frame rate to 60 fps
            TargetElapsedTime = TimeSpan.FromTicks(166667);

            // use 800 x 480 screen resolution
            m_graphicsDeviceManager.PreferredBackBufferWidth = 480;
            m_graphicsDeviceManager.PreferredBackBufferHeight = 800;
            m_graphicsDeviceManager.IsFullScreen = true;
            m_graphicsDeviceManager.PreferredDepthStencilFormat = DepthFormat.Depth16;
            m_graphicsDeviceManager.PreferMultiSampling = true;

            m_audioManager = new AudioManager(m_userProfile);
        }

        #endregion

        #region Public Properties

        public UserProfile UserProfile
        {
            get { return m_userProfile; }
        }

        public AudioManager AudioManager
        {
            get {return  m_audioManager; }
        }

        #endregion

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            // TODO: Add your initialization logic here
            TitleGameState titleGameState = new TitleGameState(this);
            Components.Add(titleGameState);

#if AD_DUPLEX
            m_adSpriteBatch = new SpriteBatch(m_graphicsDeviceManager.GraphicsDevice);
            m_adManager = new AdManager(this, AdDuplexAppId);
            m_adManager.LoadContent();
            m_adPosition = new Vector2(0.0f, 720.0f);
#endif

            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.

            // TODO: use this.Content to load your game content here
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            // TODO: Add your update logic here
#if AD_DUPLEX
            m_adManager.Update(gameTime);
#endif

            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            // TODO: Add your drawing code here

            base.Draw(gameTime);

#if AD_DUPLEX
            // ads on top of everything
            m_adManager.Draw(m_adSpriteBatch, m_adPosition, true);
#endif
        }

#if AD_DUPLEX
        #region Private Constants

        private const string AdDuplexAppId = "4863";

        #endregion
#endif

        #region Private Fields

        private UserProfile m_userProfile;
        private GraphicsDeviceManager m_graphicsDeviceManager;
        private AudioManager m_audioManager;

#if AD_DUPLEX
        private SpriteBatch m_adSpriteBatch;
        private AdManager m_adManager;
        private Vector2 m_adPosition;
#endif

        #endregion

    }
}
