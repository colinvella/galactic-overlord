using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input.Touch;
using GalacticOverlord.Players;

namespace GalacticOverlord.Core
{
    public class Space : ModularGameComponent
    {
        #region Public Methods

        public Space(GalacticOverlordGame galacticOverlordGame)
            : base(galacticOverlordGame)
        {
            m_galacticOverlordGame = galacticOverlordGame;
            m_players = new List<Player>();
            m_planets = new List<Planet>();
            m_fleets = new List<Fleet>();
            m_explosions = new List<ParticleEffect>();

            // initialise for collision detection
            m_movingPlanets = false;
            for (int sectorY = 0; sectorY < SectorsDown; sectorY++)
                for (int sectorX = 0; sectorX < SectorsAcross; sectorX++)
                    m_sectors[sectorX, sectorY] = new Sector();

            m_random = new Random();
            m_backgroundIndex = m_random.Next(Backgrounds);

            m_spriteBatch = new SpriteBatch(Game.GraphicsDevice);
        }

        public override void  Initialize()
        {
        }

        public override void  Update(GameTime gameTime)
        {
            foreach (Player player in m_players)
                player.Update(gameTime);

            foreach (Planet planet in m_planets)
                planet.Update(gameTime);

            if (m_movingPlanets)
                HandlePlanetCollisions();

            UpdateSectors();
            HandlePlanetShipCollisions();

            for (int index = 0; index < m_fleets.Count; )
            {
                Fleet fleet = m_fleets[index];
                fleet.Update(gameTime);

                if (fleet.Size == 0)
                {
                    m_fleets.RemoveAt(index);
                    CheckPlayerElimination(fleet.Player);
                }
                else
                    ++index;
            }

            for (int index = 0; index < m_explosions.Count; )
            {
                ParticleEffect explosion = m_explosions[index];
                explosion.Update(gameTime);

                if (explosion.Expired)
                    m_explosions.RemoveAt(index);
                else
                    ++index;
            }

        }

        public override void Draw(GameTime gameTime)
        {
            m_spriteBatch.Begin();
            m_spriteBatch.Draw(m_backgroundTexture, Vector2.Zero, Color.White);
            m_spriteBatch.End();

            foreach (Planet planet in m_planets)
            {
                planet.Draw(gameTime, m_spriteBatch);
            }

            foreach (Fleet fleet in m_fleets)
            {
                fleet.Draw(gameTime, m_spriteBatch);
            }

            m_spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive);
            foreach (ParticleEffect explosion in m_explosions)
            {
                explosion.Draw(gameTime, m_spriteBatch);
            }
            m_spriteBatch.End();

            foreach (Player player in m_players)
                player.Draw(gameTime);
        }

        public void BackupConfiguration()
        {
            m_movingPlanets = false;
            foreach (Planet planet in m_planets)
            {
                planet.BackupConfiguration();
                if (planet.Velocity != Vector2.Zero)
                    m_movingPlanets = true;
            }
        }

        public void RestoreConfiguration()
        {
            foreach (Planet planet in m_planets)
                planet.RestoreConfiguration();
            m_fleets.Clear();
        }

        public void ClearPlayers()
        {
            m_players.Clear();
        }

        public void AddPlayer(Player player)
        {
            player.Space = this;
            m_players.Add(player);
        }

        public void ClearPlanets()
        {
            m_planets.Clear();
        }

        public void AddPlanet(Planet planet)
        {
            m_planets.Add(planet);
        }

        public Planet GetPlanet(int index)
        {
            return m_planets[index];
        }

        public int GetPlanetIndex(Planet planet)
        {
            return m_planets.IndexOf(planet);
        }

        public void ClearFleets()
        {
            m_fleets.Clear();
        }

        public void AddFleet(Fleet fleet)
        {
            m_fleets.Add(fleet);
        }

        public void AddParticleEffect(Vector2 source, Texture2D particleTexture, Color colour)
        {
            ParticleEffect explosion = new ParticleEffect(this, source, particleTexture, colour);
            m_explosions.Add(explosion);
        }

        public void CheckPlayerElimination(Player player)
        {
            if (ActivePlayers.Where(x => x == player).Count() > 0)
                return;

            if (m_playerEliminatedEventHandler != null)
                m_playerEliminatedEventHandler(this, new PlayerEventArgs(player));
        }

        #endregion Public Methods

        #region Protected Methods

        protected override void LoadContent()
        {
            string backgroundAsset = @"Graphics\GalaxyBackground" + m_backgroundIndex;
            m_backgroundTexture = Game.Content.Load<Texture2D>(backgroundAsset);
        }

        #endregion Protected Methods

        #region Public Events

        public event EventHandler<PlanetEventArgs> PlanetCaptured
        {
            add { m_planetCapturedEventHandler += value; }
            remove { m_planetCapturedEventHandler -= value; }
        }

        public event EventHandler<PlayerEventArgs> PlayerEliminated
        {
            add { m_playerEliminatedEventHandler += value; }
            remove { m_playerEliminatedEventHandler -= value; }
        }

        #endregion Public Events

        #region Public Properties

        public int BackgroundIndex
        {
            get { return m_backgroundIndex; }
            set { m_backgroundIndex = value % Backgrounds; }
        }

        public IEnumerable<Player> Players
        {
            get { return m_players; }
        }

        public IEnumerable<Player> ActivePlayers
        {
            get
            {
                IEnumerable<Player> activeOccupants = m_planets.Where(x => x.Player != null).Select(x => x.Player);
                IEnumerable<Player> activePilots = m_fleets.Where(x => x.Player != null).Select(x => x.Player);
                return activeOccupants.Union(activePilots).Distinct();
            }
        }

        public IEnumerable<Planet> Planets
        {
            get { return m_planets; }
        }

        public IEnumerable<Fleet> Fleets
        {
            get { return m_fleets; }
        }

        public int ActiveShipCount
        {
            get { return m_fleets.Sum(x => x.ActiveShipCount); }
        }

        #endregion Public Properties

        #region Public Static Readonly Fields

        public static readonly Vector2 PlayAreaSize = new Vector2(PlayAreaWidth, PlayAreaHeight);

        #endregion Public Static Readonly Fields

        #region Private Constants

        private const int Backgrounds = 4;

        #endregion

        #region Private Methods

        private void HandlePlanetCollisions()
        {
            for (int index1 = 0; index1 < m_planets.Count; index1++)
            {
                Planet planet1 = m_planets[index1];
                for (int index2 = index1 + 1; index2 < m_planets.Count; index2++)
                {
                    Planet planet2 = m_planets[index2];

                    // skip if not colliding
                    if (!planet1.InCollisionWith(planet2))
                        continue;

                    Vector2 collisionNormal = planet2.Position - planet1.Position;
                    collisionNormal.Normalize();

                    Vector2 relativeVelocity = planet2.Velocity - planet1.Velocity;
                    float relativeNormalSpeed = Vector2.Dot(relativeVelocity, collisionNormal);

                    //skip if already moving apart
                    if (relativeNormalSpeed >= 0.0f)
                        continue;

                    float impulseMagnitude = -relativeNormalSpeed * 2.0f * planet1.Mass * planet2.Mass
                        / (planet1.Mass + planet2.Mass);

                    Vector2 impulse = impulseMagnitude * collisionNormal;

                    planet1.Velocity -= impulse / planet1.Mass;
                    planet2.Velocity += impulse / planet2.Mass;

                    // randomise rotation on collision
                    planet1.Rotation = (float)(m_random.NextDouble() - 0.5) * MathHelper.PiOver2;
                    planet1.Rotation = (float)(m_random.NextDouble() - 0.5) * MathHelper.PiOver2;
                }
            }
        }

        private int Clamp(int value, int min, int max)
        {
            return Math.Min(Math.Max(value, min), max);
        }

        private void UpdateSectors()
        {
            for (int sectorY = 0; sectorY < SectorsDown; sectorY++)
                for (int sectorX = 0; sectorX < SectorsAcross; sectorX++)
                {
                    Sector sector =  m_sectors[sectorX, sectorY];
                    sector.Planets.Clear();
                    sector.Ships.Clear();
                }

            foreach (Planet planet in m_planets)
            {
                int minX = Clamp((int)(planet.Position.X - planet.Radius) / SectorSize, 0, SectorsAcross - 1);
                int minY = Clamp((int)(planet.Position.Y - planet.Radius) / SectorSize, 0, SectorsDown - 1);
                int maxX = Clamp((int)(planet.Position.X + planet.Radius) / SectorSize, 0, SectorsAcross - 1);
                int maxY = Clamp((int)(planet.Position.Y + planet.Radius) / SectorSize, 0, SectorsDown - 1);

                for (int sectorY = minY; sectorY <= maxY; sectorY++)
                    for (int sectorX = minX; sectorX <= maxX; sectorX++)
                        m_sectors[sectorX, sectorY].Planets.Add(planet);
            }

            foreach (Fleet fleet in m_fleets)
            {
                foreach (Ship ship in fleet.Ships)
                {
                    int minX = Clamp((int)(ship.Position.X - Ship.Radius) / SectorSize, 0, SectorsAcross - 1);
                    int minY = Clamp((int)(ship.Position.Y - Ship.Radius) / SectorSize, 0, SectorsDown - 1);
                    int maxX = Clamp((int)(ship.Position.X + Ship.Radius) / SectorSize, 0, SectorsAcross - 1);
                    int maxY = Clamp((int)(ship.Position.Y + Ship.Radius) / SectorSize, 0, SectorsDown - 1);

                    for (int sectorY = minY; sectorY <= maxY; sectorY++)
                        for (int sectorX = minX; sectorX <= maxX; sectorX++)
                            m_sectors[sectorX, sectorY].Ships.Add(ship);
                }
            }
        }

        private void HandlePlanetShipCollisions()
        {
            for (int sectorY = 0; sectorY < SectorsDown; sectorY++)
                for (int sectorX = 0; sectorX < SectorsAcross; sectorX++)
                {
                    Sector sector = m_sectors[sectorX, sectorY];

                    foreach (Ship ship in sector.Ships)
                    {
                        bool removeShip = false;

                        /// collisions
                        foreach (Planet planet in sector.Planets)
                        {
                            Vector2 shipOffset = ship.Position - planet.Position;
                            float collisionRadiusSquared = planet.Radius + Ship.Radius;
                            collisionRadiusSquared *= collisionRadiusSquared;
                            if (shipOffset.LengthSquared() > collisionRadiusSquared)
                                continue;

                            if (planet == ship.Fleet.TargetPlanet)
                            {
                                // battle here

                                Player oldPlayer = planet.Player;
                                planet.AbsorbShip(ship.Fleet, ship.Position);
                                removeShip = true;

                                if (planet.Player != oldPlayer && m_planetCapturedEventHandler != null)
                                    m_planetCapturedEventHandler(this, new PlanetEventArgs(planet));

                                // no more testing against other planets
                                break;
                            }
                            else
                            {
                                // circle around non-target planets
                                Vector2 normal = shipOffset;
                                normal.Normalize();
                                float normalSpeed = Vector2.Dot(ship.Velocity, normal);
                                if (normalSpeed < 0.0f)
                                {
                                    Vector2 normalVelocity = normal * normalSpeed;
                                    Vector2 newVelocity = ship.Velocity - normalVelocity;
                                    newVelocity.Normalize();
                                    newVelocity *= Ship.TargetSpeed;
                                    ship.Velocity = newVelocity;
                                }
                            }
                        }

                        if (removeShip)
                            ship.Fleet.Ships.Remove(ship);
                    }

                }
        }

        #endregion Private Methods

        #region Private Fields

        private GalacticOverlordGame m_galacticOverlordGame;

        private int m_backgroundIndex;
        private List<Player> m_players;
        private List<Fleet> m_fleets;
        private List<Planet> m_planets;
        private List<ParticleEffect> m_explosions;

        private bool m_movingPlanets;
        private Sector[,] m_sectors = new Sector[SectorsAcross, SectorsDown];

        private Random m_random;

        private event EventHandler<PlanetEventArgs> m_planetCapturedEventHandler;
        private event EventHandler<PlayerEventArgs> m_playerEliminatedEventHandler;

        private SpriteBatch m_spriteBatch;
        private Texture2D m_backgroundTexture;

        #endregion Private Fields

        #region Private Constants

        private const int PlayAreaWidth = 480;
#if AD_DUPLEX
        private const int PlayAreaHeight = 720;
#else
        private const int PlayAreaHeight = 800;
#endif
        private const int SectorSize = 80;
        private const int SectorsAcross = PlayAreaWidth / SectorSize;
        private const int SectorsDown = PlayAreaHeight / SectorSize;

        #endregion Private Constants

        private class Sector
        {
            public Sector()
            {
                Planets = new List<Planet>();
                Ships = new List<Ship>();
            }

            public void Clear()
            {
                Planets.Clear();
                Ships.Clear();
            }

            public List<Planet> Planets;
            public List<Ship> Ships;
        }
    }
}
