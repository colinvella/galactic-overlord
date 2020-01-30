using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Audio;
using GalacticOverlord.Players;

namespace GalacticOverlord.Core
{
    public class Fleet
    {
        public Fleet(Planet sourcePlanet, Planet targetPlanet, int size)
        {
            m_space = sourcePlanet.Player.Space;
            m_player = sourcePlanet.Player;
            m_sourcePlanet = sourcePlanet;
            m_targetPlanet = targetPlanet;

            m_targetOffset = m_targetPlanet.Position - m_sourcePlanet.Position;
            m_targetBearing = (float)Math.Atan2(m_targetOffset.Y, m_targetOffset.X);

            // redimensions ship strength / count for performance reasons
            m_shipStrength = 1.0f;
            int totalActiveShips = m_player.Space.Fleets.Sum(x => x.Size) + size;
            if (totalActiveShips > MaxActiveShips / 2)
            {
                float strengthRatio = 4.0f * (float)size / (float)MaxActiveShips ;
                m_shipStrength *= strengthRatio;
                size = (int)Math.Ceiling((float)size / strengthRatio);
            }

            m_ships = new List<Ship>(size);
            m_rollout = size;
            m_rolloutTimer = 0.0f;

            if (s_fleetLaunchSoundEffectInstance == null)
            {
                ContentManager contentManager = m_space.Game.Content;
                SoundEffect fleetLaunchSoundEffect = contentManager.Load<SoundEffect>(@"Audio\FleetLaunch");
                s_fleetLaunchSoundEffectInstance = fleetLaunchSoundEffect.CreateInstance();
            }

            if (s_fleetLaunchSoundEffectInstance.State != SoundState.Playing)
                Player.Space.GalacticOverlordGame.AudioManager.PlaySound(s_fleetLaunchSoundEffectInstance);
        }

        public void Update(GameTime gameTime)
        {
            // abort rest of rollout if planet taken before all ships launched
            if (m_sourcePlanet.Player != m_player && m_rollout > 0)
                m_rollout = 0;

            if (m_rollout > 0)
            {
                m_rolloutTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;
                if (m_rolloutTimer < 0.0f)
                {
                    float perturbation = ((float)s_random.NextDouble() - 0.5f) * MathHelper.PiOver4;
                    float direction = m_targetBearing + perturbation;
                    Vector2 shipDirection = new Vector2((float)Math.Cos(direction), (float)Math.Sin(direction));
                    Vector2 shipVelocity = shipDirection * Ship.TargetSpeed;
                    Vector2 shipOffset = shipDirection * m_sourcePlanet.Radius;
                    Vector2 shipPosition = m_sourcePlanet.Position + shipOffset;

                    Ship ship = new Ship(this, shipPosition, shipVelocity);
                    m_ships.Add(ship);
                    m_rolloutTimer += RolloutInterval;
                    --m_rollout;
                }
            }

            Vector2 target = m_targetPlanet.Position;
            foreach (Ship ship in m_ships)
                ship.Update(gameTime, target);
        }

        public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            if (m_player.Cloaked)
                return;

            ContentManager contentManager = m_player.Space.Game.Content;
            Texture2D shipTexture = contentManager.Load<Texture2D>(@"Graphics\Ship");
            Texture2D shipParticleTexture = contentManager.Load<Texture2D>(@"Graphics\ShipParticle");

            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive);
            foreach (Ship ship in m_ships)
                ship.DrawTrail(gameTime, spriteBatch, shipParticleTexture);
            spriteBatch.End();

            spriteBatch.Begin();
            Color colour = m_player.Colour;
            foreach (Ship ship in m_ships)
                ship.DrawShip(gameTime, spriteBatch, shipTexture, colour);
            spriteBatch.End();
        }

        public void Redimension(float strengthRatio)
        {
            m_shipStrength *= strengthRatio;
            m_rollout = (int)Math.Round(m_rollout / strengthRatio); 
        }

        public Player Player
        {
            get { return m_player; }
        }

        public List<Ship> Ships
        {
            get { return m_ships; }
        }

        public float ShipStrength
        {
            get { return m_shipStrength; }
        }

        public Planet SourcePlanet
        {
            get { return m_sourcePlanet; }
        }

        public Planet TargetPlanet
        {
            get { return m_targetPlanet; }
        }

        public int Size
        {
            get { return (int)((m_ships.Count + m_rollout) * m_shipStrength); }
        }

        public int ActiveShipCount
        {
            get { return m_ships.Count; }
        }

        private static int MaxActiveShips = 50;
        private const float RolloutInterval = 4.0f / Ship.TargetSpeed;

        private static readonly Random s_random = new Random();

        private static SoundEffectInstance s_fleetLaunchSoundEffectInstance;

        private Space m_space;
        private Player m_player;
        private float m_shipStrength;
        private Planet m_sourcePlanet;
        private Planet m_targetPlanet;
        private Vector2 m_targetOffset;
        private float m_targetBearing;
        private List<Ship> m_ships;
        private int m_rollout;
        private float m_rolloutTimer;
    }
}
