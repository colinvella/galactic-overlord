using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input.Touch;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using GalacticOverlord.Core;
using GalacticOverlord.Net;
using System.IO;

namespace GalacticOverlord.Players
{
    public class HumanPlayer: Player
    {
        public HumanPlayer(Color colour)
            : this(colour, null)
        {
        }

        public HumanPlayer(Color colour, Channel channel)
            : base(PlayerType.Human, colour, false)
        {
            m_inputEnabled = true;
            m_startingPlanets = new List<Planet>();
            m_sourcePlanets = new List<Planet>();
            m_channel = channel;

            // stuff related to deploy ratio config
            m_deployRatio = 0.60f;
            m_setDeployRatio = false;
            m_deployRatioUI = new DeployRatioUI(m_deployRatio);
        }

        public override void Update(GameTime gameTime)
        {
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            m_startingMarkerRotation += deltaTime * MathHelper.Pi;
            m_startingMarkerRotation = m_startingMarkerRotation % MathHelper.TwoPi;
            m_startingMarkerScale = Math.Max(1.0f, m_startingMarkerScale - deltaTime * 18.0f);

            // deploy ratio interface
            if (m_setDeployRatio)
            {
                if (!InputEnabled)
                {
                    m_setDeployRatio = false;
                }
                else
                {
                    m_deployRatioUI.Update(gameTime);
                    if (m_deployRatioUI.DeployRatioSet)
                    {
                        m_deployRatio = m_deployRatioUI.DeployRatio;
                        m_deployRatioUI.DeployRatioSet = false;
                        m_setDeployRatio = false;
                    }
                }
            }

            while (m_inputEnabled && TouchPanel.IsGestureAvailable)
            {
                GestureSample gestureSample = TouchPanel.ReadGesture();

                // if any starting planets taken, remove them
                for (int index = 0; index < m_startingPlanets.Count;)
                {
                    if (m_startingPlanets[index].Player != this)
                        m_startingPlanets.RemoveAt(index);
                    else
                        ++index;
                }

                if (m_setDeployRatio)
                {
                    // set deploy ratio mode

                    if (gestureSample.GestureType == GestureType.Tap)
                        m_deployRatioUI.Tap(gestureSample.Position);
                    //    m_setDeployRatio = false;
                }
                else
                {
                    // normal play mode
                    switch (gestureSample.GestureType)
                    {
                        case GestureType.Tap:
                            {
                                Vector2 tapPosition = gestureSample.Position;
                                if (tapPosition.X > Space.PlayAreaSize.X - 48.0f
                                    && tapPosition.Y > Space.PlayAreaSize.Y - 64.0f)
                                {
                                    m_setDeployRatio = true;
                                }
                                else
                                {
                                    Planet planet = GetPlanet(tapPosition);
                                    if (planet == null)
                                    {
                                        // tap on empty - clear selections
                                        m_startingPlanets.Clear();
                                        m_sourcePlanets.Clear();
                                        m_targetPlanet = null;
                                    }
                                    else // found planet on tap
                                    {
                                        if (m_startingPlanets.Count > 0)
                                        {
                                            // have initial planets highlighted

                                            if (m_startingPlanets.Contains(planet))
                                            {
                                                // if tapped one of highlighted planets, make it source
                                                m_startingPlanets.Clear();
                                                m_sourcePlanets.Add(planet);
                                            }
                                            else
                                            {
                                                // if another planet is touched, make the highlighted ones source
                                                // and set tapped planet as target
                                                m_sourcePlanets.AddRange(m_startingPlanets);
                                                m_startingPlanets.Clear();
                                                m_targetPlanet = planet;
                                                SendFleetsAndNotify(m_deployRatio);
                                            }
                                        }
                                        else
                                        {
                                            if (m_sourcePlanets.Count == 0)
                                            {
                                                // select single source on tap
                                                if (planet.Player == this)
                                                {
                                                    m_sourcePlanets.Add(planet);
                                                    m_startingPlanets.Clear();
                                                }
                                            }
                                            else // source(s) already selected
                                            {
                                                // if not one of the sources, set as target and
                                                // send fleet
                                                if (!m_sourcePlanets.Contains(planet))
                                                {
                                                    m_targetPlanet = planet;
                                                    SendFleetsAndNotify(m_deployRatio);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            break;
                        /*case GestureType.DoubleTap:
                            {
                                // select all friendly planets on double tap
                                m_targetPlanet = null;
                                m_sourcePlanets.Clear();
                                m_sourcePlanets.AddRange(
                                    Space.Planets.Where(x => x.Player == this));
                                m_startingPlanets.Clear();
                            }
                            break;*/
                        case GestureType.FreeDrag:
                            {
                                // drag for both sources and destinations

                                m_startingPlanets.Clear();

                                // get planet currently touched
                                Planet planet = GetPlanet(gestureSample.Position);

                                if (planet != null)
                                {
                                    // planet found

                                    // add friendly planet to source selection
                                    if (planet.Player == this)
                                    {
                                        if (!m_sourcePlanets.Contains(planet))
                                        {
                                            m_sourcePlanets.Add(planet);
                                            m_startingPlanets.Clear();
                                        }
                                    }
                                    else // empty or hostile
                                    {
                                        // if sources selected, set to target
                                        if (m_sourcePlanets.Count > 0)
                                            m_targetPlanet = planet;
                                    }
                                }
                                else // no planet selected
                                {
                                    // clear target
                                    m_targetPlanet = null;
                                }
                            }
                            break;
                        case GestureType.DragComplete:
                            {
                                // on end of dragging, send fleets if selected
                                if (m_sourcePlanets.Count > 0
                                    && m_targetPlanet != null)
                                {
                                    SendFleetsAndNotify(m_deployRatio);
                                }
                            }
                            break;
                    }
                }
            }

            // remove planets from source selections if lost
            for (int planetIndex = 0; planetIndex < m_sourcePlanets.Count; )
            {
                if (m_sourcePlanets[planetIndex].Player != this)
                    m_sourcePlanets.RemoveAt(planetIndex);
                else
                    ++planetIndex;
            }
        }

        public override void Draw(GameTime gameTime)
        {
            if (m_spriteBatch == null)
            {
                LoadContent();
                m_deployRatioUI.Game = Space.Game;
            }

            if (!m_inputEnabled)
                return;

            // starting planet markers
            m_spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive);
            Vector2 selectorOrigin = new Vector2(128, 128);
            foreach (Planet planet in m_startingPlanets)
            {
                float markerScale = planet.Radius * 1.4f * m_startingMarkerScale / 128.0f;
                m_spriteBatch.Draw(m_startPlanetMarkerTexture,
                    planet.Position, null, Color.White,
                    m_startingMarkerRotation, selectorOrigin, markerScale,
                    SpriteEffects.None, 0.0f);
            }
            m_spriteBatch.End();

            // source planet selection(s)
            foreach (Planet sourcePlanet in m_sourcePlanets)
                DrawPlanetSelection(sourcePlanet);

            // target planet selection
            if (m_targetPlanet != null)
            {
                foreach (Planet sourcePlanet in m_sourcePlanets)
                {
                    Vector2 start = sourcePlanet.Position;
                    Vector2 end = m_targetPlanet.Position;
                    Vector2 unitDirection = end - start;
                    unitDirection.Normalize();
                    start += unitDirection * (sourcePlanet.Radius + 8.0f);
                    end -= unitDirection * (m_targetPlanet.Radius + 8.0f);
                    DrawSelectionLine(start, end);
                }

                DrawPlanetSelection(m_targetPlanet);
            }

            // deploy ratio icon and setting
            m_spriteBatch.Begin();
            m_spriteBatch.Draw(m_deployFleetIconTexture, Space.PlayAreaSize - new Vector2(48, 64), Color.White);
            GraphicsUtility.DrawOutlinedText(m_spriteBatch, m_planetFont, Space.PlayAreaSize, (int)Math.Round(m_deployRatio * 100.0f) + "%", Color.LightSkyBlue, TextAlignment.BottomRight);
            m_spriteBatch.End();

            // deploy ratio interface
            if (m_setDeployRatio)
                m_deployRatioUI.Draw(gameTime);
        }

        public void MarkStartingPlanets()
        {
            m_startingPlanets.Clear();
            m_startingPlanets.AddRange(Space.Planets.Where(x => x.Player == this));
            m_startingMarkerScale = 10.0f;
        }

        public bool InputEnabled
        {
            get { return m_inputEnabled; }
            set
            {
                m_inputEnabled = value;
                if (!m_inputEnabled)
                {
                    m_startingPlanets.Clear();
                    m_sourcePlanets.Clear();
                    m_targetPlanet = null;
                }
            }
        }

        private void LoadContent()
        {
            m_spriteBatch = new SpriteBatch(Space.Game.GraphicsDevice);

            ContentManager contentManager = Space.Game.Content;
            m_planetSelectionParticleTexture = contentManager.Load<Texture2D>(
                @"Graphics\SelectionParticle");
            m_startPlanetMarkerTexture = contentManager.Load<Texture2D>(
                @"Graphics\StartPlanetMarker");
            m_deployFleetIconTexture = contentManager.Load<Texture2D>(
                @"Graphics\DeployRatioIcon");

            m_planetFont = contentManager.Load<SpriteFont>(@"Fonts\PlanetFont");
        }

        private Planet GetPlanet(Vector2 position)
        {
            foreach (Planet planet in Space.Planets)
                if (planet.Touched(position))
                    return planet;

            return null;
        }

        private void SendFleetsAndNotify(float ratio)
        {
            if (m_channel != null)
            {
                int messageLength = 1 + m_sourcePlanets.Count * 3 + 2;

                MemoryStream memoryStream = new MemoryStream(messageLength);
                BinaryWriter binaryWriter = new BinaryWriter(memoryStream, Encoding.UTF8);
                binaryWriter.Write((byte)PlayPacketType.DeployFleets);
                binaryWriter.Write((byte)m_sourcePlanets.Count);
                foreach (Planet sourcePlanet in m_sourcePlanets)
                {
                    binaryWriter.Write((Int16)sourcePlanet.Population);
                    binaryWriter.Write((byte)Space.GetPlanetIndex(sourcePlanet));
                }
                binaryWriter.Write((byte)Space.GetPlanetIndex(m_targetPlanet));
                binaryWriter.Write((byte)(ratio * 100.0f));
                binaryWriter.Flush();

                byte[] message = memoryStream.GetBuffer();
                m_channel.SendAcknowledgedMessage(message);
            }

            SendFleets(m_sourcePlanets, m_targetPlanet, ratio);
            m_sourcePlanets.Clear();
            m_targetPlanet = null;
        }

        private void DrawPlanetSelection(Planet planet)
        {
            DrawSelectionCircle(planet.Position, planet.Radius + 8.0f);

            Vector2 nameOffset = new Vector2(planet.Radius + 16.0f);
            Vector2 textSize = m_planetFont.MeasureString(planet.Name);

            if (planet.Position.X + nameOffset.X + textSize.X > Space.PlayAreaSize.X)
                nameOffset.X = -nameOffset.X;

            if (planet.Position.Y + nameOffset.Y + textSize.Y > Space.PlayAreaSize.Y)
                nameOffset.Y = -nameOffset.Y;

            Vector2 lineStart = nameOffset;
            lineStart.Normalize();
            lineStart *= (planet.Radius + 8.0f);
            lineStart += planet.Position;

            Vector2 lineEnd = planet.Position + nameOffset;

            Vector2 namePosition = lineEnd;
            if (nameOffset.X < 0.0f)
                namePosition.X -= textSize.X;
            if (nameOffset.Y < 0.0f)
                namePosition.Y -= textSize.Y;

            DrawSelectionLine(lineStart, lineEnd);
          
            m_spriteBatch.Begin();
            GraphicsUtility.DrawOutlinedText(m_spriteBatch, m_planetFont, namePosition, planet.Name, Color.Cyan);
            m_spriteBatch.End();
        }

        private void DrawSelectionCircle(Vector2 centre, float radius)
        {
            float angleDelta = 2.0f / radius;
            Vector2 textureOrigin = new Vector2(8.0f, 8.0f);

            m_spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive);

            Vector2 offset = Vector2.Zero;
            for (float angle = 0.0f; angle < MathHelper.PiOver2; angle += angleDelta)
            {
                float offsetX = (float)Math.Cos(angle) * radius;
                float offsetY = (float)Math.Sin(angle) * radius;

                offset.X = offsetX;
                offset.Y = offsetY;
                m_spriteBatch.Draw(m_planetSelectionParticleTexture, centre + offset, null,
                    Color.White, 0.0f, textureOrigin, 1.0f, SpriteEffects.None, 0.0f);
                m_spriteBatch.Draw(m_planetSelectionParticleTexture, centre - offset, null,
                    Color.White, 0.0f, textureOrigin, 1.0f, SpriteEffects.None, 0.0f);

                offset.X = offsetY;
                offset.Y = -offsetX;
                m_spriteBatch.Draw(m_planetSelectionParticleTexture, centre + offset, null,
                    Color.White, 0.0f, textureOrigin, 1.0f, SpriteEffects.None, 0.0f);
                m_spriteBatch.Draw(m_planetSelectionParticleTexture, centre - offset, null,
                    Color.White, 0.0f, textureOrigin, 1.0f, SpriteEffects.None, 0.0f);
            }

            m_spriteBatch.End();
        }

        private void DrawSelectionLine(Vector2 start, Vector2 end)
        {
            Vector2 offset = end - start;
            float length = offset.Length();
            Vector2 deltaOffset = offset;
            deltaOffset.Normalize();
            deltaOffset *= 2.0f;
            Vector2 textureOrigin = new Vector2(8.0f, 8.0f);

            m_spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive);

            Vector2 position = start;
            for (float alpha = 0.0f; alpha < length; alpha += 2.0f)
            {
                m_spriteBatch.Draw(m_planetSelectionParticleTexture, position, null,
                    Color.White, 0.0f, textureOrigin, 1.0f, SpriteEffects.None, 0.0f);
                position += deltaOffset;
            }

            m_spriteBatch.End();
        }

        private Channel m_channel;
        private bool m_inputEnabled;
        private bool m_setDeployRatio;
        private List<Planet> m_startingPlanets;
        private float m_startingMarkerRotation;
        private float m_startingMarkerScale;
        private List<Planet> m_sourcePlanets;
        private Planet m_targetPlanet;

        private float m_deployRatio;
        private DeployRatioUI m_deployRatioUI;

        private SpriteBatch m_spriteBatch;
        private Texture2D m_planetSelectionParticleTexture;
        private Texture2D m_startPlanetMarkerTexture;
        private Texture2D m_deployFleetIconTexture;
        private SpriteFont m_planetFont;
    }
}
