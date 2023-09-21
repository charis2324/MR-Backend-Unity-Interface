using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MRClientHandler))]
public class MRClientHandlerEditor : Editor
{
    string textPrompt = "a recliner for relaxing";
    string taskId = "";
    string result = "";
    float guidanceScale = 15f;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        MRClientHandler myScript = (MRClientHandler)target;
        // Load OBJ File button
        if (GUILayout.Button("Load OBJ File"))
        {
            myScript.LoadOBJFromFileInEditor();
        }
        // Text prompt input field
        textPrompt = EditorGUILayout.TextField("Text Prompt", textPrompt);

        // Guidance scale input field
        guidanceScale = EditorGUILayout.FloatField("Guidance Scale", guidanceScale);



        // Generate Model Mesh button
        if (GUILayout.Button("Generate Model Mesh"))
        {
            myScript.GenerateModelMesh(textPrompt, guidanceScale);
        }

        // For call the rest of APIs
        taskId = EditorGUILayout.TextField("Task ID", taskId);
        if (GUILayout.Button("Check Task Status"))
        {
            Debug.Log("taskId " + taskId);

            myScript.GetTaskStatusById(taskId, (TaskStatus taskStatus) => {
                result = taskStatus.status;
            });
        }
        EditorGUILayout.LabelField("Result", result);
    }
}