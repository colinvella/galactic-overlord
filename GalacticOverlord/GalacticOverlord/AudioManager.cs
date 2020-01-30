using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Media;

namespace GalacticOverlord
{
    public class AudioManager
    {
        public AudioManager(UserProfile userProfile)
        {
            m_userProfile = userProfile;

            m_soundEffectInstances = new List<SoundEffectInstance>(MaxSoundEffects);
        }

        public void PlayMusic(SoundEffect music)
        {
            PlayMusic(music.CreateInstance());
        }

        public void PlayMusic(SoundEffectInstance music)
        {
            if (!m_userProfile.MusicEnabled)
                return;

            if (!MediaPlayer.GameHasControl)
                return;

            if (m_music == music)
                return;

            if (m_music != null)
                m_music.Stop();

            m_music = music;
            m_music.IsLooped = true;
            m_music.Play();
        }

        public void StopMusic()
        {
            if (m_music != null)
            {
                m_music.Stop();
                m_music = null;
            }
        }

        public SoundEffectInstance PlaySound(SoundEffectInstance soundEffectInstance)
        {
            if (!m_userProfile.SfxEnabled)
                return null;

            // clean effects that have stopped playing
            for (int index = 0; index < m_soundEffectInstances.Count; )
            {
                if (m_soundEffectInstances[index].State == SoundState.Stopped)
                    m_soundEffectInstances.RemoveAt(index);
                else
                    ++index;
            }

            if (m_soundEffectInstances.Count >= MaxSoundEffects)
                return null;

            soundEffectInstance.Play();
            m_soundEffectInstances.Add(soundEffectInstance);
            return soundEffectInstance;
        }

        public SoundEffectInstance PlaySound(SoundEffect soundEffect)
        {
            if (!m_userProfile.SfxEnabled)
                return null;

            return PlaySound(soundEffect.CreateInstance());
        }

        public bool MusicPlaying
        {
            get
            {
                return !MediaPlayer.GameHasControl || m_music != null;
            }
        }

        private const int MaxSoundEffects = 15;

        private UserProfile m_userProfile;
        private SoundEffectInstance m_music;
        private List<SoundEffectInstance> m_soundEffectInstances;
        
    }
}
