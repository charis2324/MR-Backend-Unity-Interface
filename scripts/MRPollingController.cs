using MRBackend;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MRPollingController : MonoBehaviour
{
    [SerializeField]
    private string _accessToken;
    [SerializeField]
    private MRServiceWrapper _mrServiceWrapper;
    private bool _isActive;
    [SerializeField]
    private float _pollingIntervalSerialized = 1f;

    public float PollingInterval
    {
        get { return _pollingIntervalSerialized; }
        set
        {
            if (value > 0)
                _pollingIntervalSerialized = value;
            else
                Debug.LogError("Polling interval must be greater than 0");
        }
    }

    private void Awake()
    {
        _isActive = false;
    }

    public void CreatePollingEventControllerSession(Action onSuccess, Action onFailure)
    {
        if (_mrServiceWrapper == null)
        {
            Debug.LogError("MRServiceWrapper is not set.");
            return;
        }

        StartCoroutine(_mrServiceWrapper.CreatePollingControllerSession(_accessToken, (success_response) =>
        {
            Debug.Log("Session created successfully: " + success_response);
            _isActive = true;
            onSuccess?.Invoke();
        }, (fail_response) =>
        {
            Debug.LogError("Failed to create session: " + fail_response);
            _isActive = false;
            onFailure?.Invoke();
        }));
    }

    public IEnumerator EventControllerPolling()
    {
        while (_isActive)
        {
            yield return new WaitForSeconds(_pollingIntervalSerialized);
            StartCoroutine(_mrServiceWrapper.PollEventController(_accessToken, (success_response) =>
            {
                var pollingEvents = PollingEventParser.ParseJObject(success_response);
                foreach (var pollingEvent in pollingEvents)
                {
                    pollingEvent.Trigger();
                }
            }));
        }
    }

    public void StartSessionAndPoll()
    {
        CreatePollingEventControllerSession(
            onSuccess: () => StartCoroutine(EventControllerPolling()),
            onFailure: () => Debug.Log("Failed to create a session."));
    }

    public void StopPoll()
    {
        _isActive = false;
    }
}
public class PollingEventParser
{
    public static List<IEvent> ParseJObject(JObject json)
    {
        if (json == null)
        {
            Debug.LogError("No JSON data provided.");
            return new List<IEvent>();
        }
        if (!json.ContainsKey("events"))
        {
            Debug.LogError("No 'events' key in JSON data.");
            return new List<IEvent>();
        }
        var eventsArray = (JArray)json["events"];
        if (eventsArray.Count < 1)
        {
            return new List<IEvent>();
        }
        List<IEvent> events = new List<IEvent>();
        foreach (var evt in eventsArray)
        {
            string eventName = (string)evt["event_name"];
            IEvent @event = null;
            try
            {
                if (eventName == "importFurniture")
                {
                    @event = evt.ToObject<ImportFurnitureEvent>();
                }
                if (@event != null)
                {
                    events.Add(@event);
                }
            }
            catch (Exception ex)
            {
                Debug.Log(ex);
            }
        }
        return events;
    }
}

public interface IEvent
{

    string EventName { get; }
    void Trigger();
}

[Serializable]
public class ImportFurnitureEvent : IEvent
{
    [JsonProperty("furniture_uuid")]
    public string FurnitureUuid { get; set; }
    string IEvent.EventName => "importFurniture";
    public static event Action<ImportFurnitureEvent> OnEvent;
    public ImportFurnitureEvent(string furnitureUuid)
    {
        FurnitureUuid = furnitureUuid;
    }
    public void Trigger()
    {
        OnEvent?.Invoke(this);
    }
}