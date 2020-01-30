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


namespace GalacticOverlord.Players
{
    /// <summary>
    /// This is a game component that implements IUpdateable.
    /// </summary>
    public class DeployRatioUI
    {
        public DeployRatioUI(float deployRatio)
        {
            m_deployRatio = deployRatio;
            m_deployRatioPercent = (int)Math.Round(deployRatio * 100.0f);

            m_deployFormationTargets = new Vector2[10];
            m_deployFormationPositions = new Vector2[10];
            for (int index = 0; index < 10; index++)
                m_deployFormationTargets[index] = Vector2.Zero;

            int formationIndex = (int)Math.Round(m_deployRatio * 10.0f);
            SetFormationTargets(formationIndex);

            for (int index = 0; index < 10; index++)
                m_deployFormationPositions[index] = new Vector2(185, 330);

            m_deployRatioSet = false;
        }

        public void Update(GameTime gameTime)
        {
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            for (int index = 0; index < 10; index++)
            {
                Vector2 offset = m_deployFormationTargets[index] - m_deployFormationPositions[index];
                m_deployFormationPositions[index] += offset * deltaTime * 4.0f;
            }
        }

        public void Draw(GameTime gameTime)
        {
            if (m_game == null)
                return;

            if (m_spriteBatch == null)
                LoadContent();

            m_spriteBatch.Begin();

            // draw planet overlay
            m_spriteBatch.Draw(m_deployRatioPlanetTexture, Vector2.Zero, Color.White);

            // draw ship formations
            Vector2 shipTextureOffset = new Vector2(-24.0f, -24.0f);
            int undeployed = (int)Math.Round((1.0f - m_deployRatio) * 10.0f);
            for (int index = 0; index < 10; index++)
            {
                Color colour = index < undeployed ? Color.Gray : Color.White;
                if (index >= undeployed)
                    m_spriteBatch.Draw(m_deployRatioShipJetTexture, m_deployFormationPositions[index] + shipTextureOffset, colour);

                m_spriteBatch.Draw(m_deployRatioShipTexture, m_deployFormationPositions[index] + shipTextureOffset, colour);
            }

            // UI title
            GraphicsUtility.DrawOutlinedText(m_spriteBatch, m_headerFont,
                new Vector2(20, 20), "Fleet Deployment", Color.LightCyan);

            // explanatory text
            GraphicsUtility.DrawOutlinedText(m_spriteBatch, m_planetFont, new Vector2(460, 80), "Tap here to deploy more", Color.LightSkyBlue, TextAlignment.TopRight);
            GraphicsUtility.DrawOutlinedText(m_spriteBatch, m_planetFont, new Vector2(185, 360), "Tap here to deploy less", Color.LightSkyBlue, TextAlignment.Centre);

            // percentage
            GraphicsUtility.DrawOutlinedText(m_spriteBatch, m_headerFont,
                new Vector2(460, 460), m_deployRatioPercent + "%", Color.LightSkyBlue, TextAlignment.TopRight);

            // back button text
            GraphicsUtility.DrawOutlinedText(m_spriteBatch, m_buttonFont,
                new Vector2(240, 610), "Continue", Color.LightSkyBlue, TextAlignment.Centre);

            m_spriteBatch.End();
        }

        public void Tap(Vector2 tapPosition)
        {
            int formationIndex = (int)Math.Round(m_deployRatio * 10.0f);
            if ((tapPosition - new Vector2(185, 330)).Length() < 170.0f)
            {
                if (formationIndex > 1)
                {
                    --formationIndex;
                    m_deployRatio = formationIndex * 0.1f;
                    m_deployRatioPercent = formationIndex * 10;
                    SetFormationTargets(formationIndex);
                }
            }
            else if (tapPosition.X > 185.0f && tapPosition.Y < 330.0f)
            {
                if (formationIndex < 9)
                {
                    ++formationIndex;
                    m_deployRatio = formationIndex * 0.1f;
                    m_deployRatioPercent = formationIndex * 10;
                    SetFormationTargets(formationIndex);
                }
            }
            else if (tapPosition.X >= 130 && tapPosition.X < 350
                && tapPosition.Y > 580 && tapPosition.Y < 640)
            {
                m_deployRatioSet = true;
            }
        }

        public Game Game
        {
            get { return m_game; }
            set { m_game = value; }
        }

        public float DeployRatio
        {
            get { return m_deployRatio; }
        }

        public bool DeployRatioSet
        {
            get { return m_deployRatioSet; }
            set
            {
                m_deployRatioSet = value;
                for (int index = 0; index < 10; index++)
                    m_deployFormationPositions[index] = new Vector2(185, 330);
            }
        }

        private void LoadContent()
        {
            m_spriteBatch = new SpriteBatch(m_game.GraphicsDevice);

            ContentManager contentManager = m_game.Content;

            m_deployRatioPlanetTexture = contentManager.Load<Texture2D>(
                @"Graphics\DeployRatioPlanet");
            m_deployRatioShipTexture = contentManager.Load<Texture2D>(
                @"Graphics\DeployRatioShip");
            m_deployRatioShipJetTexture = contentManager.Load<Texture2D>(
                @"Graphics\DeployRatioShipJet");

            m_headerFont = contentManager.Load<SpriteFont>(@"Fonts\HeaderFont");
            m_buttonFont = contentManager.Load<SpriteFont>(@"Fonts\ButtonFont");
            m_planetFont = contentManager.Load<SpriteFont>(@"Fonts\PlanetFont");
        }

        private void SetFormationTargets(int formationIndex)
        {
            if (formationIndex < 1 || formationIndex > 10)
                return;

            int undeployed = 10 - formationIndex;
            int deployed = formationIndex;

            float undeployedWidth = undeployed * 24.0f + 24.0f;

            for (int index = 0; index < undeployed; index++)
            {
                m_deployFormationTargets[index].X = 210.0f - undeployedWidth * 0.5f + index * 24.0f;
                m_deployFormationTargets[index].Y = 330.0f;
            }

            int rightWing = deployed / 2;
            int leftWing = deployed - rightWing;

            Vector2 leftWingPosition;
            Vector2 rightWingPosition;
            int leftOffset = 0;
            int rightOffset = 0;

            if (rightWing == leftWing)
            {
                leftWingPosition = new Vector2(350, 140);
                rightWingPosition = new Vector2(380, 170);
                leftOffset = 1;
            }
            else
            {
                leftWingPosition = new Vector2(380, 140);
                rightWingPosition = new Vector2(384, 180);
                rightOffset = 1;
            }

            for (int index = 0; index < leftWing; index++)
            {
                m_deployFormationTargets[undeployed + index * 2 + leftOffset] = leftWingPosition;
                leftWingPosition.X -= 40.0f;
                leftWingPosition.Y -= 4.0f;
            }

            for (int index = 0; index < rightWing; index++)
            {
                m_deployFormationTargets[undeployed + index * 2 + rightOffset] = rightWingPosition;
                rightWingPosition.X += 4.0f;
                rightWingPosition.Y += 40.0f;
            }
        }

        private Game m_game;
        private float m_deployRatio;
        private int m_deployRatioPercent;
        private Vector2[] m_deployFormationPositions;
        private Vector2[] m_deployFormationTargets;
        private bool m_deployRatioSet;

        private SpriteBatch m_spriteBatch;
        private Texture2D m_deployRatioPlanetTexture;
        private Texture2D m_deployRatioShipTexture;
        private Texture2D m_deployRatioShipJetTexture;
        private SpriteFont m_headerFont;
        private SpriteFont m_buttonFont;
        private SpriteFont m_planetFont;
    }
}
