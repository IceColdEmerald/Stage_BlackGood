using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public static class HighScoreManager
{
    private const string SaveKey = "Leaderboard";

    public static List<HighScoreEntry> LoadScores()
    {
        if (!PlayerPrefs.HasKey(SaveKey))
            return new List<HighScoreEntry>();

        string json = PlayerPrefs.GetString(SaveKey);

        HighScoreData data = JsonUtility.FromJson<HighScoreData>(json);

        return data.entries;
    }

    public static void SaveScores(List<HighScoreEntry> entries)
    {
        HighScoreData data = new HighScoreData();
        data.entries = entries;

        string json = JsonUtility.ToJson(data);

        PlayerPrefs.SetString(SaveKey, json);
        PlayerPrefs.Save();
    }

    public static bool IsTop10(int score)
    {
        List<HighScoreEntry> entries = LoadScores();

        if (entries.Count < 10)
            return true;

        return score > entries.Last().score;
    }

    public static void AddEntry(HighScoreEntry entry)
    {
        List<HighScoreEntry> entries = LoadScores();

        entries.Add(entry);

        entries = entries
            .OrderByDescending(e => e.score)
            .Take(10)
            .ToList();

        SaveScores(entries);
    }
}