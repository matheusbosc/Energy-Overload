using System;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

namespace com.matheusbosc.energyoverload
{
    public class AudioSettings : MonoBehaviour
    {
        public Slider masterSlider, sfxSlider, musicSlider;
        public AudioMixer mixer;

        [HideInInspector] public AudioSource currentlyActiveAudio, audioToFadeIn, audioToFadeOut;

        private bool isFadingIn = false, isFadingOut = false;

        private void Update()
        {
            if (masterSlider.value != 40)
            {
                mixer.SetFloat("MasterVolume", (0-masterSlider.value));
            }
            else
            {
                mixer.SetFloat("MasterVolume", -80);
            }

            if (musicSlider.value != 40)
            {
                mixer.SetFloat("MusicVolume", (0-musicSlider.value));
            }
            else
            {
                mixer.SetFloat("MusicVolume", -80);
            }

            if (sfxSlider.value != 40)
            {
                mixer.SetFloat("SFXVolume", (0-sfxSlider.value));
            }
            else
            {
                mixer.SetFloat("SFXVolume", -80);
            }
            if (isFadingOut)
            {
                if (audioToFadeOut.volume < 0.01)
                {
                    audioToFadeOut.Stop();
                    audioToFadeOut.volume = 0;
                    isFadingOut = false;
                }
                else
                {
                    float newVolume = audioToFadeOut.volume - (1f * Time.deltaTime);  //change 0.01f to something else to adjust the rate of the volume dropping
                    if (newVolume < 0f)
                    {
                        newVolume = 0f;
                    }
                    audioToFadeOut.volume = newVolume;
                }
            }
            
            if (isFadingIn)
            {
                if (audioToFadeIn.volume > 0.99)
                {
                    audioToFadeIn.volume = 1;
                    isFadingIn = false;
                }
                else
                {
                    float newVolume = audioToFadeIn.volume + (1f * Time.deltaTime);  //change 0.01f to something else to adjust the rate of the volume dropping
                    if (newVolume > 1f)
                    {
                        newVolume = 1f;
                    }
                    audioToFadeIn.volume = newVolume;
                }
            }
        }

        public void FadeIn(AudioSource audio)
        {
            isFadingIn = true;
            currentlyActiveAudio = audio;
            audioToFadeIn = audio;
            audioToFadeIn.volume = 0;
            audioToFadeIn.Play();
        }
        
        public void FadeOutCurrentAndInNew(AudioSource audio)
        {
            FadeOut(currentlyActiveAudio);
            FadeIn(audio);
        }
        
        public void FadeOut(AudioSource audio)
        {
            isFadingOut = true;
            audioToFadeOut = audio;
            audioToFadeOut.volume = 1;
        }
    }
}