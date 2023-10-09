using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class OBJParser
{
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
    public static void AppleMesh(GameObject gameObject, Mesh mesh)
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
}
