using System.Collections.Generic;
using UnityEngine;

public class PongMusicManager : MonoBehaviour
{
    public enum PlaylistMode { Tron, Phase2, TwoVsTwo }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource ambienceSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Playlists")]
    [SerializeField] private List<AudioClip> tronTracks = new();
    [SerializeField] private List<AudioClip> phase2Tracks = new();
    [SerializeField] private List<AudioClip> twoVsTwoTracks = new();

    [Header("Ambience (Crowd)")]
    [SerializeField] private AudioClip crowdLoop;

    [Header("SFX")]
    [SerializeField] private AudioClip pointSfx;

    [Header("Options")]
    [SerializeField] private bool avoidRepeat = true;

    private PlaylistMode _currentMode;
    private int _lastIndex = -1;

    private void Awake()
    {
        if (musicSource != null) musicSource.loop = false;

        if (ambienceSource != null)
        {
            ambienceSource.loop = true;
            ambienceSource.clip = crowdLoop;
        }
    }

    private void Update()
    {
        if (musicSource == null) return;
        if (musicSource.clip == null) return;

        if (!musicSource.isPlaying && Time.timeScale > 0f)
        {
            PlayNextFromCurrentPlaylist();
        }
    }

    public void PlayTron()
    {
        _currentMode = PlaylistMode.Tron;
        PlayRandomFrom(tronTracks);
        StartAmbience();
    }

    public void PlayPhase2()
    {
        _currentMode = PlaylistMode.Phase2;
        PlayRandomFrom(phase2Tracks);
        StartAmbience();
    }

    public void PlayTwoVsTwo()
    {
        _currentMode = PlaylistMode.TwoVsTwo;
        PlayRandomFrom(twoVsTwoTracks.Count > 0 ? twoVsTwoTracks : phase2Tracks);
        StartAmbience();
    }

    public void StopMusicOnly()
    {
        if (musicSource == null) return;
        musicSource.Stop();
        musicSource.clip = null;
    }

    public void StopAmbienceOnly()
    {
        if (ambienceSource == null) return;
        ambienceSource.Stop();
    }

    public void StopMusicAndAmbience()
    {
        StopMusicOnly();
        StopAmbienceOnly();
    }

    public void ResumeAmbience()
    {
        StartAmbience();
    }

    public void PlayPointSfx()
    {
        PlayOneShot(pointSfx);
    }
    private void StartAmbience()
    {
        if (ambienceSource == null) return;
        if (crowdLoop == null) return;

        ambienceSource.clip = crowdLoop;

        if (!ambienceSource.isPlaying)
            ambienceSource.Play();
    }

    private void PlayNextFromCurrentPlaylist()
    {
        switch (_currentMode)
        {
            case PlaylistMode.Tron: PlayRandomFrom(tronTracks); break;
            case PlaylistMode.Phase2: PlayRandomFrom(phase2Tracks); break;
            case PlaylistMode.TwoVsTwo:
                PlayRandomFrom(twoVsTwoTracks.Count > 0 ? twoVsTwoTracks : phase2Tracks);
                break;
        }
    }

    private void PlayRandomFrom(List<AudioClip> list)
    {
        if (musicSource == null) return;
        if (list == null || list.Count == 0) return;

        int idx = Random.Range(0, list.Count);

        if (avoidRepeat && list.Count > 1)
        {
            int guard = 0;
            while (idx == _lastIndex && guard < 20)
            {
                idx = Random.Range(0, list.Count);
                guard++;
            }
        }

        _lastIndex = idx;

        musicSource.clip = list[idx];
        musicSource.time = 0f;
        musicSource.Play();
    }

    private void PlayOneShot(AudioClip clip)
    {
        if (sfxSource == null || clip == null) return;
        sfxSource.PlayOneShot(clip);
    }
}