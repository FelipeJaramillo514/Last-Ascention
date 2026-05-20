using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

public enum MusicTrackId
{
    None,
    Hub,
    DungeonFloor1,
    Boss
}

public enum AudioCueId
{
    FootstepA,
    FootstepB,
    FootstepC,
    FootstepD,
    SwordSwing,
    PlayerHit,
    DodgeRoll,
    PlayerDeath,
    LevelUp,
    ShadowExtract,
    AbilityUnlocked,
    Penalty,
    GoblinAttack,
    GoblinDeath,
    GiantImpact,
    BossRoar,
    BossCharge,
    CrystalFire,
    EnemyProjectile,
    WallImpact,
    DoorOpen,
    RiftHum,
    CrystalPickup
}

public class AudioManager : MonoBehaviour
{
    private const string MasterVolumeKey = "audio_master";
    private const string MusicVolumeKey = "audio_music";
    private const string SfxVolumeKey = "audio_sfx";

    public static AudioManager Instance { get; private set; }

    [Header("Mixer")]
    [SerializeField] private AudioMixer mixer;
    [SerializeField] private AudioMixerGroup masterGroup;
    [SerializeField] private AudioMixerGroup musicGroup;
    [SerializeField] private AudioMixerGroup sfxGroup;

    [Header("Sources")]
    [SerializeField] private int sfxPoolSize = 10;
    [SerializeField] private float defaultMusicVolume = 0.55f;
    [SerializeField] private float defaultSfxVolume = 0.9f;

    private readonly Dictionary<AudioCueId, AudioClip> cueClips = new Dictionary<AudioCueId, AudioClip>();
    private readonly Dictionary<MusicTrackId, AudioClip> musicClips = new Dictionary<MusicTrackId, AudioClip>();
    private readonly List<AudioSource> sfxSources = new List<AudioSource>();

    private AudioSource musicSourceA;
    private AudioSource musicSourceB;
    private bool useSourceA = true;
    private int nextSfxIndex;
    private Coroutine musicRoutine;
    private MusicTrackId currentTrack = MusicTrackId.None;
    private float masterVolume = 1f;
    private float musicVolume = 1f;
    private float sfxVolume = 1f;

    public float MasterVolume => masterVolume;
    public float MusicVolume => musicVolume;
    public float SfxVolume => sfxVolume;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (FindFirstObjectByType<AudioManager>() != null)
        {
            return;
        }

        GameObject managerObject = new GameObject("AudioManager");
        managerObject.AddComponent<AudioManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        EnsureSources();
        LoadVolumePreferences();
        WarmCaches();
        RefreshSourceVolumes();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        EventBus.Subscribe<PlayerDeathEvent>(OnPlayerDeath);
        EventBus.Subscribe<RoomVisitedEvent>(OnRoomVisited);
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        EventBus.Unsubscribe<PlayerDeathEvent>(OnPlayerDeath);
        EventBus.Unsubscribe<RoomVisitedEvent>(OnRoomVisited);
    }

    public void PlayMusic(AudioClip clip, float fadeTime = 1f)
    {
        if (clip == null)
        {
            StopMusic(fadeTime);
            return;
        }

        EnsureSources();
        if (musicRoutine != null)
        {
            StopCoroutine(musicRoutine);
        }

        AudioSource activeSource = useSourceA ? musicSourceA : musicSourceB;
        AudioSource inactiveSource = useSourceA ? musicSourceB : musicSourceA;
        useSourceA = !useSourceA;

        inactiveSource.clip = clip;
        inactiveSource.loop = true;
        inactiveSource.volume = 0f;
        inactiveSource.Play();

        musicRoutine = StartCoroutine(CrossfadeRoutine(activeSource, inactiveSource, Mathf.Max(0.01f, fadeTime)));
    }

    public void PlayMusic(MusicTrackId trackId, float fadeTime = 1f)
    {
        if (currentTrack == trackId && trackId != MusicTrackId.None)
        {
            return;
        }

        currentTrack = trackId;
        PlayMusic(GetMusicClip(trackId), fadeTime);
    }

    public void StopMusic(float fadeTime = 0.8f)
    {
        EnsureSources();
        if (musicRoutine != null)
        {
            StopCoroutine(musicRoutine);
        }

        musicRoutine = StartCoroutine(FadeOutMusicRoutine(Mathf.Max(0.01f, fadeTime)));
        currentTrack = MusicTrackId.None;
    }

    public void PlaySFX(AudioClip clip, Vector2 position, float volume = 1f, float pitch = 1f, bool spatial = true, float spatialBlend = 1f)
    {
        if (clip == null)
        {
            return;
        }

        AudioSource source = GetNextSfxSource();
        if (source == null)
        {
            return;
        }

        source.transform.position = position;
        source.clip = clip;
        source.loop = false;
        source.spatialBlend = spatial ? Mathf.Clamp01(spatialBlend) : 0f;
        source.volume = Mathf.Clamp01(volume) * GetEffectiveSfxVolume();
        source.pitch = Mathf.Clamp(pitch + UnityEngine.Random.Range(-0.05f, 0.05f), 0.65f, 1.35f);
        source.Play();
    }

    public void PlayCue(AudioCueId cueId, Vector2 position, float volume = 1f, float pitch = 1f, bool spatial = true, float spatialBlend = 1f)
    {
        PlaySFX(GetCueClip(cueId), position, volume, pitch, spatial, spatialBlend);
    }

    public AudioClip GetCueClip(AudioCueId cueId)
    {
        if (!cueClips.TryGetValue(cueId, out AudioClip clip) || clip == null)
        {
            clip = ProceduralAudioLibrary.CreateCue(cueId);
            cueClips[cueId] = clip;
        }

        return clip;
    }

    public AudioClip GetMusicClip(MusicTrackId trackId)
    {
        if (trackId == MusicTrackId.None)
        {
            return null;
        }

        if (!musicClips.TryGetValue(trackId, out AudioClip clip) || clip == null)
        {
            clip = ProceduralAudioLibrary.CreateMusic(trackId);
            musicClips[trackId] = clip;
        }

        return clip;
    }

    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(MasterVolumeKey, masterVolume);
        ApplyMixerVolumes();
        RefreshSourceVolumes();
    }

    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(MusicVolumeKey, musicVolume);
        ApplyMixerVolumes();
        RefreshSourceVolumes();
    }

    public void SetSfxVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(SfxVolumeKey, sfxVolume);
        ApplyMixerVolumes();
        RefreshSourceVolumes();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "CityArken" || scene.name == "MainMenu")
        {
            PlayMusic(MusicTrackId.Hub, 1f);
        }
        else if (scene.name == "GameplayScene")
        {
            PlayMusic(MusicTrackId.DungeonFloor1, 1f);
        }
    }

    private void OnRoomVisited(RoomVisitedEvent roomVisitedEvent)
    {
        if (roomVisitedEvent == null || roomVisitedEvent.room == null)
        {
            return;
        }

        if (roomVisitedEvent.room.RuntimeRoomType == RoomType.Boss)
        {
            PlayMusic(MusicTrackId.Boss, 1f);
        }
        else if (SceneManager.GetActiveScene().name == "GameplayScene" && currentTrack != MusicTrackId.DungeonFloor1)
        {
            PlayMusic(MusicTrackId.DungeonFloor1, 1f);
        }
    }

    private void OnPlayerDeath(PlayerDeathEvent playerDeathEvent)
    {
        StopMusic(1.1f);
    }

    private void EnsureSources()
    {
        if (musicSourceA == null)
        {
            musicSourceA = CreateChildSource("MusicA", true);
        }

        if (musicSourceB == null)
        {
            musicSourceB = CreateChildSource("MusicB", true);
        }

        while (sfxSources.Count < sfxPoolSize)
        {
            sfxSources.Add(CreateChildSource("SFX_" + sfxSources.Count, false));
        }
    }

    private AudioSource CreateChildSource(string objectName, bool musicSource)
    {
        Transform existing = transform.Find(objectName);
        GameObject sourceObject = existing != null ? existing.gameObject : new GameObject(objectName);
        sourceObject.transform.SetParent(transform, false);

        AudioSource source = sourceObject.GetComponent<AudioSource>();
        if (source == null)
        {
            source = sourceObject.AddComponent<AudioSource>();
        }

        source.playOnAwake = false;
        source.loop = musicSource;
        source.spatialBlend = musicSource ? 0f : 1f;
        source.volume = musicSource ? defaultMusicVolume : defaultSfxVolume;
        source.rolloffMode = AudioRolloffMode.Linear;
        source.minDistance = 0.5f;
        source.maxDistance = 18f;
        source.outputAudioMixerGroup = musicSource ? musicGroup : sfxGroup;
        return source;
    }

    private AudioSource GetNextSfxSource()
    {
        EnsureSources();
        if (sfxSources.Count == 0)
        {
            return null;
        }

        AudioSource source = sfxSources[nextSfxIndex % sfxSources.Count];
        nextSfxIndex = (nextSfxIndex + 1) % sfxSources.Count;
        return source;
    }

    private IEnumerator CrossfadeRoutine(AudioSource fromSource, AudioSource toSource, float duration)
    {
        float elapsed = 0f;
        float fromStartVolume = fromSource != null ? fromSource.volume : 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            if (fromSource != null)
            {
                fromSource.volume = Mathf.Lerp(fromStartVolume, 0f, t);
            }

            if (toSource != null)
            {
                toSource.volume = Mathf.Lerp(0f, GetEffectiveMusicVolume(), t);
            }

            yield return null;
        }

        if (fromSource != null)
        {
            fromSource.Stop();
            fromSource.volume = 0f;
        }

        if (toSource != null)
        {
            toSource.volume = GetEffectiveMusicVolume();
        }

        musicRoutine = null;
    }

    private IEnumerator FadeOutMusicRoutine(float duration)
    {
        float elapsed = 0f;
        float startA = musicSourceA != null ? musicSourceA.volume : 0f;
        float startB = musicSourceB != null ? musicSourceB.volume : 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            if (musicSourceA != null)
            {
                musicSourceA.volume = Mathf.Lerp(startA, 0f, t);
            }

            if (musicSourceB != null)
            {
                musicSourceB.volume = Mathf.Lerp(startB, 0f, t);
            }

            yield return null;
        }

        if (musicSourceA != null)
        {
            musicSourceA.Stop();
            musicSourceA.volume = GetEffectiveMusicVolume();
        }

        if (musicSourceB != null)
        {
            musicSourceB.Stop();
            musicSourceB.volume = GetEffectiveMusicVolume();
        }

        musicRoutine = null;
    }

    private void WarmCaches()
    {
        GetMusicClip(MusicTrackId.Hub);
        GetMusicClip(MusicTrackId.DungeonFloor1);
        GetMusicClip(MusicTrackId.Boss);
        GetCueClip(AudioCueId.FootstepA);
        GetCueClip(AudioCueId.FootstepB);
        GetCueClip(AudioCueId.FootstepC);
        GetCueClip(AudioCueId.FootstepD);
        GetCueClip(AudioCueId.SwordSwing);
        GetCueClip(AudioCueId.PlayerHit);
        GetCueClip(AudioCueId.DodgeRoll);
        GetCueClip(AudioCueId.PlayerDeath);
        GetCueClip(AudioCueId.LevelUp);
        GetCueClip(AudioCueId.ShadowExtract);
        GetCueClip(AudioCueId.AbilityUnlocked);
        GetCueClip(AudioCueId.Penalty);
        GetCueClip(AudioCueId.GoblinAttack);
        GetCueClip(AudioCueId.GoblinDeath);
        GetCueClip(AudioCueId.GiantImpact);
        GetCueClip(AudioCueId.BossRoar);
        GetCueClip(AudioCueId.BossCharge);
        GetCueClip(AudioCueId.CrystalFire);
        GetCueClip(AudioCueId.EnemyProjectile);
        GetCueClip(AudioCueId.WallImpact);
        GetCueClip(AudioCueId.DoorOpen);
        GetCueClip(AudioCueId.RiftHum);
        GetCueClip(AudioCueId.CrystalPickup);
    }

    private void LoadVolumePreferences()
    {
        masterVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(MasterVolumeKey, 1f));
        musicVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(MusicVolumeKey, 1f));
        sfxVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(SfxVolumeKey, 1f));
    }

    private void RefreshSourceVolumes()
    {
        if (musicSourceA != null)
        {
            musicSourceA.volume = GetEffectiveMusicVolume();
        }

        if (musicSourceB != null)
        {
            musicSourceB.volume = GetEffectiveMusicVolume();
        }

        for (int i = 0; i < sfxSources.Count; i++)
        {
            if (sfxSources[i] != null)
            {
                sfxSources[i].volume = GetEffectiveSfxVolume();
            }
        }
    }

    private float GetEffectiveMusicVolume()
    {
        return defaultMusicVolume * masterVolume * musicVolume;
    }

    private float GetEffectiveSfxVolume()
    {
        return defaultSfxVolume * masterVolume * sfxVolume;
    }

    private void ApplyMixerVolumes()
    {
        if (mixer == null)
        {
            PlayerPrefs.Save();
            return;
        }

        mixer.SetFloat("MasterVolume", LinearToDecibels(masterVolume));
        mixer.SetFloat("MusicVolume", LinearToDecibels(musicVolume));
        mixer.SetFloat("SFXVolume", LinearToDecibels(sfxVolume));
        PlayerPrefs.Save();
    }

    private static float LinearToDecibels(float value)
    {
        if (value <= 0.0001f)
        {
            return -80f;
        }

        return Mathf.Log10(value) * 20f;
    }
}

internal static class ProceduralAudioLibrary
{
    private const int SampleRate = 22050;

    public static AudioClip CreateCue(AudioCueId cueId)
    {
        switch (cueId)
        {
            case AudioCueId.FootstepA:
                return CreateNoiseSweepClip("footstep_a", 0.12f, 180f, 110f, 0.18f, 0.65f);
            case AudioCueId.FootstepB:
                return CreateNoiseSweepClip("footstep_b", 0.11f, 200f, 120f, 0.2f, 0.7f);
            case AudioCueId.FootstepC:
                return CreateNoiseSweepClip("footstep_c", 0.1f, 150f, 90f, 0.17f, 0.62f);
            case AudioCueId.FootstepD:
                return CreateNoiseSweepClip("footstep_d", 0.13f, 170f, 95f, 0.19f, 0.68f);
            case AudioCueId.SwordSwing:
                return CreateSweepClip("sword_swing", 0.16f, 950f, 320f, 0.28f, 0.15f, true);
            case AudioCueId.PlayerHit:
                return CreateStackedClip("player_hit", 0.28f, 140f, 75f, 0.3f);
            case AudioCueId.DodgeRoll:
                return CreateNoiseSweepClip("dodge_roll", 0.22f, 600f, 180f, 0.2f, 0.35f);
            case AudioCueId.PlayerDeath:
                return CreateSweepClip("player_death", 0.7f, 120f, 38f, 0.38f, 0.22f, true);
            case AudioCueId.LevelUp:
                return CreateArpeggioClip("level_up", new[] { 392f, 523f, 784f }, 0.65f, 0.18f);
            case AudioCueId.ShadowExtract:
                return CreateShadowExtractClip();
            case AudioCueId.AbilityUnlocked:
                return CreateArpeggioClip("ability_unlock", new[] { 262f, 392f, 523f, 784f }, 0.8f, 0.2f);
            case AudioCueId.Penalty:
                return CreateSweepClip("penalty", 0.9f, 90f, 48f, 0.35f, 0.45f, true);
            case AudioCueId.GoblinAttack:
                return CreateSweepClip("goblin_attack", 0.18f, 680f, 420f, 0.24f, 0.12f, false);
            case AudioCueId.GoblinDeath:
                return CreateStackedClip("goblin_death", 0.3f, 420f, 120f, 0.24f);
            case AudioCueId.GiantImpact:
                return CreateNoiseSweepClip("giant_impact", 0.5f, 110f, 40f, 0.45f, 0.8f);
            case AudioCueId.BossRoar:
                return CreateSweepClip("boss_roar", 1.3f, 88f, 52f, 0.48f, 0.3f, true);
            case AudioCueId.BossCharge:
                return CreateNoiseSweepClip("boss_charge", 0.42f, 520f, 140f, 0.3f, 0.4f);
            case AudioCueId.CrystalFire:
                return CreateSweepClip("crystal_fire", 0.18f, 1100f, 760f, 0.25f, 0.12f, false);
            case AudioCueId.EnemyProjectile:
                return CreateSweepClip("enemy_projectile", 0.18f, 240f, 160f, 0.22f, 0.2f, true);
            case AudioCueId.WallImpact:
                return CreateNoiseSweepClip("wall_impact", 0.22f, 820f, 220f, 0.25f, 0.72f);
            case AudioCueId.DoorOpen:
                return CreateNoiseSweepClip("door_open", 0.55f, 210f, 90f, 0.3f, 0.75f);
            case AudioCueId.RiftHum:
                return CreateAmbientHumClip("rift_hum", 7f, 62f, 0.1f);
            case AudioCueId.CrystalPickup:
                return CreateArpeggioClip("crystal_pickup", new[] { 620f, 930f }, 0.32f, 0.16f);
            default:
                return CreateSweepClip("fallback", 0.2f, 440f, 220f, 0.2f, 0.15f, false);
        }
    }

    public static AudioClip CreateMusic(MusicTrackId trackId)
    {
        switch (trackId)
        {
            case MusicTrackId.Hub:
                return CreateHubMusic();
            case MusicTrackId.DungeonFloor1:
                return CreateDungeonMusic();
            case MusicTrackId.Boss:
                return CreateBossMusic();
            default:
                return null;
        }
    }

    private static AudioClip CreateSweepClip(string name, float duration, float startFrequency, float endFrequency, float amplitude, float noiseAmount, bool triangle)
    {
        int sampleCount = Mathf.CeilToInt(duration * SampleRate);
        float[] data = new float[sampleCount];
        float phase = 0f;
        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)(sampleCount - 1);
            float envelope = Mathf.Sin(t * Mathf.PI);
            float frequency = Mathf.Lerp(startFrequency, endFrequency, t);
            phase += (Mathf.PI * 2f * frequency) / SampleRate;
            float wave = triangle ? Triangle(phase) : Mathf.Sin(phase);
            float noise = (UnityEngine.Random.value * 2f - 1f) * noiseAmount * (1f - t);
            data[i] = Mathf.Clamp((wave * amplitude + noise * 0.2f) * envelope, -1f, 1f);
        }

        return CreateClip(name, data);
    }

    private static AudioClip CreateNoiseSweepClip(string name, float duration, float lowPassStart, float lowPassEnd, float amplitude, float noiseAmount)
    {
        int sampleCount = Mathf.CeilToInt(duration * SampleRate);
        float[] data = new float[sampleCount];
        float state = 0f;
        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)(sampleCount - 1);
            float cutoff = Mathf.Lerp(lowPassStart, lowPassEnd, t);
            float alpha = Mathf.Clamp01((cutoff * 2f * Mathf.PI) / SampleRate);
            float white = (UnityEngine.Random.value * 2f - 1f) * noiseAmount;
            state += alpha * (white - state);
            float envelope = Mathf.Sin(t * Mathf.PI);
            data[i] = Mathf.Clamp(state * amplitude * envelope, -1f, 1f);
        }

        return CreateClip(name, data);
    }

    private static AudioClip CreateStackedClip(string name, float duration, float topFrequency, float lowFrequency, float amplitude)
    {
        int sampleCount = Mathf.CeilToInt(duration * SampleRate);
        float[] data = new float[sampleCount];
        float phaseA = 0f;
        float phaseB = 0f;
        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)(sampleCount - 1);
            float envelope = Mathf.Sin(t * Mathf.PI);
            phaseA += (Mathf.PI * 2f * Mathf.Lerp(topFrequency, lowFrequency, t)) / SampleRate;
            phaseB += (Mathf.PI * 2f * Mathf.Lerp(lowFrequency, lowFrequency * 0.7f, t)) / SampleRate;
            float sample = (Triangle(phaseA) * 0.6f + Mathf.Sin(phaseB) * 0.4f) * amplitude * envelope;
            data[i] = Mathf.Clamp(sample, -1f, 1f);
        }

        return CreateClip(name, data);
    }

    private static AudioClip CreateArpeggioClip(string name, float[] notes, float duration, float amplitude)
    {
        int sampleCount = Mathf.CeilToInt(duration * SampleRate);
        float[] data = new float[sampleCount];
        int noteSamples = Mathf.Max(1, sampleCount / Mathf.Max(1, notes.Length));
        float phase = 0f;
        for (int i = 0; i < sampleCount; i++)
        {
            int noteIndex = Mathf.Clamp(i / noteSamples, 0, notes.Length - 1);
            float frequency = notes[noteIndex];
            phase += (Mathf.PI * 2f * frequency) / SampleRate;
            float localT = (i % noteSamples) / (float)noteSamples;
            float envelope = Mathf.Sin(localT * Mathf.PI);
            data[i] = Mathf.Clamp((Mathf.Sin(phase) + Triangle(phase * 2f) * 0.25f) * amplitude * envelope, -1f, 1f);
        }

        return CreateClip(name, data);
    }

    private static AudioClip CreateShadowExtractClip()
    {
        int sampleCount = Mathf.CeilToInt(1.5f * SampleRate);
        float[] data = new float[sampleCount];
        float phase = 0f;
        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)(sampleCount - 1);
            float frequency = Mathf.Lerp(190f, 60f, t);
            phase += (Mathf.PI * 2f * frequency) / SampleRate;
            float whisper = (UnityEngine.Random.value * 2f - 1f) * 0.12f * (1f - t);
            float sample = (Mathf.Sin(phase) * 0.22f + whisper) * Mathf.Sin(t * Mathf.PI);
            data[i] = Mathf.Clamp(sample, -1f, 1f);
        }

        return CreateClip("shadow_extract", data);
    }

    private static AudioClip CreateAmbientHumClip(string name, float duration, float baseFrequency, float amplitude)
    {
        int sampleCount = Mathf.CeilToInt(duration * SampleRate);
        float[] data = new float[sampleCount];
        float phaseA = 0f;
        float phaseB = 0f;
        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)sampleCount;
            phaseA += (Mathf.PI * 2f * (baseFrequency + Mathf.Sin(t * Mathf.PI * 2f) * 2f)) / SampleRate;
            phaseB += (Mathf.PI * 2f * (baseFrequency * 2.02f)) / SampleRate;
            data[i] = Mathf.Clamp((Mathf.Sin(phaseA) * 0.7f + Mathf.Sin(phaseB) * 0.3f) * amplitude, -1f, 1f);
        }

        return CreateClip(name, data);
    }

    private static AudioClip CreateHubMusic()
    {
        int sampleCount = SampleRate * 10;
        float[] data = new float[sampleCount];
        float phaseBass = 0f;
        float phasePad = 0f;
        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)SampleRate;
            phaseBass += (Mathf.PI * 2f * 55f) / SampleRate;
            phasePad += (Mathf.PI * 2f * 110f) / SampleRate;
            float pulse = Mathf.Sin(t * Mathf.PI * 0.5f) * 0.5f + 0.5f;
            float bell = 0f;
            float measureTime = t % 2.5f;
            if (measureTime < 0.18f)
            {
                bell = Mathf.Sin(measureTime * 2f * Mathf.PI * 620f) * (1f - (measureTime / 0.18f)) * 0.08f;
            }

            data[i] = Mathf.Clamp(Mathf.Sin(phaseBass) * 0.08f + Triangle(phasePad) * 0.03f * pulse + bell, -1f, 1f);
        }

        return CreateClip("bgm_arken", data);
    }

    private static AudioClip CreateDungeonMusic()
    {
        int sampleCount = SampleRate * 10;
        float[] data = new float[sampleCount];
        float phaseBass = 0f;
        float phaseLead = 0f;
        float[] notes = { 110f, 165f, 147f, 220f };
        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)SampleRate;
            phaseBass += (Mathf.PI * 2f * 72f) / SampleRate;
            int noteIndex = Mathf.FloorToInt((t * 2f) % notes.Length);
            phaseLead += (Mathf.PI * 2f * notes[noteIndex]) / SampleRate;
            float gate = ((Mathf.FloorToInt(t * 4f) % 2) == 0) ? 1f : 0.35f;
            data[i] = Mathf.Clamp(Triangle(phaseBass) * 0.1f + Mathf.Sin(phaseLead) * 0.05f * gate, -1f, 1f);
        }

        return CreateClip("bgm_dungeon_1", data);
    }

    private static AudioClip CreateBossMusic()
    {
        int sampleCount = SampleRate * 8;
        float[] data = new float[sampleCount];
        float phaseBass = 0f;
        float phaseDrive = 0f;
        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)SampleRate;
            phaseBass += (Mathf.PI * 2f * 62f) / SampleRate;
            phaseDrive += (Mathf.PI * 2f * 248f) / SampleRate;
            float rhythm = ((Mathf.FloorToInt(t * 3f) % 2) == 0) ? 1f : 0.5f;
            float sample = Triangle(phaseBass) * 0.14f + Mathf.Sign(Mathf.Sin(phaseDrive)) * 0.035f * rhythm;
            data[i] = Mathf.Clamp(sample, -1f, 1f);
        }

        return CreateClip("bgm_boss", data);
    }

    private static AudioClip CreateClip(string name, float[] data)
    {
        AudioClip clip = AudioClip.Create(name, data.Length, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private static float Triangle(float phase)
    {
        return 2f * Mathf.Abs(2f * ((phase / (Mathf.PI * 2f)) - Mathf.Floor((phase / (Mathf.PI * 2f)) + 0.5f))) - 1f;
    }
}
