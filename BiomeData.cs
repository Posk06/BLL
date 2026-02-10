using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BiomeData", menuName = "Procedural/Biome Data")]
public class BiomeData : ScriptableObject
{
    public List<Biome> biomes = new List<Biome>();
}

[System.Serializable]
public class Biome
{
    public string name;
    public int id;
    public biomeTemps temperature;
    public biomeMoistures moisture;
    public float frequency;
    public float amplitude;
    public float lacunarity;
    public float gain;
    public int octaves;
    public Texture2D groundTexture;
    //
}

public enum biomeTemps
{
    LOW, MID, HIGH
}
public enum biomeMoistures
{
    LOW, MID, HIGH
}
