using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class PlayerPanel : MonoBehaviour
{
    public TMP_InputField playerNameInput;
    public TMP_InputField scoreInput;
    public Button submitButton;
    public TMP_Text statusText;

    private const string API_URL = "http://localhost:8080/api/v1/scores";

    void Start()
    {
        if (playerNameInput == null)
            playerNameInput = transform.Find("PlayerNameInput")?.GetComponent<TMP_InputField>();
        if (scoreInput == null)
            scoreInput = transform.Find("ScoreInput")?.GetComponent<TMP_InputField>();
        if (submitButton == null)
            submitButton = transform.Find("SubmitButton")?.GetComponent<Button>();
        if (statusText == null)
            statusText = transform.Find("StatusText")?.GetComponent<TMP_Text>();

        submitButton.onClick.AddListener(OnSubmit);
    }

    void OnSubmit()
    {
        string playerName = playerNameInput.text.Trim();
        string scoreStr = scoreInput.text.Trim();

        if (string.IsNullOrEmpty(playerName) || string.IsNullOrEmpty(scoreStr))
        {
            statusText.text = "請填寫名稱和分數";
            return;
        }

        if (!float.TryParse(scoreStr, out float score) || score < 0)
        {
            statusText.text = "分數格式錯誤";
            return;
        }

        StartCoroutine(SubmitScore(playerName, score));
    }

    IEnumerator SubmitScore(string playerID, float score)
    {
        string body = JsonUtility.ToJson(new ScoreRequest { player_id = playerID, score = score });
        UnityWebRequest request = new UnityWebRequest(API_URL, "POST");
        request.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(body));
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        statusText.text = request.result == UnityWebRequest.Result.Success
            ? $"提交成功！{playerID} 分數 {score}"
            : $"錯誤：{request.error}";
    }

    [System.Serializable]
    class ScoreRequest
    {
        public string player_id;
        public float score;
    }
}
