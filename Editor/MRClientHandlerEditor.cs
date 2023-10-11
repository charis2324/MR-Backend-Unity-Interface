using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MRClientHandler))]
public class MRClientHandlerEditor : Editor
{
    uint skip = 0;
    uint limit = 10;
    string furnitureId = string.Empty;

    public override void OnInspectorGUI()
    {
        MRClientHandler myScript = (MRClientHandler)target;
        DrawDefaultInspector();
        EditorGUILayout.TextField("Login Code Expiration Duration", myScript.loginCodeExpirationDuration.ToString());

        if (GUILayout.Button("Get Login Code"))
        {
            myScript.GetLoginCode();
            Debug.Log(string.Format("Login Code: {0}", myScript.loginCode));
            Debug.Log(string.Format("Expire in: {0}", myScript.loginCodeExpirationDuration.ToString()));
        }
        if (GUILayout.Button("Login With Code"))
        {
            myScript.LoginWithLoginCode();
            Debug.Log(string.Format("Login Code: {0}", myScript.accessToken));
        }
        if (GUILayout.Button("Logout"))
        {
            myScript.Logout();
            Debug.Log(string.Format("Login Code: {0}", myScript.accessToken));
        }
        skip = (uint)EditorGUILayout.IntField("skip", (int)skip);
        limit = (uint)EditorGUILayout.IntField("limit", (int)limit);

        //GetSelfInfo
        if (GUILayout.Button("Get Self Info"))
        {
            myScript.GetSelfInfo((res) =>
            {
                Debug.Log(string.Format("User Info:\n{0}", myScript.userUuid.ToString()));
            });
        }
        if (GUILayout.Button("Get Furniture Info"))
        {
            myScript.GetFurnitureInfo(skip, limit, (res) =>
            {
                Debug.Log(string.Format("Furniture Info:\n{0}", myScript.furnitureInfoList.ToString()));
            });
        }
        if (GUILayout.Button("Get Furniture Info from Self"))
        {
            myScript.GetAllFurnitureInfoByUser((res) =>
            {
                Debug.Log(string.Format("My Furniture Info:\n{0}", myScript.furnitureInfoByUserList.ToString()));
            });
        }
        furnitureId = EditorGUILayout.TextField("Furniture ID", furnitureId);
        if (GUILayout.Button("Get Furniture OBJ"))
        {
            myScript.GetFurnitureOBJ(furnitureId, (res) =>
            {
                Debug.Log(string.Format("Furniture OBJ:\n{0}", myScript.obj_string));
                Mesh mesh = OBJParser.LoadOBJFromString(myScript.obj_string);
                OBJParser.AppleMesh(myScript.gameObject, mesh);
            });
        }
        if (GUILayout.Button("Get Furniture Preview"))
        {
            myScript.GetFurniturePreview(furnitureId, (res) =>
            {
                Debug.Log("Loaded Preview");
            });
        }

        if (EditorGUI.EndChangeCheck())
        {
            if (skip < 0)
                skip = 0;
            if (limit < 0)
                limit = 0;
            EditorUtility.SetDirty(myScript);
        }
    }
}