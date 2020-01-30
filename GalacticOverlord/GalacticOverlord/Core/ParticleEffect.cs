using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;

namespace GalacticOverlord.Core
{
    public class ParticleEffect
    {
        public ParticleEffect(Space space, Vector2 source, Texture2D particleTexture, Color colour)
        {
            m_space = space;
            m_source = source;
            m_particleTexture = particleTexture;
            m_colour = colour;

            m_lifetime = ParticleLifetime;

            m_particles = new Particle[ParticleCount];
            for (int index = 0; index < ParticleCount; index++)
            {
                m_particles[index].Position = m_source;
                Vector2 velocity = new Vector2(s_random.Next(-20, 20), s_random.Next(-20, 20));
                m_particles[index].Velocity = velocity;
            }
        }

        public bool Expired
        {
            get { return m_lifetime <= 0.0f; }
        }

        public void Update(GameTime gameTime)
        {
            float elapsedTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            m_lifetime -= elapsedTime;

            if (m_lifetime <= 0.0f)
                return;

            for (int index = 0; index < ParticleCount; index++)
            {
                m_particles[index].Position += m_particles[index].Velocity * elapsedTime;
            }
        }

        public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            Vector2 origin = new Vector2(16.0f, 16.0f);
            float opacity = m_lifetime / ParticleLifetime;
            Color colour = m_colour * opacity;
            for (int index = 0; index < ParticleCount; index++)
            {
                spriteBatch.Draw(m_particleTexture, m_particles[index].Position, null,
                    colour, 0.0f, origin, 1.0f, SpriteEffects.None, 0.0f);
            }
        }

        private const int ParticleCount = 8;
        private const float ParticleLifetime = 1.0f;

        private static readonly Random s_random = new Random();

        private Space m_space;
        private Vector2 m_source;
        private float m_lifetime;

        private Particle[] m_particles;

        private Texture2D m_particleTexture;
        private Color m_colour;

        private struct Particle
        {
            public Vector2 Position;
            public Vector2 Velocity;
        }
    }
}
