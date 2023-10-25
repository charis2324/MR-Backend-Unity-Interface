using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;
using Object = System.Object;

namespace MRBackend
{
    public sealed class MRServiceWrapper : MonoBehaviour
    {
        [SerializeField]
        private string BASE_URL = "http://127.0.0.1:8000"; // Replace with your base url
        private static MRServiceWrapper _instance;
        private static readonly Object _lock = new();
        private MRServiceWrapper() { }
        public static MRServiceWrapper Instance()
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = new MRServiceWrapper();
                    }
                }
                _instance = new MRServiceWrapper();
            }
            return _instance;
        }
        public IEnumerator CreatePollingControllerSession(string accessToken, Action<CreatePollingControllerSessionResponse> successCallback, Action<string> errorCallback = null)
        {
            yield return new CreatePollingControllerSessionCommand(accessToken, BASE_URL).Execute(successCallback, errorCallback);
        }
        public IEnumerator PollEventController(string accessToken, Action<JObject> successCallback, Action<string> errorCallback = null)
        {
            yield return new PollEventControllerCommand(accessToken, BASE_URL).Execute(successCallback, errorCallback);
        }
        public IEnumerator GetFurnitureOBJ(string uuid, Action<string> successCallback, Action<string> errorCallback = null)
        {
            yield return new GetFurnitureOBJCommand(uuid, BASE_URL).Execute(successCallback, errorCallback);
        }
        public IEnumerator GetFurniturePreview(string uuid, bool return_png, Action<byte[]> successCallback, Action<string> errorCallback = null)
        {
            yield return new GetFurniturePreviewCommand(uuid, return_png, BASE_URL).Execute(successCallback, errorCallback);
        }
        public IEnumerator GetLoginCode(Action<LoginCodeResponse> successCallback, Action<string> errorCallback = null)
        {
            Debug.Log(string.Format("[MRServiceWrapper] GetLoginCode -> BASE_URL: {0}", BASE_URL));
            yield return new GetLoginCodeCommand(BASE_URL).Execute(successCallback, errorCallback);
        }

        public IEnumerator LoginWithCode(string code, Action<AccessTokenResponse> successCallback, Action<string> errorCallback = null)
        {
            yield return new LoginWithCodeCommand(code, BASE_URL).Execute(successCallback, errorCallback);
        }
        public IEnumerator GetAllFurnitureInfoByUser(string user_uuid, Action<FurnitureInfoList> successCallback, Action<string> errorCallback = null)
        {
            yield return new GetAllFurnitureInfoByUserCommand(user_uuid, BASE_URL).Execute(successCallback, errorCallback);
        }
        public IEnumerator GetFurnitureInfoById(string uuid, Action<FurnitureInfo> successCallback, Action<string> errorCallback = null)
        {
            yield return new GetFurnitureInfoByIdCommand(uuid, BASE_URL).Execute(successCallback, errorCallback);
        }
        public IEnumerator GetFurnitureInfo(uint skip, uint limit, Action<FurnitureInfoList> successCallback, Action<string> errorCallback = null)
        {
            yield return new GetFurnitureInfoCommand(skip, limit, BASE_URL).Execute(successCallback, errorCallback);
        }
        public IEnumerator GetSelfInfo(string access_token, Action<UserInfo> successCallback, Action<string> errorCallback = null)
        {
            yield return new GetSelfInfoCommand(access_token, BASE_URL).Execute(successCallback, errorCallback);
        }
    }
    public class HttpCallBuilder
    {
        private string baseUrl;
        private string url;
        private Dictionary<string, string> headers = new Dictionary<string, string>();
        private string method = "GET";
        private string body;
        public HttpCallBuilder WithBaseUrl(string baseUrl)
        {

            this.baseUrl = baseUrl;
            Debug.Log(string.Format("[HttpCallBuilder] WithBaseUrl -> this.baseUrl: {0}", this.baseUrl));
            return this;
        }
        public HttpCallBuilder WithUrl(string url)
        {

            this.url = url;
            return this;
        }

        public HttpCallBuilder WithHeader(string headerName, string headerValue)
        {
            headers.Add(headerName, headerValue);
            return this;
        }
        //WithHeader("Content-Type", "application/json")
        public HttpCallBuilder WithJsonHeader()
        {
            headers.Add("Content-Type", "application/json");
            return this;
        }
        public HttpCallBuilder WithNgrokHeader()
        {
            headers.Add("ngrok-skip-browser-warning", "whatever");
            return this;
        }
        public HttpCallBuilder WithAccessTokenHeader(string accessToken)
        {
            headers.Add("Authorization", "Bearer " + accessToken);
            return this;
        }
        public HttpCallBuilder WithMethod(string method)
        {
            this.method = method;
            return this;
        }

        public HttpCallBuilder WithBody(string body)
        {
            this.body = body;
            return this;
        }

        public UnityWebRequest Build()
        {
            UnityWebRequest www;
            Debug.Log(string.Format("[HttpCallBuilder] Build -> baseUrl + url: {0}", baseUrl + url));
            if (method == "POST")
            {
                www = UnityWebRequest.Post(baseUrl + url, "POST");
                byte[] bodyRaw = Encoding.UTF8.GetBytes(body);
                www.uploadHandler?.Dispose();
                www.uploadHandler = new UploadHandlerRaw(bodyRaw);
                www.downloadHandler = new DownloadHandlerBuffer();
            }
            else
            {
                www = UnityWebRequest.Get(baseUrl + url);
            }

            foreach (var header in headers)
            {
                www.SetRequestHeader(header.Key, header.Value);
            }

            return www;
        }
    }
    public interface IHttpCommand<T>
    {
        IEnumerator Execute(Action<T> onSuccess, Action<string> onError);
    }
    public abstract class CommandBase
    {
        protected string baseUrl;
        public CommandBase(string baseUrl)
        {
            this.baseUrl = baseUrl;
        }
        protected IEnumerator ProcessRequestInternal(HttpCallBuilder builder, Action<string> errorCallback, Func<UnityWebRequest, bool> processData)
        {
            using (UnityWebRequest www = builder.Build())
            {
                Debug.Log($"HTTP Method: {www.method}");
                Debug.Log($"URL: {www.url}");
                yield return www.SendWebRequest();

                if (www.result != UnityWebRequest.Result.Success)
                {
                    Debug.Log(www.error);
                    errorCallback?.Invoke(www.error);
                }
                else
                {
                    processData(www);
                }
            }
        }

        protected IEnumerator ProcessRequest<T>(HttpCallBuilder builder, Action<T> successCallback, Action<string> errorCallback = null) where T : class
        {
            return ProcessRequestInternal(builder, errorCallback, www =>
            {
                T response = JsonConvert.DeserializeObject<T>(www.downloadHandler.text);
                successCallback(response);
                return true;
            });
        }
        protected IEnumerator ProcessRequestForJObject(HttpCallBuilder builder, Action<JObject> successCallback, Action<string> errorCallback = null)
        {
            return ProcessRequestInternal(builder, errorCallback, www =>
            {
                JObject json = JObject.Parse(www.downloadHandler.text);
                successCallback(json);
                return true;
            });
        }
        protected IEnumerator ProcessRequestForBytes(HttpCallBuilder builder, Action<byte[]> successCallback, Action<string> errorCallback = null)
        {
            return ProcessRequestInternal(builder, errorCallback, www =>
            {
                successCallback(www.downloadHandler.data);
                return true;
            });
        }
        protected IEnumerator ProcessRequestForText(HttpCallBuilder builder, Action<string> successCallback, Action<string> errorCallback = null)
        {
            return ProcessRequestInternal(builder, errorCallback, www =>
            {
                successCallback(www.downloadHandler.text);
                return true;
            });
        }
    }
    public class CreatePollingControllerSessionCommand : CommandBase, IHttpCommand<CreatePollingControllerSessionResponse>
    {
        private string accessToken;
        public CreatePollingControllerSessionCommand(string accessToken, string baseUrl) : base(baseUrl)
        {
            Debug.Log(string.Format("[HttpCommand] CreatePollingControllerSessionCommand -> baseUrl: {0}", baseUrl));
            this.accessToken = accessToken;
        }

        public IEnumerator Execute(Action<CreatePollingControllerSessionResponse> onSuccess, Action<string> onError)
        {
            HttpCallBuilder builder = new HttpCallBuilder()
            .WithBaseUrl(baseUrl)
            .WithUrl("/api/v1/controller/event/connect")
            .WithAccessTokenHeader(accessToken)
            .WithNgrokHeader();
            yield return ProcessRequest(builder, onSuccess, onError);
        }
    }

    public class PollEventControllerCommand : CommandBase, IHttpCommand<JObject>
    {
        private string accessToken;
        public PollEventControllerCommand(string accessToken, string baseUrl) : base(baseUrl)
        {
            Debug.Log(string.Format("[HttpCommand] PollEventControllerCommand -> baseUrl: {0}", baseUrl));
            this.accessToken = accessToken;
        }
        public IEnumerator Execute(Action<JObject> onSuccess, Action<string> onError)
        {
            HttpCallBuilder builder = new HttpCallBuilder()
                .WithBaseUrl(baseUrl)
                .WithUrl("/api/v1/controller/event/poll")
                .WithAccessTokenHeader(accessToken)
                .WithNgrokHeader();
            yield return ProcessRequestForJObject(builder, onSuccess, onError);
        }
    }


    public class GetLoginCodeCommand : CommandBase, IHttpCommand<LoginCodeResponse>
    {



        public GetLoginCodeCommand(string baseUrl) : base(baseUrl)
        {
            Debug.Log(string.Format("[HttpCommand] GetLoginCodeCommand -> baseUrl: {0}", baseUrl));
        }
        public IEnumerator Execute(Action<LoginCodeResponse> onSuccess, Action<string> onError)
        {
            HttpCallBuilder builder = new HttpCallBuilder()
            .WithBaseUrl(baseUrl)
            .WithUrl("/api/v1/auth/login-code")
            .WithNgrokHeader();
            yield return ProcessRequest(builder, onSuccess, onError);
        }
    }
    public class LoginWithCodeCommand : CommandBase, IHttpCommand<AccessTokenResponse>
    {
        private string code;

        public LoginWithCodeCommand(string code, string baseUrl) : base(baseUrl)
        {
            this.code = code;
        }

        public IEnumerator Execute(Action<AccessTokenResponse> onSuccess, Action<string> onError)
        {
            var loginWithCodeRequest = new LoginWithCodeRequest
            {
                LoginCode = code
            };

            var json = JsonConvert.SerializeObject(loginWithCodeRequest);

            HttpCallBuilder builder = new HttpCallBuilder()
                .WithBaseUrl(baseUrl)
                .WithUrl("/api/v1/auth/token-with-login-code")
                .WithMethod("POST")
                .WithBody(json)
                .WithJsonHeader()
                .WithNgrokHeader();

            yield return ProcessRequest(builder, onSuccess, onError);
        }
    }
    public class GetSelfInfoCommand : CommandBase, IHttpCommand<UserInfo>
    {
        private string accessToken;
        public GetSelfInfoCommand(string accessToken, string baseUrl) : base(baseUrl)
        {
            this.accessToken = accessToken;
        }

        public IEnumerator Execute(Action<UserInfo> onSuccess, Action<string> onError)
        {
            HttpCallBuilder builder = new HttpCallBuilder()
                .WithBaseUrl(baseUrl)
                .WithUrl("/api/v1/me")
                .WithAccessTokenHeader(accessToken)
                .WithNgrokHeader();
            yield return ProcessRequest(builder, onSuccess, onError);
        }
    }
    public class GetAllFurnitureInfoByUserCommand : CommandBase, IHttpCommand<FurnitureInfoList>
    {
        private string userId;
        public GetAllFurnitureInfoByUserCommand(string userId, string baseUrl) : base(baseUrl)
        {
            this.userId = userId;
        }
        public IEnumerator Execute(Action<FurnitureInfoList> onSuccess, Action<string> onError)
        {
            HttpCallBuilder builder = new HttpCallBuilder()
                .WithBaseUrl(baseUrl)
                .WithUrl(string.Format("/api/v1/users/{0}/furnitures/info", userId))
                .WithNgrokHeader();
            yield return ProcessRequest(builder, onSuccess, onError);
        }
    }
    public class GetFurnitureInfoByIdCommand : CommandBase, IHttpCommand<FurnitureInfo>
    {
        private string uuid;
        public GetFurnitureInfoByIdCommand(string uuid, string baseUrl) : base(baseUrl)
        {
            this.uuid = uuid;
        }
        public IEnumerator Execute(Action<FurnitureInfo> onSuccess, Action<string> onError)
        {
            HttpCallBuilder builder = new HttpCallBuilder()
                .WithBaseUrl(baseUrl)
                .WithUrl(string.Format("/api/v1/furnitures/{0}/info", uuid))
                .WithNgrokHeader();
            yield return ProcessRequest(builder, onSuccess, onError);
        }
    }
    public class GetFurnitureInfoCommand : CommandBase, IHttpCommand<FurnitureInfoList>
    {
        private uint skip;
        private uint limit;
        public GetFurnitureInfoCommand(uint skip, uint limit, string baseUrl) : base(baseUrl)
        {
            this.skip = skip;
            this.limit = limit;
        }
        public IEnumerator Execute(Action<FurnitureInfoList> onSuccess, Action<string> onError)
        {
            HttpCallBuilder builder = new HttpCallBuilder()
                .WithBaseUrl(baseUrl)
                .WithUrl(string.Format("/api/v1/furnitures/info?skip={0}&limit={1}", skip, limit))
                .WithNgrokHeader();
            yield return ProcessRequest(builder, onSuccess, onError);
        }
    }
    public class GetFurnitureOBJCommand : CommandBase, IHttpCommand<string>
    {
        private string uuid;
        public GetFurnitureOBJCommand(string uuid, string baseUrl) : base(baseUrl)
        {
            this.uuid = uuid;
        }
        public IEnumerator Execute(Action<string> onSuccess, Action<string> onError)
        {
            HttpCallBuilder builder = new HttpCallBuilder()
                .WithBaseUrl(baseUrl)
                .WithUrl(string.Format("/api/v1/furnitures/{0}", uuid))
                .WithNgrokHeader();
            yield return ProcessRequestForText(builder, onSuccess, onError);
        }
    }
    public class GetFurniturePreviewCommand : CommandBase, IHttpCommand<byte[]>
    {
        private string uuid;
        private bool returnPng;
        public GetFurniturePreviewCommand(string uuid, bool returnPng, string baseUrl) : base(baseUrl)
        {
            this.uuid = uuid;
            this.returnPng = returnPng;
        }
        public IEnumerator Execute(Action<byte[]> onSuccess, Action<string> onError)
        {
            HttpCallBuilder builder = new HttpCallBuilder()
                .WithBaseUrl(baseUrl)
                .WithUrl(string.Format("/api/v1/furnitures/{0}/preview?return_png={1}", uuid, returnPng.ToString().ToLower()))
                .WithNgrokHeader();
            yield return ProcessRequestForBytes(builder, onSuccess, onError);
        }
    }
    [Serializable]
    public class TaskStatus
    {
        [JsonProperty("task_id")]
        public string TaskId { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; } // Stored as string

        [JsonProperty("message")]
        public string Message { get; set; }
    }

    [Serializable]
    public class LoginCodeResponse
    {
        [JsonProperty("login_code")]
        public string LoginCode { get; set; }

        [JsonProperty("expiration_duration")]
        public string ExpirationDuration { get; set; } // Stored as string in ISO8601 format
    }

    [Serializable]
    public class LoginWithCodeRequest
    {
        [JsonProperty("login_code")]
        public string LoginCode { get; set; }
    }

    [Serializable]
    public class AccessTokenResponse
    {
        [JsonProperty("access_token")]
        public string AccessToken { get; set; }

        [JsonProperty("token_type")]
        public string TokenType { get; set; }
    }

    public class FurnitureInfo
    {
        [JsonProperty("uuid")]
        public string Uuid { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("user_uuid")]
        public string UserUuid { get; set; }

        [JsonProperty("username")]
        public string Username { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("scale_type")]
        public int ScaleType { get; set; }

        [JsonProperty("scale_x")]
        public double ScaleX { get; set; }

        [JsonProperty("scale_y")]
        public double ScaleY { get; set; }

        [JsonProperty("scale_z")]
        public double ScaleZ { get; set; }

        [JsonProperty("source")]
        public string Source { get; set; }
        public override string ToString()
        {
            return $"Uuid: {Uuid}, Name: {Name}, UserUuid: {UserUuid}, Username: {Username}, Description: {Description}, ScaleType: {ScaleType}, ScaleX: {ScaleX}, ScaleY: {ScaleY}, ScaleZ: {ScaleZ}, Source: {Source}";
        }
    }

    public class FurnitureInfoList
    {
        [JsonProperty("furniture_infos")]
        public List<FurnitureInfo> FurnitureInfos { get; set; }

        [JsonProperty("total_furniture_count")]
        public int TotalFurnitureCount { get; set; }
        public override string ToString()
        {
            var furnitureInfos = FurnitureInfos.Aggregate("", (current, furnitureInfo) => current + (furnitureInfo + "\n"));
            return $"FurnitureInfos:\n{furnitureInfos}, TotalFurnitureCount: {TotalFurnitureCount}";
        }
    }

    [Serializable]
    public class UserInfo
    {
        [JsonProperty("uuid")]
        public string Uuid { get; set; }

        [JsonProperty("username")]
        public string Username { get; set; }

        [JsonProperty("created_at")]
        public string CreatedAt { get; set; }
    }
    [Serializable]
    public class CreatePollingControllerSessionResponse
    {
        [JsonProperty("detail")]
        public string Detail { get; set; }
    }

}