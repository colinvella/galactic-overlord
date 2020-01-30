using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using GalacticOverlord.Core;
using GalacticOverlord.Pipeline;

namespace GalacticOverlord.Players
{
    public class ComputerPlayer: Player
    {
        public ComputerPlayer(DifficultyLevel difficultyLevel, Color colour, bool cloaked)
            :base(PlayerType.Computer, colour, cloaked)
        {
            m_difficultyLevel = difficultyLevel;

            switch (m_difficultyLevel)
            {
                case DifficultyLevel.Newbie:
                    m_maxFleets = 1;
                    m_launchInterval = 10.0f;
                    break;
                case DifficultyLevel.Easy:
                    m_maxFleets = 1;
                    m_launchInterval = 8.0f;
                    break;
                case DifficultyLevel.Normal:
                    m_maxFleets = 1;
                    m_launchInterval = 6.0f;
                    break;
                case DifficultyLevel.Hard:
                    m_maxFleets = 2;
                    m_launchInterval = 4.0f;
                    break;
                case DifficultyLevel.Extreme:
                    m_maxFleets = 2;
                    m_launchInterval = 2.0f;
                    break;
                case DifficultyLevel.Impossible:
                    m_maxFleets = 4;
                    m_launchInterval = 1.0f;
                    break;
            }

            m_launchTimer = m_launchInterval;
        }

        public override void Update(GameTime gameTime)
        {
            // artificial intelligence logic

            // don't do anything until assigned to space
            if (Space == null)
                return;

            // launch timer
            m_launchTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (m_launchTimer > 0.0f)
                return;

            // reload timer
            m_launchTimer += m_launchInterval;

            // launch only a limited number of fleets
            if (Space.Fleets.Count(x => x.Player == this) >= m_maxFleets)
                return;

            // don't do anything if there are no other active players
            if (Space.ActivePlayers.Count(x => x != this) == 0)
                return;

            // determine owned planets
            IEnumerable<Planet> ownedPlanets = Space.Planets.Where(x => x.Player == this);

            // compute score for most suitable move
            float bestScore = 0.0f;
            Planet bestSource = null, bestTarget = null;
            foreach (Planet ownedPlanet in ownedPlanets)
            {
                IEnumerable<Planet> otherPlanets = Space.Planets.Where(x => x != ownedPlanet);
                foreach (Planet otherPlanet in otherPlanets)
                {
                    // predict other planet's owner and population,
                    // given current fleet movements
                    Player otherPlayer = null;
                    float otherPopulation = 0.0f;
                    PredictPlanetFuture(otherPlanet, out otherPlayer, out otherPopulation);

                    // more likely if planet is large (quick population regrowth)
                    float score = ownedPlanet.Radius;

                    // more likely for larger populations (leaves larger defense behind)
                    score *= ownedPlanet.Population;

                    // more likely if other planet is larger (future growth)
                    score *= otherPlanet.Radius;

                    // less likely if other population larger (more losses)
                    score /= (1.0f + otherPopulation);

                    // more likely if planet is closer
                    float distance = (otherPlanet.Position - ownedPlanet.Position).Length();
                    score /= distance;

                    // more likely if other planet is unconquered
                    if (otherPlayer == null)
                        score *= 4.0f;

                    // less likely for planet reinforcement
                    else if (otherPlayer == this)
                        score *= 0.25f;
                    // otherwise it's an attack
                    else if (otherPlayer != this)
                        score *= 1.0f;

                    // determine if this candidate move is better
                    if (score > bestScore)
                    {
                        bestSource = ownedPlanet;
                        bestTarget = otherPlanet;
                        bestScore = score;
                    }
                }
            }

            if (bestSource != null && bestTarget != null)
                SendFleet(bestSource, bestTarget, 0.65f);
        }

        public override void Draw(GameTime gameTime)
        {
        }

        private void PredictPlanetFuture(Planet planet, out Player predictedPlayer, out float predictedPopulation)
        {
            // start with current owner and population
            predictedPlayer = planet.Player;
            predictedPopulation = planet.Population;

            // consider all inbound fleets
            foreach (Fleet fleet in Space.Fleets.Where(x => x.TargetPlanet == planet))
            {
                if (fleet.Player == planet.Player)
                {
                    // if friendly fleet, increase population
                    predictedPopulation += fleet.Size;
                }
                else
                {
                    // otherwise if different player or planet unoccupied,
                    // reduce population
                    predictedPopulation -= fleet.Size;

                    if (predictedPopulation == 0.0f)
                    {
                        // if population annihilated exactly (unlikely!),
                        // planet is no longer ruled by any player
                        predictedPlayer = null;
                    }
                    else if (predictedPopulation < 0.0f)
                    {
                        // if negative population, invading fleet conquers planet
                        // and negative value indicates surviving victors (once inverted)
                        predictedPopulation = -predictedPopulation;
                        predictedPlayer = fleet.Player;
                    }
                }
            }
        }

        private DifficultyLevel m_difficultyLevel;
        private int m_maxFleets;
        private float m_launchInterval;
        private float m_launchTimer;
    }
}
