using System.IO;
using UnityEngine;

public class PlayerSaveSystem
{

    private static SaveData _saveData = new SaveData();



    [System.Serializable]
    public struct SaveData
    {
        public PlayerSaveData PlayerData;
    }

    public static string SaveFileName()
    {
        string saveFile = Application.persistentDataPath + "/playerData.save";
        return saveFile;
    }

    public static void Save()
    {
        HandelSavaData();

        File.WriteAllText(SaveFileName(), JsonUtility.ToJson(_saveData, true));
    }

    private static void HandelSavaData()
    {
        Movment.instance.Save(ref _saveData.PlayerData);
    }

    public static void Load()
    {
        if (File.Exists(SaveFileName()))
        {
            _saveData = JsonUtility.FromJson<SaveData>(File.ReadAllText(SaveFileName()));
            HandleLoadData();
        }
    }

    private static void HandleLoadData()
    {
        Movment.instance.Load(ref _saveData.PlayerData);
    }
}