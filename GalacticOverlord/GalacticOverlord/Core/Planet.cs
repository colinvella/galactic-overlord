using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Audio;
using GalacticOverlord.Players;
using GalacticOverlord.Pipeline;

namespace GalacticOverlord.Core
{
    public class Planet
    {
        public Planet(Space space,
            string name, Vector2 position, float radius, PlanetType planetType, float rotation, Player player, int population)
        {
            m_space = space;
            m_name = name;

            m_position = position;
            m_orientation = 0.0f;

            m_velocity = Vector2.Zero;
            m_rotation = 0.0f;

            Radius = radius;

            m_orientation = rotation;
            m_player = player;
            m_population = population;

            m_random = new Random();

            m_contentManager = space.Game.Content;

            string planetSurfaceTextureId = @"Graphics\" + planetType.ToString();
            m_planetSurfaceTexture = m_contentManager.Load<Texture2D>(planetSurfaceTextureId);

            m_planetLightingTexture = m_contentManager.Load<Texture2D>(@"Graphics\PlanetLighting");
            m_planetFlash = m_contentManager.Load<Texture2D>(@"Graphics\PlanetFlash");
            m_shipExplosionTexture = m_contentManager.Load<Texture2D>(@"Graphics\ShipExplosion");
            m_shipLandingTexture = m_contentManager.Load<Texture2D>(@"Graphics\ShipLanding");

            m_planetFont = m_contentManager.Load<SpriteFont>(@"Fonts\PlanetFont");

            SoundEffect soundEffect = m_contentManager.Load<SoundEffect>(@"Audio\ShipAttack");
            m_shipAttackSoundEffectInstace = soundEffect.CreateInstance();

            soundEffect = m_contentManager.Load<SoundEffect>(@"Audio\ShipAccumulate");
            m_shipAccumulateSoundEffectInstace = soundEffect.CreateInstance();
        }

        public void Update(GameTime gameTime)
        {
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (m_velocity != Vector2.Zero)
            {
                // update position for asteroids mode
                m_position += m_velocity * deltaTime;

                // basic collision detection around borders
                bool borderCollision = false;
                if (m_position.X - m_radius < 0.0f && m_velocity.X < 0.0f)
                {
                    m_position.X = m_radius;
                    m_velocity.X = -m_velocity.X;
                }
                if (m_position.Y - m_radius < 0.0f && m_velocity.Y < 0.0f)
                {
                    m_position.Y = m_radius;
                    m_velocity.Y = -m_velocity.Y;
                    borderCollision = true;
                }
                if (m_position.X + m_radius >= Space.PlayAreaSize.X && m_velocity.X > 0.0f)
                {
                    m_position.X = Space.PlayAreaSize.X - m_radius - 1.0f;
                    m_velocity.X = -m_velocity.X;
                    borderCollision = true;
                }
                if (m_position.Y + m_radius >= Space.PlayAreaSize.Y && m_velocity.Y > 0.0f)
                {
                    m_position.Y = Space.PlayAreaSize.Y - m_radius - 1.0f;
                    m_velocity.Y = -m_velocity.Y;
                    borderCollision = true;
                }

                // randomise rotation on border collision
                if (borderCollision)
                    m_rotation = (float)(m_random.NextDouble() - 0.5) * MathHelper.PiOver2;
            }

            m_orientation += m_rotation * deltaTime;
            m_orientation = m_orientation % MathHelper.TwoPi;

            if (m_player != null)
            {
                // compute population growth

                // consider all fleets originating from planet
                float totalPopulation = m_population;
                foreach (Fleet fleet in m_space.Fleets)
                    if (fleet.Player == m_player
                        && fleet.SourcePlanet == this)
                        totalPopulation += fleet.Size;

                float growthPerSecond = m_radius * SELDON_GROWTH_FACTOR / (Math.Max(m_radius, totalPopulation));
                m_population += growthPerSecond * deltaTime;

                if (m_flashLifetime > 0.0f)
                    m_flashLifetime -= (float)gameTime.ElapsedGameTime.TotalSeconds;
                else
                    m_flashLifetime = 0.0f;
            }
        }

        public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            spriteBatch.Begin();

            Color planetColour = m_player == null ? Color.Gray : m_player.Colour;

            float textureScale = m_radius * 2.0f / (float)m_planetSurfaceTexture.Width;
            Vector2 origin = new Vector2(m_planetSurfaceTexture.Width * 0.5f);

            spriteBatch.Draw(m_planetSurfaceTexture, m_position, null, planetColour,
                m_orientation, origin, textureScale, SpriteEffects.None, 0.0f);

            spriteBatch.Draw(m_planetLightingTexture, m_position, null, Color.White,
                0.0f, origin, textureScale, SpriteEffects.None, 0.0f);

            if (Player == null || !Player.Cloaked)
            {
                GraphicsUtility.DrawOutlinedText(
                    spriteBatch, m_planetFont, m_position, ((int)m_population).ToString(), Color.White, TextAlignment.Centre);
            }

            spriteBatch.End();

            // conquer shockwave
            if (m_flashLifetime > 0.0f && m_player != null)
            {
                float flashScale = textureScale * (21.0f - m_flashLifetime * 10.0f);
                Color flashColour = m_player.Colour * (m_flashLifetime * 0.5f);

                spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive);

                spriteBatch.Draw(m_planetFlash, m_position, null, flashColour,
                    0.0f, origin, flashScale, SpriteEffects.None, 0.0f);

                spriteBatch.End();
            }
        }

        public void BackupConfiguration()
        {
            m_backupPosition = m_position;
            m_backupVelocity = m_velocity;
            m_backupPlayer = m_player;
            m_backupPopulation = m_population;
        }

        public void RestoreConfiguration()
        {
            m_position = m_backupPosition;
            m_velocity = m_backupVelocity;
            m_player = m_backupPlayer;
            m_population = m_backupPopulation;
        }

        public bool Touched(Vector2 touchPoint)
        {
            return (touchPoint - m_position).LengthSquared() <= m_touchRadiusSquared;
        }

        public bool InCollisionWith(Planet otherPlanet)
        {
            float radiusSumSquared = m_radius + otherPlanet.Radius;
            radiusSumSquared *= radiusSumSquared;
            return (otherPlanet.m_position - m_position).LengthSquared()
                <= radiusSumSquared;
        }

        public Fleet LaunchFleet(float ratio, Planet targetPlanet)
        {
            ratio = MathHelper.Clamp(ratio, 0.0f, 1.0f);
            int size = (int)(m_population * ratio);
            m_population -= size;
            return new Fleet(this, targetPlanet, size);
        }

        public void AbsorbShip(Fleet fleet, Vector2 shipPosition)
        {
            Player player = fleet.Player;
            AudioManager audioManager = m_space.GalacticOverlordGame.AudioManager;

            if (m_player == null)
            {
                // conquer local population
                if (fleet.Player.Type != PlayerType.NetClient)
                    m_population -= fleet.ShipStrength;

                m_space.AddParticleEffect(shipPosition, m_shipExplosionTexture, Color.White);

                if (m_shipAttackSoundEffectInstace.State == SoundState.Stopped)
                    audioManager.PlaySound(m_shipAttackSoundEffectInstace);

                // conquer empty planet
                if (m_population < 0.0f)
                {
                    m_player = player;
                    m_population = 0.0f;
                    m_flashLifetime = 2.0f;
                }
            }
            else if (m_player == player)
            {
                // increase own population
                m_population += fleet.ShipStrength;

                m_space.AddParticleEffect(shipPosition, m_shipLandingTexture, player.Colour);

                if (m_shipAccumulateSoundEffectInstace.State != SoundState.Playing)
                    audioManager.PlaySound(m_shipAccumulateSoundEffectInstace);
            }
            else // other player
            {
                // decrease local enemy population
                m_population -= fleet.ShipStrength;
                if (m_population < 0.0f)
                {
                    Player formerPlayer = m_player;

                    // on eradication, planet becomes empty
                    m_player = null;
                    m_population = 0.0f;
                    m_flashLifetime = 2.0f;

                    m_space.CheckPlayerElimination(formerPlayer);
                }

                m_space.AddParticleEffect(shipPosition, m_shipExplosionTexture, Color.White);

                if (m_shipAttackSoundEffectInstace.State == SoundState.Stopped)
                    audioManager.PlaySound(m_shipAttackSoundEffectInstace);
            }
        }

        public Player Player
        {
            get { return m_player; }
            set
            {
                if (m_player == value)
                    return;
                m_player = value;
                if (m_player != null)
                    m_flashLifetime = 2.0f;
            }
        }

        public string Name
        {
            get { return m_name; }
        }

        public bool Occupied
        {
            get { return m_player != null; }
        }

        public Vector2 Position
        {
            get { return m_position; }
            set { m_position = value; }
        }

        public float Orientation
        {
            get { return m_orientation; }
            set
            {
                m_orientation = value;
                while (m_orientation >= MathHelper.Pi)
                    m_orientation -= MathHelper.TwoPi;
                while (m_orientation < -MathHelper.Pi)
                    m_orientation += MathHelper.TwoPi;
            }
        }

        public Vector2 Velocity
        {
            get { return m_velocity; }
            set
            {
                m_velocity = value;

            }
        }

        public float Rotation
        {
            get { return m_rotation; }
            set { m_rotation = value; }
        }

        public float Radius
        {
            get { return m_radius; }
            set
            {
                m_radius = value;
                m_radiusSquared = m_radius * m_radius;
                float touchRadius = Math.Max(m_radius, 32.0f);
                m_touchRadiusSquared = touchRadius * touchRadius;
            }
        }

        public float Mass
        {
            get { return m_radiusSquared; }
        }

        public Vector2 Momentum
        {
            get { return m_velocity * m_radiusSquared; }
        }

        public float Population
        {
            get { return m_population; }
            set { m_population = value; }
        }

        private const float SELDON_GROWTH_FACTOR = 5.0f;

        private Space m_space;
        private string m_name;
        private Vector2 m_position;
        private float m_orientation;
        private Vector2 m_velocity;
        private float m_rotation;
        private float m_radius;
        private float m_radiusSquared;
        private float m_touchRadiusSquared;
        private Player m_player;
        private float m_population;
        private float m_flashLifetime;

        private Vector2 m_backupPosition;
        private Vector2 m_backupVelocity;
        private Player m_backupPlayer;
        private float m_backupPopulation;

        private Random m_random;

        private ContentManager m_contentManager;

        private Texture2D m_planetSurfaceTexture;
        private Texture2D m_planetLightingTexture;
        private Texture2D m_planetFlash;
        private Texture2D m_shipExplosionTexture;
        private Texture2D m_shipLandingTexture;
        private SpriteFont m_planetFont;

        private SoundEffectInstance m_shipAttackSoundEffectInstace;
        private SoundEffectInstance m_shipAccumulateSoundEffectInstace;
    }
}
