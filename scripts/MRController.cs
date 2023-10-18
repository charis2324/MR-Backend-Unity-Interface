using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;


public class MRController
{
    private string baseUrl;
    private HttpClient client = new HttpClient();
    private string accessToken;
    public MRController(string baseUrl, string accessToken)
    {
        this.baseUrl = baseUrl;
        this.accessToken = accessToken;
    }
    public async Task Connect(CancellationToken token)
    {
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        client.DefaultRequestHeaders.Add("ngrok-skip-browser-warning", "whatever");
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        try
        {
            Debug.Log("[MRController] Connect: Connecting to backend...");
            var stream = await client.GetStreamAsync(baseUrl + "/api/v1/controller/event");
            Debug.Log("[MRController] Connect: Connected...");
            using (var reader = new StreamReader(stream))
            {
                StringBuilder sb = new StringBuilder();
                while (true)
                {
                    if (token.IsCancellationRequested)
                    {
                        break; // Exit the loop if cancellation is requested.
                    }
                    Debug.Log("[MRController] Connect: Waiting for event...");
                    var line = await reader.ReadLineAsync();
                    if (string.IsNullOrEmpty(line))
                    {
                        var json = sb.ToString();
                        sb.Clear();
                        Debug.Log(json);
                        try
                        {
                            var eventCommand = EventParser.Parse(json);
                            Debug.Log(eventCommand);
                            eventCommand?.Execute();
                        }
                        catch (Exception e)
                        {
                            Debug.Log($"Error executing event command: {e.Message}");
                        }

                    }
                    else
                    {
                        sb.AppendLine(line);
                    }
                }
            }
        }
        catch (TaskCanceledException e)
        {
            Debug.Log("Task was cancelled: " + e.Message);
        }
        catch (Exception e)
        {
            Debug.Log("Error: " + e.Message);
        }
    }
}

public static class EventParser
{
    public static IEventCommand? Parse(string eventString)
    {
        eventString = eventString.TrimEnd();
        var lines = eventString.Split("\n");
        if (lines[0][0] == ':')
        {
            return null;
        }
        if (lines.Length != 2)
        {
            throw new Exception("Event string must have 2 lines");
        }
        if (!lines[0].StartsWith("event: "))
        {
            throw new Exception("Event string must start with 'event: '");
        }
        if (!lines[1].StartsWith("data: "))
        {
            throw new Exception("Data string must start with 'data: '");
        }
        var eventName = lines[0].Substring(7).Trim();
        var eventData = lines[1].Substring(6).Trim();
        return EventCommandFactory.GetEventCommand(eventName, eventData);
    }
}
public interface IEventCommand
{
    public void Execute();
    public void Deserialize(string jsonData);
}
[Serializable]
public class ImportFurnitureEventCommand : IEventCommand
{
    [JsonProperty("furniture_uuid")]
    public string? FurnitureUuid { get; set; }

    public static event Action<string>? OnEvent;
    public void Execute()
    {
        if (FurnitureUuid == null)
        {
            Debug.Log("FurnitureUuid is null");
            return;
        }
        Debug.Log($"[ImportFurnitureEventCommand] Execute: event triggered...");
        OnEvent?.Invoke(FurnitureUuid);
    }

    public void Deserialize(string jsonData)
    {
        var cmd = JsonConvert.DeserializeObject<ImportFurnitureEventCommand>(jsonData);
        if (cmd == null) return;
        FurnitureUuid = cmd.FurnitureUuid;
    }
}

public static class EventCommandFactory
{
    static Dictionary<string, Type> eventCommandTypes = new Dictionary<string, Type>();

    static EventCommandFactory()
    {
        eventCommandTypes.Add("importFurniture", typeof(ImportFurnitureEventCommand));
    }

    public static IEventCommand? GetEventCommand(string eventName, string jsonData)
    {
        var eventCommandType = eventCommandTypes
            .Where(x => x.Key == eventName)
            .Select(x => x.Value).FirstOrDefault();

        if (eventCommandType == null) return null;

        var eventCommandInstance = Activator.CreateInstance(eventCommandType);
        if (eventCommandInstance == null) return null;

        var eventCommand = (IEventCommand)eventCommandInstance;
        eventCommand.Deserialize(jsonData);
        return eventCommand;
    }
}
