#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System;
using System.Collections;

public class MRClientHandler : MonoBehaviour
{
    public MRServiceWrapper generationServiceWrapper;
    private void Start()
    {
        //DebugDisplay debugDisplay = FindObjectOfType<DebugDisplay>();
        //debugDisplay.AddMessage("Generate an apple");
        GenerateModelMesh("an apple", 15f);
    }
    public void LoadOBJFromFileInEditor()
    {
#if UNITY_EDITOR
        string path = EditorUtility.OpenFilePanel("Overwrite with .obj", "", "obj");
        if (path.Length != 0) {
            var mesh = LoadOBJFromFile(path);

            // Add the mesh to a new GameObject.
            var meshFilter = gameObject.GetComponent<MeshFilter>();
            if (meshFilter == null) {
                meshFilter = gameObject.AddComponent<MeshFilter>();
            }
            
            meshFilter.mesh = mesh;

            // Add a MeshRenderer so the mesh becomes visible.
            var meshRenderer = gameObject.GetComponent<MeshRenderer>();
            if (meshRenderer == null) {
                meshRenderer = gameObject.AddComponent<MeshRenderer>();
            }
            // Create a default Material and assign it to the MeshRenderer.
            //meshRenderer.material = new Material(Shader.Find("Standard"));

            meshRenderer.material = new Material(Shader.Find("Particles/Standard Surface"));
        }
#endif
    }

    public static Mesh LoadOBJFromFile(string path)
    {
        string[] lines = File.ReadAllLines(path);
        return OBJParseLines(lines);

    }
    public static Mesh LoadOBJFromString(string input)
    {
        string[] lines = input.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        return OBJParseLines(lines);
    }
    public static Mesh OBJParseLines(string[] lines)
    {

        List<Color> colors = new List<Color>();
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();

        foreach (string line in lines)
        {
            string[] data = line.Split(' ');

            switch (data[0])
            {
                case "v":
                    // Parse the x, y, z values and apply rotation.
                    Vector3 vertex = new Vector3(
                        float.Parse(data[1]),
                        float.Parse(data[2]),
                        float.Parse(data[3])
                    );

                    // Rotate the vertices -90 degrees around the x-axis.
                    vertex = Quaternion.Euler(-90, 0, 0) * vertex;

                    vertices.Add(vertex);
                    // Parse the RGB values.
                    colors.Add(new Color(
                        float.Parse(data[4]),
                        float.Parse(data[5]),
                        float.Parse(data[6])
                    ));
                    break;
                case "f":
                    triangles.Add(int.Parse(data[1]) - 1);
                    triangles.Add(int.Parse(data[2]) - 1);
                    triangles.Add(int.Parse(data[3]) - 1);
                    break;
            }
        }

        Mesh mesh = new Mesh();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.colors = colors.ToArray(); // Set the vertex colors.
        mesh.Optimize();
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        return mesh;
    }
    public void GenerateModelMesh(string prompt, float guidance_scale = 15f)
    {
        generationServiceWrapper = GetComponent<MRServiceWrapper>();
        if (generationServiceWrapper == null)
        {
            generationServiceWrapper = gameObject.AddComponent<MRServiceWrapper>();
        }
        generationServiceWrapper.GenerateMeshAndStartPolling(prompt, guidance_scale, 1000, (string objStr) =>
        {
            var newMesh = LoadOBJFromString(objStr);
            AppleGeneratedMesh(newMesh);
        });
    }

    void AppleGeneratedMesh(Mesh mesh)
    {
        // Add the mesh to a new GameObject.
        if (gameObject.GetComponent<MeshFilter>() == null)
        {
            gameObject.AddComponent<MeshFilter>();
        }
        var meshFilter = gameObject.GetComponent<MeshFilter>();
        meshFilter.mesh = mesh;

        if (gameObject.GetComponent<MeshRenderer>() == null)
        {
            gameObject.AddComponent<MeshRenderer>();
        }
        // Add a MeshRenderer so the mesh becomes visible.
        var meshRenderer = gameObject.GetComponent<MeshRenderer>();


        meshRenderer.material = new Material(Shader.Find("Particles/Standard Unlit"));
    }
    public void GetTaskStatusById(string taskId, Action<TaskStatus> callback) {
        StartCoroutine(generationServiceWrapper.GetTaskStatus(taskId, (TaskStatus taskStatus) => {
            callback(taskStatus);
        }));
    }
}