using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GalacticOverlord.Core
{
    public class Ship
    {
        public Ship(Fleet fleet, Vector2 position, Vector2 velocity)
        {
            m_fleet = fleet;
            m_position = position;
            m_velocity = velocity;
            m_trail = new Vector2[TrailSize];
            for (int index = 0; index < TrailSize; index++)
                m_trail[index] = position;
        }

        public void Update(GameTime gameTime, Vector2 target)
        {
            Vector2 targetVelocity = target - m_position;
            targetVelocity.Normalize();
            targetVelocity *= TargetSpeed;

            Vector2 deltaVelocity = targetVelocity - m_velocity;

            m_velocity += deltaVelocity * (float)gameTime.ElapsedGameTime.TotalSeconds * 2.0f;

            m_position += m_velocity * (float)gameTime.ElapsedGameTime.TotalSeconds;

            if ((m_position - m_trail[m_trailIndex]).LengthSquared() > 4.0f)
            {
                m_trailIndex = (m_trailIndex + 1) % TrailSize;
                m_trail[m_trailIndex] = m_position;
            }
        }

        public void DrawShip(GameTime gameTime, SpriteBatch spriteBatch, Texture2D shipTexture, Color colour)
        {
            Vector2 origin = new Vector2(8.0f, 8.0f);
            float rotation = (float)Math.Atan2(m_velocity.Y, m_velocity.X);
            spriteBatch.Draw(shipTexture, m_position, null, colour, rotation, origin, 1.0f, SpriteEffects.None, 0.0f);
        }

        public void DrawTrail(GameTime gameTime, SpriteBatch spriteBatchAdditive, Texture2D shipParticleTexture)
        {
            Vector2 origin = new Vector2(8.0f, 8.0f);
            int trailIndex = (m_trailIndex + 1) % TrailSize;
            float opacity = 0.0f, opacityDelta = 1.0f / TrailSize;
            for (int count = TrailSize; count > 0; count--)
            {
                Vector2 trail = m_trail[trailIndex];
                trailIndex = (trailIndex + 1) % TrailSize;
                Color particleColour = Color.Orange * opacity;
                opacity += opacityDelta;
                spriteBatchAdditive.Draw(shipParticleTexture, trail, null, particleColour, 0.0f, origin, 1.0f, SpriteEffects.None, 0.0f);
            }
        }

        public Fleet Fleet
        {
            get { return m_fleet; }
        }

        public Vector2 Position
        {
            get { return m_position; }
        }

        public Vector2 Velocity
        {
            get { return m_velocity; }
            set { m_velocity = value; }
        }

        public const float TargetSpeed = 100.0f;

        public const float Radius = 8.0f;

        private const int TrailSize = 16;

        private Fleet m_fleet;
        private Vector2 m_position;
        private Vector2 m_velocity;
        private Vector2[] m_trail;
        private int m_trailIndex;
    }
}
