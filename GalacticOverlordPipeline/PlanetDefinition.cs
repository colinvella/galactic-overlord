using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;

namespace GalacticOverlord.Pipeline
{
    public class PlanetDefinition
    {
        #region Public Methods

        public PlanetDefinition()
        {
        }

        #endregion

        #region Public Properties

        public string Name
        {
            get { return m_name; }
            set { m_name = value; }
        }

        public Vector2 Position
        {
            get { return m_position; }
            set { m_position = value; }
        }

        [ContentSerializer(Optional = true)]
        public float Orientation
        {
            get { return m_orientation; }
            set { m_orientation = value; }
        }

        [ContentSerializer(Optional=true)]
        public Vector2 Velocity
        {
            get { return m_velocity; }
            set { m_velocity = value; }
        }

        [ContentSerializer(Optional = true)]
        public float Rotation
        {
            get { return m_rotation; }
            set { m_rotation = value; }
        }

        public float Radius
        {
            get { return m_radius; }
            set { m_radius = value; }
        }

        public int Population
        {
            get { return m_population; }
            set { m_population = value; }
        }

        public PlayerId Owner
        {
            get { return m_owner; }
            set { m_owner = value; }
        }

        #endregion

        #region Private Fields

        private string m_name;
        private Vector2 m_position;
        private float m_orientation;
        private Vector2 m_velocity;
        private float m_rotation;
        private float m_radius;
        private int m_population;
        private PlayerId m_owner;

        #endregion
    }
}
