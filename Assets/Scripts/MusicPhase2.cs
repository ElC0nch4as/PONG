using UnityEngine;

public class MusicPhase2 : MonoBehaviour
{
    [Header("Music Source")]
    [SerializeField] private AudioSource musicSource;

    [Header("Playlists")]
    [SerializeField] private AudioClip[] normalPlaylist;
    [SerializeField] private AudioClip[] phase2Playlist;

    [Header("SFX Source ")]
    [SerializeField] private AudioSource sfxSource;

    [Header("SFX Clips")]
    [SerializeField] private AudioClip gameStartSfx;
    [SerializeField] private AudioClip pointSfx;
    [SerializeField] private AudioClip phase2ExtraSfx; // el “otro mas” que quieres después del expand

    [Header("Options")]
    [SerializeField] private bool avoidRepeatingLast = true;

    private int _lastNormalIndex = -1;
    private int _lastPhase2Index = -1;

    // ---------------- MUSIC ----------------

    public void PlayNormal()
    {
        PlayRandomFrom(normalPlaylist, ref _lastNormalIndex);
    }

    public void PlayPhase2()
    {
        PlayRandomFrom(phase2Playlist, ref _lastPhase2Index);
    }

    public void StopAllMusic()
    {
        if (musicSource == null) return;
        musicSource.Stop();
        musicSource.clip = null;
        musicSource.time = 0f;
    }

    // esto es lo que ya estabas usando en Phase2Routine
    public void StopNormalOnly()
    {
        StopAllMusic();
    }

    // Llamar esto cuando se te acabe una canción (o si quieres forzar “siguiente”)
    public void PlayNextNormal()
    {
        PlayNormal();
    }

    public void PlayNextPhase2()
    {
        PlayPhase2();
    }

    private void PlayRandomFrom(AudioClip[] list, ref int lastIndex)
    {
        if (musicSource == null || list == null || list.Length == 0) return;

        int idx = Random.Range(0, list.Length);
        if (avoidRepeatingLast && list.Length > 1)
        {
            while (idx == lastIndex)
                idx = Random.Range(0, list.Length);
        }

        lastIndex = idx;

        musicSource.Stop();
        musicSource.clip = list[idx];
        musicSource.loop = false;
        musicSource.time = 0f;
        musicSource.Play();

        CancelInvoke(nameof(CheckMusicEnd));
        InvokeRepeating(nameof(CheckMusicEnd), 0.25f, 0.25f);
    }

    private void CheckMusicEnd()
    {
        if (musicSource == null) return;

        // si ya no está sonando y sí había clip, ponemos otra random del mismo “modo”
        if (!musicSource.isPlaying && musicSource.clip != null)
        {
            // No sabemos si era normal o fase2 por el clip, así que:
            // - si el clip está dentro de phase2Playlist => siguiente phase2
            // - si no => siguiente normal
            if (IsInList(musicSource.clip, phase2Playlist)) PlayPhase2();
            else PlayNormal();
        }
    }

    private bool IsInList(AudioClip c, AudioClip[] list)
    {
        if (c == null || list == null) return false;
        for (int i = 0; i < list.Length; i++)
            if (list[i] == c) return true;
        return false;
    }

    // ---------------- SFX ----------------

    public void PlayGameStartSfx()
    {
        PlaySfx(gameStartSfx);
    }

    public void PlayPointSfx()
    {
        PlaySfx(pointSfx);
    }

    public void PlayPhase2ExtraSfx()
    {
        PlaySfx(phase2ExtraSfx);
    }

    private void PlaySfx(AudioClip clip)
    {
        if (sfxSource == null || clip == null) return;
        sfxSource.PlayOneShot(clip);
    }
}