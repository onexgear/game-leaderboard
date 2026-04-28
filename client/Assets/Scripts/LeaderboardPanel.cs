using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

public class LeaderboardPanel : MonoBehaviour
{
    public TMP_Text[] rankTexts;

    private const string API_URL = "http://localhost:8080/api/v1/leaderboard?limit=5";

    void Start()
    {
        if (rankTexts == null || rankTexts.Length == 0)
        {
            rankTexts = new TMP_Text[5];
            for (int i = 0; i < 5; i++)
                rankTexts[i] = transform.Find($"Rank{i + 1}Text")?.GetComponent<TMP_Text>();
        }

        InvokeRepeating(nameof(FetchLeaderboard), 1f, 5f);
    }

    void FetchLeaderboard()
    {
        StartCoroutine(GetLeaderboard());
    }

    IEnumerator GetLeaderboard()
    {
        UnityWebRequest request = UnityWebRequest.Get(API_URL);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning($"[Leaderboard] Fetch failed: {request.error}");
            yield break;
        }

        LeaderboardResponse response = JsonUtility.FromJson<LeaderboardResponse>(request.downloadHandler.text);
        for (int i = 0; i < rankTexts.Length; i++)
        {
            if (response.leaderboard != null && i < response.leaderboard.Length)
            {
                LeaderboardEntry e = response.leaderboard[i];
                rankTexts[i].text = $"#{e.rank}  {e.player_id}  {e.score}";
            }
            else
            {
                rankTexts[i].text = "-";
            }
        }
    }

    [System.Serializable]
    class LeaderboardEntry
    {
        public int rank;
        public string player_id;
        public float score;
    }

    [System.Serializable]
    class LeaderboardResponse
    {
        public LeaderboardEntry[] leaderboard;
    }
}
