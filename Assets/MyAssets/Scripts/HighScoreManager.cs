using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class HighScoreManager : MonoBehaviour
{
    public static HighScoreManager Instance { get; private set; }

    private const string SaveKey = "Leaderboard";

    private List<HighScoreEntry> cachedEntries = new List<HighScoreEntry>();

    private void Awake()
    {
        // Singleton protection
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadScores(); // preload data once
    }

    // ---------------------------
    // LOAD
    // ---------------------------
    public List<HighScoreEntry> LoadScores()
    {
        if (!PlayerPrefs.HasKey(SaveKey))
        {
            cachedEntries = new List<HighScoreEntry>();
            return cachedEntries;
        }

        string json = PlayerPrefs.GetString(SaveKey);

        HighScoreData data = JsonUtility.FromJson<HighScoreData>(json);

        cachedEntries = data?.entries ?? new List<HighScoreEntry>();

        return cachedEntries;
    }

    // ---------------------------
    // SAVE
    // ---------------------------
    public void SaveScores()
    {
        HighScoreData data = new HighScoreData
        {
            entries = cachedEntries
        };

        string json = JsonUtility.ToJson(data);

        PlayerPrefs.SetString(SaveKey, json);
        PlayerPrefs.Save();
    }

    // ---------------------------
    // CHECK TOP 10
    // ---------------------------
    public bool IsTop10(int score)
    {
        if (cachedEntries.Count < 10)
            return true;

        return score > cachedEntries.Last().score;
    }

    // ---------------------------
    // ADD ENTRY (string version)
    // ---------------------------
    public void AddPoints(string playerName, int score, float survivalTime = 0f, int maxLanes = 0)
    {
        HighScoreEntry entry = new HighScoreEntry
        {
            playerName = playerName,
            score = score,
            survivalTime = survivalTime,
            maxLanes = maxLanes
        };

        AddEntry(entry);
    }

    // ---------------------------
    // ADD ENTRY (core)
    // ---------------------------
    public void AddEntry(HighScoreEntry entry)
    {
        cachedEntries.Add(entry);

        cachedEntries = cachedEntries
            .OrderByDescending(e => e.score)
            .Take(10)
            .ToList();

        SaveScores();
    }

    // ---------------------------
    // GET
    // ---------------------------
    public List<HighScoreEntry> GetScores()
    {
        return cachedEntries;
    }
}