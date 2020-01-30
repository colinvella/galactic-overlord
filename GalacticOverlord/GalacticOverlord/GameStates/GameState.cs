using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace GalacticOverlord.GameStates
{
    public abstract class GameState: ModularGameComponent
    {
        public GameState(GalacticOverlordGame galacticOverlordGame)
            : base(galacticOverlordGame)
        {
        }

        public void SwitchToState(GameState gameState)
        {
            UnloadContent();
            Shutdown();
            Game.Components.Remove(this);
            Game.Components.Add(gameState);
        }

    }
}
