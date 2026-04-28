using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

public class OpponentPanel : MonoBehaviour
{
    public TMP_Dropdown nameDropdown;

    [Header("對手名稱")]
    public string opponent1 = "DragonSlayer";
    public string opponent2 = "NightHawk";
    public string opponent3 = "IronFist";
    public string opponent4 = "ShadowWolf";
    public string opponent5 = "ThunderBolt";
    public string opponent6 = "StarGazer";
    public string opponent7 = "FrostByte";
    public string opponent8 = "PhoenixRise";
    public string opponent9 = "VoidWalker";
    public string opponent10 = "StormBreaker";

    private string[] opponentNames;
    private string currentOpponent;
    private const string API_URL = "http://localhost:8080/api/v1/scores";

    void Start()
    {
        if (nameDropdown == null)
            nameDropdown = transform.Find("NameDropdown")?.GetComponent<TMP_Dropdown>();

        opponentNames = new string[] {
            opponent1, opponent2, opponent3, opponent4, opponent5,
            opponent6, opponent7, opponent8, opponent9, opponent10
        };

        int randomIndex = Random.Range(0, opponentNames.Length);
        currentOpponent = opponentNames[randomIndex];

        nameDropdown.ClearOptions();
        nameDropdown.AddOptions(new List<string>(opponentNames));
        nameDropdown.value = randomIndex;
        nameDropdown.onValueChanged.AddListener(index => currentOpponent = opponentNames[index]);

        InvokeRepeating(nameof(AutoSubmitScore), 5f, 5f);
    }

    void AutoSubmitScore()
    {
        float score = Mathf.Round(Random.Range(0f, 20000f));
        StartCoroutine(SubmitScore(currentOpponent, score));
    }

    IEnumerator SubmitScore(string playerID, float score)
    {
        string body = JsonUtility.ToJson(new ScoreRequest { player_id = playerID, score = score });
        UnityWebRequest request = new UnityWebRequest(API_URL, "POST");
        request.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(body));
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
            Debug.Log($"[Opponent] {playerID} submitted {score}");
        else
            Debug.LogWarning($"[Opponent] Submit failed: {request.error}");
    }

    [System.Serializable]
    class ScoreRequest
    {
        public string player_id;
        public float score;
    }
}
