using System;
using System.Collections;
using System.Text;
using System.Xml;
using UnityEngine;
using UnityEngine.Networking;

public class MRServiceWrapper : MonoBehaviour
{
    [SerializeField]
    public string BASE_URL = "http://localhost:8000"; // Replace with your base url

    public void GenerateMeshAndStartPolling(string prompt, float guidanceScale, int pollingRateMilliseconds, Action<string> callback)
    {
        StartCoroutine(GenerateMesh(prompt, guidanceScale, (GenerationTaskResponse task) =>
        {

            StartPollingTaskStatus(task.task_id, task.estimated_duration, pollingRateMilliseconds, callback);
        }));
    }

    public IEnumerator GenerateMesh(string prompt, float guidanceScale, Action<GenerationTaskResponse> callback)
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

            Debug.Log($"HTTP Method: {www.method}");
            Debug.Log($"URL: {www.url}");
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.Log(www.error);
            }
            else
            {
                GenerationTaskResponse response = JsonUtility.FromJson<GenerationTaskResponse>(www.downloadHandler.text);
                callback(response);
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
                yield return GetTaskResult(taskId, callback);
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

    public IEnumerator GetTaskStatus(string taskId, Action<TaskStatus> callback)
    {
        using (UnityWebRequest www = UnityWebRequest.Get(BASE_URL + "/tasks/" + taskId + "/status"))
        {
            Debug.Log($"HTTP Method: {www.method}");
            Debug.Log($"URL: {www.url}");
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.Log(www.error);
            }
            else
            {
                TaskStatus response = JsonUtility.FromJson<TaskStatus>(www.downloadHandler.text);
                callback(response);
            }
        }
    }

    public IEnumerator GetTaskResult(string taskId, Action<string> callback)
    {
        using (UnityWebRequest www = UnityWebRequest.Get(BASE_URL + "/tasks/" + taskId + "/results"))
        {
            Debug.Log($"HTTP Method: {www.method}");
            Debug.Log($"URL: {www.url}");
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.Log(www.error);
            }
            else
            {
                // Assuming the result is a text file. Adapt to your needs.
                callback(www.downloadHandler.text);
            }
        }
    }
    public IEnumerator GetTaskPreview(string taskId, Action<byte[]> callback)
    {
        using (UnityWebRequest www = UnityWebRequest.Get(BASE_URL + "/tasks/" + taskId + "/preview"))
        {
            Debug.Log($"HTTP Method: {www.method}");
            Debug.Log($"URL: {www.url}");
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.Log(www.error);
            }
            else
            {
                // Assuming the result is a gif file. Adapt to your needs.
                callback(www.downloadHandler.data);
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
