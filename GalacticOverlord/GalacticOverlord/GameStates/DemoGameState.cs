using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GalacticOverlord.Core;
using GalacticOverlord.Players;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input.Touch;
using Microsoft.Xna.Framework.Graphics;
using GalacticOverlord.UI;
using Microsoft.Xna.Framework.Input;
using GalacticOverlord.Pipeline;

namespace GalacticOverlord.GameStates
{
    public class DemoGameState: GameState
    {
        public DemoGameState(GalacticOverlordGame galacticOverlordGame)
            :base(galacticOverlordGame)
        {
        }

        public override void Initialize()
        {
            m_space = new Space(GalacticOverlordGame);
            Player playerOne = new ComputerPlayer(DifficultyLevel.Impossible, Color.Crimson, false);
            Player playerTwo = new ComputerPlayer(DifficultyLevel.Impossible, Color.Magenta, false);
            Player playerThree = new ComputerPlayer(DifficultyLevel.Impossible, Color.YellowGreen, false);
            Player playerFour = new ComputerPlayer(DifficultyLevel.Impossible, Color.SkyBlue, false);

            MapGenerator.GenerateFourWayMap(m_space, playerOne, playerTwo, playerThree, playerFour);

            m_space.PlayerEliminated += OnPlayerEliminated;

            ChildComponents.Add(m_space);

            TouchPanel.EnabledGestures = GestureType.Tap;

            m_spriteBatch = new SpriteBatch(Game.GraphicsDevice);

            base.Initialize();
        }

        public override void Update(GameTime gameTime)
        {
            // switch to title on back button
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
            {
                SwitchToState(new TitleGameState(GalacticOverlordGame));
                return;
            }

            // switch to title on tap
            while (TouchPanel.IsGestureAvailable)
            {
                GestureSample gestureSample = TouchPanel.ReadGesture();
                if (gestureSample.GestureType == GestureType.Tap)
                {
                    SwitchToState(new TitleGameState(GalacticOverlordGame));
                    return;
                }
            }

            base.Update(gameTime);
        }

        public override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);

            m_spriteBatch.Begin();
            m_spriteBatch.Draw(m_overlayTexture, new Vector2(0, 336), Color.White);

            if (gameTime.TotalGameTime.Milliseconds < 500)
                GraphicsUtility.DrawOutlinedText(m_spriteBatch, m_largeFont,
                    new Vector2(240, 400), "Tap To Start", Color.LightCyan, TextAlignment.Centre);

#if AD_DUPLEX
            const int creditOffset = 80;
#else
            const int creditOffset = 0;
#endif
            GraphicsUtility.DrawOutlinedText(m_spriteBatch, m_smallFont,
                new Vector2(240, 760 - creditOffset), "Design & Programming", Color.LightCyan, TextAlignment.Centre);
            GraphicsUtility.DrawOutlinedText(m_spriteBatch, m_smallFont,
                new Vector2(240, 780 - creditOffset), "Colin Vella", Color.LightCyan, TextAlignment.Centre);

            m_spriteBatch.End();
        }

        protected override void LoadContent()
        {
            m_overlayTexture = Game.Content.Load<Texture2D>(@"Graphics\HeaderOverlay");
            m_largeFont = Game.Content.Load<SpriteFont>(@"Fonts\HeaderFont");
            m_smallFont = Game.Content.Load<SpriteFont>(@"Fonts\PlanetFont");

            base.LoadContent();
        }

        private void OnPlayerEliminated(object sender, PlayerEventArgs playerEventArgs)
        {
            if (m_space.ActivePlayers.Count() < 2)
                SwitchToState(new TitleGameState(GalacticOverlordGame));
        }

        private Space m_space;

        private SpriteBatch m_spriteBatch;
        private Texture2D m_overlayTexture;
        private SpriteFont m_largeFont;
        private SpriteFont m_smallFont;
    }
}
