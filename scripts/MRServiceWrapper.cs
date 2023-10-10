using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;
using Object = System.Object;

public sealed class MRServiceWrapper : MonoBehaviour
{
    [SerializeField]
    private string BASE_URL = "https://2778-123-205-105-56.ngrok-free.app"; // Replace with your base url
    private static MRServiceWrapper _instance;
    private static readonly Object _lock = new();
    private MRServiceWrapper() { }
    public static MRServiceWrapper Instance() {
        if (_instance == null) {
            lock (_lock) {
                if (_instance == null) {
                    _instance = new MRServiceWrapper();
                }
            }
            _instance = new MRServiceWrapper();
        }
        return _instance;
    }
    public void GenerateMeshAndStartPolling(string access_token, string prompt, float guidanceScale, int pollingRateMilliseconds, Action<string> callback)
    {
        StartCoroutine(GenerateMesh(access_token, prompt, guidanceScale, (GenerationTaskResponse task) =>
        {

            StartPollingTaskStatus(task.task_id, task.estimated_duration, pollingRateMilliseconds, callback);
        }));
    }

    public IEnumerator GenerateMesh(string access_token, string prompt, float guidanceScale, Action<GenerationTaskResponse> successCallback, Action<string> errorCallback = null)
    {
 
        var requestPayload = new GenerationTaskRequest()
        {
            prompt = prompt,
            guidance_scale = guidanceScale
        };

        var json = JsonUtility.ToJson(requestPayload);
        var url = BASE_URL + "/generate";
        using (var www = UnityWebRequest.Post(url, "POST"))
        using (var uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json)))
        {
            www.uploadHandler.Dispose(); // <- Unity 沒有正確地對原始 uploadHandler 進行垃圾回收。 需要手動處置。
            www.uploadHandler = uploadHandler;
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            // Set the Authorization header
            www.SetRequestHeader("Authorization", "Bearer " + access_token);


            Debug.Log($"HTTP Method: {www.method}");
            Debug.Log($"URL: {www.url}");
            www.SetRequestHeader("ngrok-skip-browser-warning", "any_value_here");
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.Log(www.error);
                errorCallback?.Invoke(www.error);
            }
            else
            {
                GenerationTaskResponse response = JsonUtility.FromJson<GenerationTaskResponse>(www.downloadHandler.text);
                successCallback(response);
            }
        }
    }
    private void StartPollingTaskStatus(string taskId, string estimatedDuration, int pollingRateMilliseconds, Action<string> callback)
    {
        TimeSpan duration = XmlConvert.ToTimeSpan(estimatedDuration);
        Debug.Log($"Backend is generating. Estimated duration: {duration}");
        StartCoroutine(PollTaskStatus(taskId, duration, pollingRateMilliseconds, callback));
    }

    private IEnumerator PollTaskStatus(string taskId, TimeSpan estimatedDuration, int pollingRateMilliseconds, Action<string> callback)
    {
        yield return new WaitForSecondsRealtime((float)estimatedDuration.TotalSeconds);

        while (true)
        {
            TaskStatus taskStatus = null;
            yield return GetTaskStatus(taskId, (status) => taskStatus = status);
            Debug.Log($"Task Status: {taskStatus.status}");
            if (taskStatus.status == TaskStatusEnum.completed.ToString())
            {
                yield return GetFurnitureOBJ(taskId, callback);
                yield break;
            }
            else if (taskStatus.status == TaskStatusEnum.failed.ToString())
            {
                Debug.Log("Task failed");
                yield break;
            }

            yield return new WaitForSecondsRealtime(pollingRateMilliseconds / 1000f); // Convert milliseconds to seconds
        }
    }

    public IEnumerator GetTaskStatus(string taskId, Action<TaskStatus> successCallback, Action<string> errorCallback = null)
    {
        using (UnityWebRequest www = UnityWebRequest.Get(BASE_URL + "/tasks/" + taskId + "/status"))
        {
            Debug.Log($"HTTP Method: {www.method}");
            Debug.Log($"URL: {www.url}");
            www.SetRequestHeader("ngrok-skip-browser-warning", "any_value_here");
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.Log(www.error);
                errorCallback?.Invoke(www.error);
            }
            else
            {
                TaskStatus response = JsonUtility.FromJson<TaskStatus>(www.downloadHandler.text);
                successCallback(response);
            }
        }
    }

    public IEnumerator GetFurnitureOBJ(string uuid, Action<string> successCallback, Action<string> errorCallback = null)
    {
        using (UnityWebRequest www = UnityWebRequest.Get(BASE_URL + "/furnitures/" + uuid))
        {
            Debug.Log($"HTTP Method: {www.method}");
            Debug.Log($"URL: {www.url}");
            www.SetRequestHeader("ngrok-skip-browser-warning", "any_value_here");
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.Log(www.error);
                errorCallback?.Invoke(www.error);
            }
            else
            {
                // Assuming the result is a text file. Adapt to your needs.
                successCallback(www.downloadHandler.text);
            }
        }
    }
    /*
        Unity doesn't support gif out of the box.
    */
    public IEnumerator GetTaskPreview(string taskId, bool return_png, Action<byte[]> successCallback, Action<string> errorCallback = null)
    {
        var return_png_string = return_png.ToString().ToLower();
        using (UnityWebRequest www = UnityWebRequest.Get(BASE_URL + "/tasks/" + taskId + "/preview?return_png=" + return_png_string))
        {
            Debug.Log($"HTTP Method: {www.method}");
            Debug.Log($"URL: {www.url}");
            www.SetRequestHeader("ngrok-skip-browser-warning", "any_value_here");
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.Log(www.error);
                errorCallback?.Invoke(www.error);
            }
            else
            {
                // Assuming the result is a gif file. Adapt to your needs.
                successCallback(www.downloadHandler.data);
            }
        }
    }
    public IEnumerator GetLoginCode(Action<LoginCodeResponse> successCallback, Action<string> errorCallback = null) {
        using (UnityWebRequest www = UnityWebRequest.Get(BASE_URL + "/login_code")) {
            Debug.Log($"HTTP Method: {www.method}");
            Debug.Log($"URL: {www.url}");
            www.SetRequestHeader("ngrok-skip-browser-warning", "any_value_here");
            yield return www.SendWebRequest();
            if (www.result != UnityWebRequest.Result.Success) {
                Debug.Log(www.error);
                errorCallback?.Invoke(www.error);
            } else {
                LoginCodeResponse response = JsonUtility.FromJson<LoginCodeResponse>(www.downloadHandler.text);
                // Assuming the result is a gif file. Adapt to your needs.
                successCallback(response);
            }
        }
    }
    public IEnumerator LoginWithCode(string code, Action<AccessTokenResponse> successCallback, Action<string> errorCallback = null) {
        var loginWithCodeRequest = new LoginWithCodeRequest {
            login_code = code
        };

        var json = JsonUtility.ToJson(loginWithCodeRequest);
        var url = BASE_URL + "/login_code/token";
        using (var www = UnityWebRequest.Post(url, "POST"))
        using (var uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json))) {
            www.uploadHandler.Dispose(); // <- Unity 沒有正確地對原始 uploadHandler 進行垃圾回收。 需要手動處置。
            www.uploadHandler = uploadHandler;
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            Debug.Log($"HTTP Method: {www.method}");
            Debug.Log($"URL: {www.url}");
            www.SetRequestHeader("ngrok-skip-browser-warning", "any_value_here");
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success) {
                Debug.Log(www.error);
                errorCallback?.Invoke(www.error);
            } else {
                AccessTokenResponse response = JsonUtility.FromJson<AccessTokenResponse>(www.downloadHandler.text);
                successCallback(response);
            }
        }
    }
    public IEnumerator GetAllFurnitureInfoByUser(string user_uuid, Action<FurnitureInfoList> successCallback, Action<string> errorCallback = null) {
        using (UnityWebRequest www = UnityWebRequest.Get(BASE_URL + "/users/" + user_uuid + "/model_info")) {
            Debug.Log($"HTTP Method: {www.method}");
            Debug.Log($"URL: {www.url}");
            www.SetRequestHeader("ngrok-skip-browser-warning", "any_value_here");
            yield return www.SendWebRequest();
            if (www.result != UnityWebRequest.Result.Success) {
                Debug.Log(www.error);
                errorCallback?.Invoke(www.error);
            } else {
                FurnitureInfoList response = JsonConvert.DeserializeObject<FurnitureInfoList>(www.downloadHandler.text);
                // Assuming the result is a gif file. Adapt to your needs.
                successCallback(response);
            }
        }
    }
    public IEnumerator GetFurnitureInfo(uint skip, uint limit, Action<FurnitureInfoList> successCallback, Action<string> errorCallback = null) {
        using (UnityWebRequest www = UnityWebRequest.Get(BASE_URL + String.Format("/furnitures/info?skip={0}&limit={1}", skip, limit))) {
            Debug.Log($"HTTP Method: {www.method}");
            Debug.Log($"URL: {www.url}");
            www.SetRequestHeader("ngrok-skip-browser-warning", "any_value_here");
            yield return www.SendWebRequest();
            if (www.result != UnityWebRequest.Result.Success) {
                Debug.Log(www.error);
                errorCallback?.Invoke(www.error);
            } else {
                FurnitureInfoList response = JsonConvert.DeserializeObject<FurnitureInfoList>(www.downloadHandler.text);
                // Assuming the result is a gif file. Adapt to your needs.
                successCallback(response);
            }
        }
    }
    public IEnumerator GetSelfInfo(string access_token, Action<UserInfo> successCallback, Action<string> errorCallback = null) {
        using (UnityWebRequest www = UnityWebRequest.Get(BASE_URL + string.Format("/user-info"))) {
            Debug.Log($"HTTP Method: {www.method}");
            Debug.Log($"URL: {www.url}");
            www.SetRequestHeader("Authorization", "Bearer " + access_token);
            www.SetRequestHeader("ngrok-skip-browser-warning", "any_value_here");
            yield return www.SendWebRequest();
            if (www.result != UnityWebRequest.Result.Success) {
                Debug.Log(www.error);
                errorCallback?.Invoke(www.error);
            } else {
                UserInfo response = JsonUtility.FromJson<UserInfo>(www.downloadHandler.text);
                // Assuming the result is a gif file. Adapt to your needs.
                successCallback(response);
            }
        }
    }
}

[Serializable]
public class GenerationTaskRequest
{
    public string prompt;
    public float guidance_scale;
}
[Serializable]
public class GenerationTaskResponse
{
    public string task_id;
    public string estimated_duration;// Stored as string in ISO8601 format

}
public enum TaskStatusEnum
{
    waiting,
    processing,
    completed,
    failed
}

[Serializable]
public class TaskStatus
{
    public string task_id;
    public string status; // Stored as string
    public string message;
}
[Serializable]
public class LoginCodeResponse {
    public string login_code;
    public string expiration_duration;// Stored as string in ISO8601 format
}
[Serializable]
public class LoginWithCodeRequest { 
    public string login_code;
}
[Serializable]
public class AccessTokenResponse {
    public string access_token;
    public string token_type;
}
public class FurnitureInfo {
    public string uuid { get; set; }
    public string name { get; set; }
    public string user_uuid { get; set; }
    public string username { get; set; }
    public string description { get; set; }
    public int scale_type { get; set; }
    public double scale_x { get; set; }
    public double scale_y { get; set; }
    public double scale_z { get; set; }
    public string source { get; set; }
    public override string ToString() {
        return String.Format("UUID: {0}, Name: {1}, User UUID: {2}, Username: {3}, Description: {4}, Scale Type: {5}, Scale X: {6}, Scale Y: {7}, Scale Z: {8}, Source: {9}", uuid, name, user_uuid, username, description, scale_type, scale_x, scale_y, scale_z, source);
    }
}
public class FurnitureInfoList {
    public List<FurnitureInfo> furniture_infos { get; set; }
    public int total_furniture_count { get; set; }
    public override string ToString() {
        var output = "";
        foreach (var info in furniture_infos) {
            output += info.ToString() + "\n";
        }

        output += "Total Furniture Count: " + total_furniture_count;

        return output;
    }
}
[Serializable]
public class UserInfo {

    public string uuid;
    public string username;
    public string created_at;
    public override string ToString() {
        return String.Format("uuid: {0} username: {1} created_at: {2}", uuid, username, created_at);
    }
}