#if UNITY_EDITOR
#endif
using UnityEngine;
using System;
using System.Xml;
using System.Collections;
using System.IO;
using UnityEditor.PackageManager.Requests;
using UnityEngine.Networking;
using UnityEngine.UI;

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

    public void GetLoginCode(Action<LoginCodeResponse> callback = null) {
        StartCoroutine(_mrServiceWrapper.GetLoginCode((response) => {
            loginCode = response.login_code;
            loginCodeExpirationDuration = XmlConvert.ToTimeSpan(response.expiration_duration);
            callback?.Invoke(response);
        }));
    }
    public void LoginWithLoginCode(Action<AccessTokenResponse> callback = null) {
        if (string.IsNullOrEmpty(loginCode)) {
            Debug.Log("Empty login code.");
            return;
        }
        StartCoroutine(_mrServiceWrapper.LoginWithCode(loginCode, (response) => {
            accessToken = response.access_token;
            callback?.Invoke(response);
        }));
    }
    public void GetSelfInfo(Action<UserInfo> callback = null) {
        if (string.IsNullOrEmpty(accessToken)) {
            Debug.Log("Empty user uuid");
            return;
        }
        StartCoroutine(_mrServiceWrapper.GetSelfInfo(accessToken, (response) => {
            selfInfo = response;
            userUuid = response.uuid;
            callback?.Invoke(response);
        }));
    }
    public void Logout() {
        accessToken = "";
    }
    public void GetFurnitureInfo(uint skip, uint limit, Action<FurnitureInfoList> callback = null) {
        StartCoroutine(_mrServiceWrapper.GetFurnitureInfo(skip, limit, (res) => {
            furnitureInfoList = res;
            callback?.Invoke(res);
        }));
    }
    public void GetAllFurnitureInfoByUser(Action<FurnitureInfoList> callback = null) {
        if (string.IsNullOrEmpty(accessToken)) {
            Debug.Log("Empty user uuid");
            return;
        }
        StartCoroutine(_mrServiceWrapper.GetAllFurnitureInfoByUser(userUuid, (res) => {
            furnitureInfoByUserList = res;
            callback?.Invoke(res);
        }));
    }
    public void GetObjById(string uuid, Action<string> callback = null) {
        
        StartCoroutine(_mrServiceWrapper.GetFurnitureOBJ(uuid, (res) => {
            obj_string = res;
            callback?.Invoke(res);
        }));
    }
    public void GetTaskStatusById(string taskId, Action<TaskStatus> callback) {
        StartCoroutine(_mrServiceWrapper.GetTaskStatus(taskId, (TaskStatus taskStatus) => {
            callback(taskStatus);
        }));
    }
    public void GetPreviewById(string uuid, Action<byte[]> callback = null) {

        StartCoroutine(_mrServiceWrapper.GetTaskPreview(uuid, return_png:true, (res) => {
            Display(res);
            callback?.Invoke(res);
        }));
    }
    public void Display(byte[] imageBytes) {
        Texture2D texture = new Texture2D(2, 2);
        if (texture.LoadImage(imageBytes)) {
            rawImage.texture = texture;
            rawImage.rectTransform.sizeDelta = new Vector2(texture.width, texture.height);
        }
    }
}