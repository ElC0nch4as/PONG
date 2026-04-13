using UnityEngine;

public class MusicPhase2 : MonoBehaviour
{
    [Header("Audio Sources")]
    [SerializeField] private AudioSource normalMusic;
    [SerializeField] private AudioSource phase2Music;

    public void PlayNormal()
    {
        StopSource(phase2Music);
        PlaySource(normalMusic);
    }

    public void PlayPhase2()
    {
        StopSource(normalMusic);
        PlaySource(phase2Music);
    }

    public void StopAllMusic()
    {
        StopSource(normalMusic);
        StopSource(phase2Music);
    }

    public void StopNormalOnly()
    {
        if (normalMusic != null)
        {
            normalMusic.Stop();
            normalMusic.time = 0f;
        }
    }

    private void PlaySource(AudioSource source)
    {
        if (source == null)
        {
            return;
        }

        source.time = 0f;
        source.Play();
    }

    private void StopSource(AudioSource source)
    {
        if (source == null)
        {
            return;
        }

        source.Stop();
        source.time = 0f;
    }
}