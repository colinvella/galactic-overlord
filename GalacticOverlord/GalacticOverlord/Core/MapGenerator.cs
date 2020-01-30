using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using GalacticOverlord.Players;
using GalacticOverlord.Pipeline;

namespace GalacticOverlord.Core
{
    public class MapGenerator
    {
        public static void GenerateCampaignMap(Space space, Player campaignPlayer, CampaignDefinition campaignDefinition, int levelIndex)
        {
            LevelDefinition levelDefinition = campaignDefinition.Levels[levelIndex];

            space.BackgroundIndex = levelIndex;

            space.ClearFleets();
            space.ClearPlanets();
            space.ClearPlayers();

            Dictionary<PlayerId, Player> playerDicitonary = new Dictionary<PlayerId, Player>();
            playerDicitonary[PlayerId.None] = null;
            playerDicitonary[PlayerId.Player] = campaignPlayer;

            space.AddPlayer(campaignPlayer);
            foreach (EnemyDefinition enemyDefinition in levelDefinition.Enemies)
            {
                ComputerPlayer computerPlayer = new ComputerPlayer(
                    enemyDefinition.Difficulty, enemyDefinition.GetColour(), enemyDefinition.Cloaked);
                space.AddPlayer(computerPlayer);
                playerDicitonary[enemyDefinition.Id] = computerPlayer;
            }

            foreach (PlanetDefinition planetDefinition in levelDefinition.Planets)
            {
                Player ownerPlayer = playerDicitonary[planetDefinition.Owner];
                PlanetType planetType = (PlanetType)s_random.Next(4);
                float orientation = GenerateRandomValue(0.0f, MathHelper.TwoPi);

                Planet planet = new Planet(space,
                    planetDefinition.Name, planetDefinition.Position, planetDefinition.Radius,
                    planetType, orientation, ownerPlayer, planetDefinition.Population);
                planet.Velocity = planetDefinition.Velocity;
                planet.Rotation = MathHelper.ToRadians(planetDefinition.Rotation);
                space.AddPlanet(planet);

#if AD_DUPLEX
                Vector2 position = planet.Position;
                position.Y *= Space.PlayAreaSize.Y / 800.0f;
                planet.Position = position;
#endif
            }
        }

        public static void GenerateDuelMap(Space space,
            Player playerOne, Player playerTwo)
        {
            GenerateMultiWayMap(space, new Player[] { playerOne, playerTwo });
        }

        public static void GenerateThreeWayMap(Space space,
            Player playerOne, Player playerTwo, Player playerThree)
        {
            GenerateMultiWayMap(space, new Player[] { playerOne, playerTwo, playerThree });
        }

        public static void GenerateFourWayMap(Space space,
            Player playerOne, Player playerTwo, Player playerThree, Player playerFour)
        {
            GenerateMultiWayMap(space, new Player[] { playerOne, playerTwo, playerThree, playerFour });
        }

        public static void GenerateAsteroidsMap(Space space,
            Player playerOne, Player playerTwo)
        {
            GenerateDuelMap(space, playerOne, playerTwo);

            Vector2 minVelocity = new Vector2(-40.0f, -40.0f);
            Vector2 maxVelocity = -minVelocity;

            foreach (Planet planet in space.Planets)
                planet.Velocity = GenerateRandomValue(minVelocity, maxVelocity);
        }

        private static float GenerateRandomValue(float min, float max)
        {
            return min + (float)s_random.NextDouble() * (max - min); 
        }

        private static Vector2 GenerateRandomValue(Vector2 min, Vector2 max)
        {
            return new Vector2(GenerateRandomValue(min.X, max.X), GenerateRandomValue(min.Y, max.Y));
        }

        private static string GenerateRandomName(int length)
        {
            bool consonant = true;
            StringBuilder stringBuilder = new StringBuilder();
            while (length-- > 0)
            {
                char nextChar = consonant
                    ? s_consonants[s_random.Next(s_consonants.Length)]
                    : s_vowels[s_random.Next(s_vowels.Length)];

                if (stringBuilder.Length == 0)
                    nextChar = char.ToUpper(nextChar);

                stringBuilder.Append(nextChar);

                consonant = !consonant;
            }

            return stringBuilder.ToString(); ;
        }

        private static string GenerateRandomName(int minLength, int maxLength)
        {
            int length = s_random.Next(minLength, maxLength + 1);
            return GenerateRandomName(length);
        }

        private static Space GeneratePlanetLayout(Space space)
        {
            space.ClearFleets();
            space.ClearPlanets();
            space.ClearPlayers();

            int planetCount = 20;
            Vector2 minPosition = new Vector2(64.0f, 64.0f);
            Vector2 maxPosition = Space.PlayAreaSize - minPosition;
            while (planetCount-- > 0)
            {
                float radius = GenerateRandomValue(16.0f, 40.0f);

                // determine non-colliding position
                Vector2 position = Vector2.Zero;
                while (true)
                {
                    position = GenerateRandomValue(minPosition, maxPosition);

                    bool validPlanet = true;
                    foreach (Planet planet in space.Planets)
                    {
                        float distance = (planet.Position - position).Length();
                        if (distance < (planet.Radius + radius) * 1.2f)
                        {
                            validPlanet = false;
                            break;
                        }
                    }

                    if (validPlanet)
                        break;
                }

                string planetName = GenerateRandomName(3, 9);

                float orientation = GenerateRandomValue(0.0f, MathHelper.TwoPi);
                int population = s_random.Next(50);
                PlanetType planetType = (PlanetType)s_random.Next(4);

                // compute rotation from position to keep it deterministic for campaigns
                float rotation = position.X + position.Y;
                while (rotation >= MathHelper.PiOver4)
                    rotation -= MathHelper.PiOver2;
                while (rotation < -MathHelper.PiOver4)
                    rotation += MathHelper.PiOver2;

                Planet newPlanet = new Planet(space, planetName, position, radius, planetType, orientation, null, population);
                newPlanet.Rotation = rotation;
                space.AddPlanet(newPlanet);
            }

            return space;
        }

        private static void GenerateMultiWayMap(Space space,
            Player[] players)
        {
            GeneratePlanetLayout(space);

            foreach (Player player in players)
                space.AddPlayer(player);

            // place players at multiple extremes
            Vector2 orientation = GenerateRandomValue(Vector2.Zero, new Vector2(1.0f));
            orientation.Normalize();

            Vector2[] orientations = new Vector2[players.Length];
            float angle = (float)Math.Atan2(orientation.Y, orientation.X);
            float angleDelta = MathHelper.TwoPi / players.Length;
            for (int index = 0; index < players.Length; index++)
            {
                orientations[index] = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
                angle += angleDelta;
            }

            Planet[] playerPlanets = new Planet[players.Length];
            float[] projectionMax = new float[players.Length];
            for (int index = 0; index < players.Length; index++)
                projectionMax[index] = float.MinValue;

            foreach (Planet planet in space.Planets)
            {
                for (int index = 0; index < players.Length; index++)
                {
                    float projection = Vector2.Dot(planet.Position, orientations[index]);

                    if (playerPlanets[index] == null || projectionMax[index] < projection)
                    {
                        playerPlanets[index] = planet;
                        projectionMax[index] = projection;
                    }
                }
            }

            for (int index = 0; index < players.Length; index++)
            {
                playerPlanets[index].Player = players[index];
                playerPlanets[index].Radius = Math.Max(32.0f, playerPlanets[index].Radius);
                playerPlanets[index].Population = 100.0f;
            }
        }

        private static readonly Random s_random = new Random();

        private static readonly char[] s_consonants
            = { 'b', 'c', 'd', 'f', 'g', 'h', 'j', 'k', 'l', 'm',
                'n', 'p', 'q', 'r', 's', 't', 'v', 'w', 'x', 'y', 'z' };
        private static readonly char[] s_vowels = { 'a', 'e', 'i', 'o', 'u' };
    }
}
