using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

public class WeatherManager : MonoBehaviour
{
    public class WeatherState
    {
        public float InitialIntensity;
        public float CurrentIntensity;
        public float TargetIntensity;

        bool TransitionInProgress = false;
        float TransitionProgress;
        float TransitionTime;
        WeatherElementConfig Config;

        public void SwitchToNewPreset(WeatherElementConfig config, float transitionTime)
        {
            // transfer the previous intensity as our starting point
            InitialIntensity = CurrentIntensity;

            // pick a new intensity
            TargetIntensity = config.GetRandomIntensity();

            // store the config
            Config = config;

            // setup the transition
            TransitionProgress = transitionTime > 0f ? 0f : 1f;
            TransitionTime = transitionTime;
            TransitionInProgress = true;
        }

        public float Tick()
        {
            // if there's no transition then do nothing
            if (!TransitionInProgress)
                return CurrentIntensity;

            // need to update progress?
            if (TransitionProgress < 1f)
                TransitionProgress += Time.deltaTime / TransitionTime;

            // update intensity?
            CurrentIntensity = Mathf.Lerp(InitialIntensity, TargetIntensity, TransitionProgress);

            // transition finished?
            if (TransitionProgress >= 1f)
            {
                InitialIntensity = CurrentIntensity;
                TargetIntensity = Config.GetRandomIntensity();
                TransitionProgress = 0f;
                TransitionTime = Config.GetFluctuationTime();

                TransitionInProgress = TransitionTime > 0f;
            }

            return CurrentIntensity;
        }
    }

    [SerializeField, Range(0f, 1f)] float RainIntensity;
    [SerializeField, Range(0f, 1f)] float SnowIntensity;
    [SerializeField, Range(0f, 1f)] float HailIntensity;
    [SerializeField, Range(0f, 1f)] float FogIntensity;

    [SerializeField] float MinFogAttenuationDistance = 10f;
    [SerializeField] float MaxFogAttenuationDistance = 50f;

    [SerializeField] VisualEffect RainVFX;
    [SerializeField] VisualEffect SnowVFX;
    [SerializeField] VisualEffect HailVFX;
    [SerializeField] Volume FogVolume;

    [Header("Default Preset")]
    [SerializeField] WeatherPreset DefaultWeather;

    [Header("Transition Debug")]
    [SerializeField] bool DEBUG_PerformTransition;
    [SerializeField] WeatherPreset DEBUG_TargetPreset;
    [SerializeField] float DEBUG_TransitionTime;

    float PreviousRainIntensity;
    float PreviousHailIntensity;
    float PreviousSnowIntensity;
    float PreviousFogIntensity;
    Fog CachedFogComponent;

    WeatherPreset CurrentWeather;

    WeatherState State_Rain = new WeatherState();
    WeatherState State_Hail = new WeatherState();
    WeatherState State_Snow = new WeatherState();
    WeatherState State_Fog = new WeatherState();

    float InitialFluctuation = 0f;
    public float CurrentFluctuation = 0f;
    float TargetFluctuation = 0f;
    float FluctuationTime = 0f;
    float FluctuationProgress = 0f;
    bool FluctuationInProgress = false;

    // Start is called before the first frame update
    void Start()
    {
        RainVFX.SetFloat("Intensity", RainIntensity);
        HailVFX.SetFloat("Intensity", HailIntensity);
        SnowVFX.SetFloat("Intensity", SnowIntensity);
        FogVolume.weight = FogIntensity;

        FogVolume.profile.TryGet<Fog>(out CachedFogComponent);

        if (CachedFogComponent != null)
        {
            CachedFogComponent.meanFreePath.Override(Mathf.Lerp(MaxFogAttenuationDistance,
                                                               MinFogAttenuationDistance,
                                                               FogIntensity));
        }

        ChangeWeather(DefaultWeather, 0f);
    }

    // Update is called once per frame
    void Update()
    {
        if (DEBUG_PerformTransition)
        {
            DEBUG_PerformTransition = false;
            ChangeWeather(DEBUG_TargetPreset, DEBUG_TransitionTime);
        }

        Update_WeatherTransition();

        if (RainIntensity != PreviousRainIntensity)
        {
            PreviousRainIntensity = RainIntensity;
            RainVFX.SetFloat("Intensity", RainIntensity);
        }
        if (HailIntensity != PreviousHailIntensity)
        {
            PreviousHailIntensity = HailIntensity;
            HailVFX.SetFloat("Intensity", HailIntensity);
        }
        if (SnowIntensity != PreviousSnowIntensity)
        {
            PreviousSnowIntensity = SnowIntensity;
            SnowVFX.SetFloat("Intensity", SnowIntensity);
        }
        if (FogIntensity != PreviousFogIntensity)
        {
            PreviousFogIntensity = FogIntensity;
            FogVolume.weight = FogIntensity;

            if (CachedFogComponent != null)
                CachedFogComponent.meanFreePath.value = Mathf.Lerp(MaxFogAttenuationDistance,
                                                                   MinFogAttenuationDistance,
                                                                   FogIntensity);
        }
    }

    void Update_WeatherTransition()
    {
        // is there a fluctuation?
        if (FluctuationInProgress)
        {
            FluctuationProgress += Time.deltaTime / FluctuationTime;

            CurrentFluctuation = Mathf.Lerp(InitialFluctuation, TargetFluctuation, FluctuationProgress);

            // fluctuation finished? start a new one
            if (FluctuationProgress >= 1f)
            {
                InitialFluctuation = CurrentFluctuation;
                TargetFluctuation = CurrentWeather.GetRandomFluctuation();
                FluctuationTime = CurrentWeather.GetFluctuationTime();
                FluctuationProgress = 0f;
            }
        }
        
        RainIntensity = Mathf.Clamp01(CurrentFluctuation + State_Rain.Tick());
        HailIntensity = Mathf.Clamp01(CurrentFluctuation + State_Hail.Tick());
        SnowIntensity = Mathf.Clamp01(CurrentFluctuation + State_Snow.Tick());
        FogIntensity  = Mathf.Clamp01(CurrentFluctuation + State_Fog.Tick());
    }

    public void ChangeWeather(WeatherPreset newWeather, float transitionTime)
    {
        CurrentWeather = newWeather;

        State_Rain.SwitchToNewPreset(newWeather.Rain, transitionTime);
        State_Hail.SwitchToNewPreset(newWeather.Hail, transitionTime);
        State_Snow.SwitchToNewPreset(newWeather.Snow, transitionTime);
        State_Fog.SwitchToNewPreset(newWeather.Fog, transitionTime);

        // setup for the fluctuation
        InitialFluctuation = CurrentFluctuation;
        TargetFluctuation = CurrentWeather.GetRandomFluctuation();
        FluctuationTime = CurrentWeather.GetFluctuationTime();
        FluctuationProgress = 0f;
        FluctuationInProgress = FluctuationTime > 0f;

        // no fluctuation happening - reset the modifiers
        if (!FluctuationInProgress)
            CurrentFluctuation = InitialFluctuation = TargetFluctuation = 0f;
    }
}
