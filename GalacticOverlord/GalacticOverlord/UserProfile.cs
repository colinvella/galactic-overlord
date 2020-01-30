using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.IsolatedStorage;
using System.IO;
using Microsoft.Xna.Framework.GamerServices;
using System.Threading;
using GalacticOverlord.Pipeline;
using GalacticOverlord.GameStates;

namespace GalacticOverlord
{
    public class UserProfile
    {
        #region Public Methods

        public UserProfile()
        {
            m_playerId = "Player" + new Random().Next(100000, 1000000);
            m_difficultyLevel = DifficultyLevel.Normal;

            m_musicEnabled = m_sfxEnabled = true;

            m_campaignProgress = 0;
            m_multiplayerVictories = 0;
            m_skirmishVictories = new Dictionary<string, int>();
            foreach (string skirmishKey in GenerateSkirmishKeys())
                m_skirmishVictories[skirmishKey] = 0;

            LoadProperties();

#if AD_DUPLEX
            m_isTrialMode = false;
#else
            m_isTrialMode = Guide.IsTrialMode;

            Thread trialThread = new Thread(CheckTrial);
            trialThread.IsBackground = true;
            trialThread.Start();
            while (!trialThread.IsAlive) ;
#endif
        }

        public void LoadProperties()
        {
            IsolatedStorageFile isolatedStorageFile = IsolatedStorageFile.GetUserStoreForApplication();

            if (!isolatedStorageFile.FileExists(ProfileFilename))
                StoreProperties();

            IEnumerable<string> skirmishKeys = GenerateSkirmishKeys();

            try
            {
                FileStream fileStream = isolatedStorageFile.OpenFile(ProfileFilename, FileMode.Open);
                BinaryReader binaryReader = new BinaryReader(fileStream);

                m_playerId = binaryReader.ReadString();
                m_difficultyLevel = (DifficultyLevel)binaryReader.ReadByte();

                m_musicEnabled = binaryReader.ReadBoolean();
                m_sfxEnabled = binaryReader.ReadBoolean();

                m_campaignProgress = binaryReader.ReadInt16();
                m_multiplayerVictories = binaryReader.ReadInt16();
                foreach (string skirmishKey in skirmishKeys)
                    m_skirmishVictories[skirmishKey] = binaryReader.ReadInt16();

                binaryReader.Close();
            }
            catch
            {
                // don't crash game - just ignore file if problematic
                m_playerId = "Player" + new Random().Next(100000, 1000000);
                m_difficultyLevel = DifficultyLevel.Normal;

                m_musicEnabled = true;
                m_sfxEnabled = true;

                m_campaignProgress = 0;
                m_multiplayerVictories = 0;
                foreach (string skirmishKey in skirmishKeys)
                    m_skirmishVictories[skirmishKey] = 0;
            }
        }

        public void StoreProperties()
        {
            IEnumerable<string> skirmishKeys = GenerateSkirmishKeys();

            try
            {
                IsolatedStorageFile isolatedStorageFile = IsolatedStorageFile.GetUserStoreForApplication();
                FileStream fileStream = isolatedStorageFile.OpenFile(ProfileFilename, FileMode.Create);
                BinaryWriter binaryWriter = new BinaryWriter(fileStream);

                binaryWriter.Write(m_playerId);
                binaryWriter.Write((byte)m_difficultyLevel);

                binaryWriter.Write(m_musicEnabled);
                binaryWriter.Write(m_sfxEnabled);

                binaryWriter.Write((Int16)m_campaignProgress);
                binaryWriter.Write((Int16)m_multiplayerVictories);
                foreach (string skirmishKey in skirmishKeys)
                    binaryWriter.Write((Int16)m_skirmishVictories[skirmishKey]);

                binaryWriter.Flush();
                binaryWriter.Close();
            }
            catch
            {
                // do nothing if storage fails
            }
        }

        public int GetSkirmishVictories(SkirmishMode skirmishMode, DifficultyLevel difficultyLevel)
        {
            return m_skirmishVictories[GenerateSkirmishKey(skirmishMode, difficultyLevel)];
        }

        public void AddSkirmishVictory(SkirmishMode skirmishMode, DifficultyLevel difficultyLevel)
        {
            ++m_skirmishVictories[GenerateSkirmishKey(skirmishMode, difficultyLevel)];
        }

        #endregion

        #region Public Properties

        public bool IsTrialMode
        {
            get { return m_isTrialMode; }
        }

        public string PlayerId
        {
            get { return m_playerId; }
            set { m_playerId = value; }
        }

        public DifficultyLevel Difficulty
        {
            get { return m_difficultyLevel; }
            set { m_difficultyLevel = value; }
        }

        public bool MusicEnabled
        {
            get { return m_musicEnabled; }
            set { m_musicEnabled = value; }
        }

        public bool SfxEnabled
        {
            get { return m_sfxEnabled; }
            set { m_sfxEnabled = value; }
        }

        public int CampaignProgress
        {
            get { return m_campaignProgress; }
            set { m_campaignProgress = value; }
        }

        public int MultiplayerVictories
        {
            get { return m_multiplayerVictories; }
            set { m_multiplayerVictories = value; }
        }

        #endregion

        #region Private Methods

        private string GenerateSkirmishKey(SkirmishMode skirmishMode, DifficultyLevel difficultyLevel)
        {
            return skirmishMode + "-" + difficultyLevel;
        }

        private IEnumerable<string> GenerateSkirmishKeys()
        {
            List<SkirmishMode> skirmishModes = new List<SkirmishMode>();
            skirmishModes.Add(SkirmishMode.Duel);
            skirmishModes.Add(SkirmishMode.ThreeWay);
            skirmishModes.Add(SkirmishMode.Cloaked);
            skirmishModes.Add(SkirmishMode.Asteroids);

            List<DifficultyLevel> difficultyLevels = new List<DifficultyLevel>();
            difficultyLevels.Add(DifficultyLevel.Newbie);
            difficultyLevels.Add(DifficultyLevel.Easy);
            difficultyLevels.Add(DifficultyLevel.Normal);
            difficultyLevels.Add(DifficultyLevel.Hard);
            difficultyLevels.Add(DifficultyLevel.Extreme);
            difficultyLevels.Add(DifficultyLevel.Impossible);

            List<string> skirmishKeys = new List<string>();
            foreach (SkirmishMode skirmishMode in skirmishModes)
                foreach (DifficultyLevel difficultyLevel in difficultyLevels)
                    skirmishKeys.Add(GenerateSkirmishKey(skirmishMode, difficultyLevel));
            return skirmishKeys;
        }

        private void CheckTrial()
        {
            // Stop the thread once we are no longer in trial since the state can't change anymore
            while (m_isTrialMode)
            {
                m_isTrialMode = Guide.IsTrialMode;

                // Sleep if the game is in trial 
                if (m_isTrialMode)
                {
                    Thread.Sleep(TimeSpan.FromSeconds(1));
                }
            }
        }

        #endregion

        #region Private Constants

        private const string OldOptionsFilename = "GalacticOverloard.options";
        private const string ProfileFilename = "GalacticOverloard.profile";

        #endregion

        #region Private Fields

        private bool m_isTrialMode;
        private DifficultyLevel m_difficultyLevel;
        private bool m_musicEnabled;
        private bool m_sfxEnabled;
        private string m_playerId;
        private int m_campaignProgress;
        private int m_multiplayerVictories;
        private Dictionary<string, int> m_skirmishVictories;

        #endregion
    }
}
