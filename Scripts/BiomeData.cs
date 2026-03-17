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
    public biomeElevations elevation;
    public biomeMoistures moisture;
    public continentaless continentaless;
    public Color color;
    public GameObject tree;
    public Texture2D texture;
}

public enum biomeElevations
{
    NONE, LOW, MID, HIGH
}
public enum biomeMoistures
{
    NONE, LOW, MID, HIGH
}
public enum continentaless
{
    NONE, LOW, MID, HIGH
}
