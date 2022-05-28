using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

[System.Serializable]
public class WeatherElementConfig
{
    [Range(0f, 1f)] public float Intensity = 0f;

    [Range(0f, 1f)] public float FluctuationAmount = 0f;
    public float MinFluctuationInterval = 0f;
    public float MaxFluctuationInterval = 0f;

    public float GetRandomIntensity()
    {
        if (Intensity <= 0f)
            return 0f;

        return Mathf.Clamp01(Intensity + Random.Range(-FluctuationAmount, FluctuationAmount));
    }

    public float GetFluctuationTime()
    {
        return Random.Range(MinFluctuationInterval, MaxFluctuationInterval);
    }
}

[CreateAssetMenu(menuName = "Weather/Preset", fileName = "WeatherPreset")]
public class WeatherPreset : ScriptableObject
{
    [Header("Individual Effects")]
    public WeatherElementConfig Rain;
    public WeatherElementConfig Hail;
    public WeatherElementConfig Snow;
    public WeatherElementConfig Fog;

    [Header("Cloud Configuration")]
    public VolumetricClouds.CloudPresets CloudPreset = VolumetricClouds.CloudPresets.Sparse;
    [Range(0f, 1f)] public float SunLightDimmer = 1f;
    [Range(0f, 1f)] public float AmbientLightDimmer = 1f;

    [Header("Overall")]
    [Range(0f, 1f)] public float FluctuationAmount = 0f;
    public float MinFluctuationInterval = 0f;
    public float MaxFluctuationInterval = 0f;

    public float GetFluctuationTime()
    {
        return Random.Range(MinFluctuationInterval, MaxFluctuationInterval);
    }
    public float GetRandomFluctuation()
    {
        if (FluctuationAmount <= 0f)
            return 0f;

        return Random.Range(-FluctuationAmount, FluctuationAmount);
    }
}
