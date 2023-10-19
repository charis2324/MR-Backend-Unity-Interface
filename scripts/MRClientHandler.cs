#if UNITY_EDITOR
#endif
using UnityEngine;
using System;
using System.Xml;
using UnityEngine.UI;
using MRBackend;
using System.Threading;

public class MRClientHandler : MonoBehaviour
{
    [SerializeField]
    private MRServiceWrapper _mrServiceWrapper;
    [SerializeField]
    public string accessToken = "";
    public string loginCode = "";
    public string userUuid = "";
    public string obj_string = "";
    public TimeSpan loginCodeExpirationDuration;
    public FurnitureInfoList furnitureInfoList;
    public FurnitureInfoList furnitureInfoByUserList;
    public UserInfo selfInfo;
    public Canvas canvas; // assign in Inspector
    public RawImage rawImage;

    private void Start()
    {
        //ListenToController(accessToken);
    }
    public void ListenToController(string accessToken)
    {
        CancellationTokenSource cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMinutes(1));
        //_mrServiceWrapper.ListenToController("eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJhc2QiLCJleHAiOjE2OTc2MTk4Njh9.Qxw3OjpogesI-GQgd61wGHWf5Pg6z6Xin4G9pbGH32Q", cts.Token);
    }
    public void GetFurnitureInfoById(string uuid, Action<FurnitureInfo> callback = null)
    {
        StartCoroutine(_mrServiceWrapper.GetFurnitureInfoById(uuid, (info) =>
        {
            callback?.Invoke(info);
        }));
    }
    public void GetLoginCode(Action<LoginCodeResponse> callback = null)
    {
        StartCoroutine(_mrServiceWrapper.GetLoginCode((response) =>
        {
            loginCode = response.LoginCode;
            loginCodeExpirationDuration = XmlConvert.ToTimeSpan(response.ExpirationDuration);
            callback?.Invoke(response);
        }));
    }
    public void LoginWithLoginCode(Action<AccessTokenResponse> callback = null)
    {
        if (string.IsNullOrEmpty(loginCode))
        {
            Debug.Log("Empty login code.");
            return;
        }
        StartCoroutine(_mrServiceWrapper.LoginWithCode(loginCode, (response) =>
        {
            accessToken = response.AccessToken;
            callback?.Invoke(response);
        }));
    }
    public void GetSelfInfo(Action<UserInfo> callback = null)
    {
        if (string.IsNullOrEmpty(accessToken))
        {
            Debug.Log("Empty user uuid");
            return;
        }
        StartCoroutine(_mrServiceWrapper.GetSelfInfo(accessToken, (response) =>
        {
            selfInfo = response;
            userUuid = response.Uuid;
            Debug.Log(string.Format("User uuid: {0}", userUuid));
            callback?.Invoke(response);

        }));
    }
    public void Logout()
    {
        accessToken = "";
    }
    public void GetFurnitureInfo(uint skip, uint limit, Action<FurnitureInfoList> callback = null)
    {
        StartCoroutine(_mrServiceWrapper.GetFurnitureInfo(skip, limit, (res) =>
        {
            furnitureInfoList = res;
            callback?.Invoke(res);
        }));
    }
    public void GetAllFurnitureInfoByUser(Action<FurnitureInfoList> callback = null)
    {
        if (string.IsNullOrEmpty(accessToken))
        {
            Debug.Log("Empty user uuid");
            return;
        }
        StartCoroutine(_mrServiceWrapper.GetAllFurnitureInfoByUser(userUuid, (res) =>
        {
            furnitureInfoByUserList = res;
            callback?.Invoke(res);
        }));
    }
    public void GetFurnitureOBJ(string uuid, Action<string> callback = null)
    {

        StartCoroutine(_mrServiceWrapper.GetFurnitureOBJ(uuid, (res) =>
        {
            obj_string = res;
            callback?.Invoke(res);
        }));
    }
    public void GetFurniturePreview(string uuid, Action<byte[]> callback = null)
    {

        StartCoroutine(_mrServiceWrapper.GetFurniturePreview(uuid, return_png: true, (res) =>
        {
            Display(res);
            callback?.Invoke(res);
        }));
    }
    public void Display(byte[] imageBytes)
    {
        Texture2D texture = new Texture2D(2, 2);
        if (texture.LoadImage(imageBytes))
        {
            rawImage.texture = texture;
            rawImage.rectTransform.sizeDelta = new Vector2(texture.width, texture.height);
        }
    }
}