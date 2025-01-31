using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class RandomMeshGenerator : MonoBehaviour
{
    [SerializeField] private int pointCount = 30; 
    [SerializeField] private float radius = 5f; 
    
    private MeshFilter meshFilter;

    void Start()
    {
        GenerateMesh();
    }

    void GenerateMesh()
    {
        GameObject meshObject = new GameObject("GeneratedMesh");
        meshFilter = meshObject.AddComponent<MeshFilter>();
        meshObject.AddComponent<MeshRenderer>().material = new Material(Shader.Find("Standard"));

        List<Vector3> points = GeneratePointsOnSphere(pointCount, radius);
        List<int> triangles = IncrementalConvexHull.GenerateHull(points);

        Mesh generatedMesh = new Mesh();
        generatedMesh.vertices = points.ToArray();
        generatedMesh.triangles = triangles.ToArray();
        generatedMesh.RecalculateNormals();

        meshFilter.mesh = generatedMesh;
    }

    List<Vector3> GeneratePointsOnSphere(int count, float sphereRadius)
    {
        HashSet<Vector3> uniquePoints = new HashSet<Vector3>();
        float phi = Mathf.PI * (3f - Mathf.Sqrt(5f));
        
        for (int i = 0; i < count; i++)
        {
            float y = 1f - (i / (float)(count - 1)) * 2f;
            float radiusAtY = Mathf.Sqrt(1f - y * y);
            float theta = phi * i;
            float x = Mathf.Cos(theta) * radiusAtY;
            float z = Mathf.Sin(theta) * radiusAtY;
            uniquePoints.Add(new Vector3(x, y, z) * sphereRadius);
        }
        return uniquePoints.ToList();
    }
}

public static class IncrementalConvexHull
{
    public static List<int> GenerateHull(List<Vector3> points)
    {
        if (points.Count < 4)
            return new List<int>();
        
        HashSet<Triangle> hull = new HashSet<Triangle>();
        HashSet<Edge> edges = new HashSet<Edge>();
        List<Vector3> workingSet = points.Distinct().ToList(); 
        
        hull.Add(new Triangle(workingSet[0], workingSet[1], workingSet[2]));
        hull.Add(new Triangle(workingSet[0], workingSet[2], workingSet[3]));
        hull.Add(new Triangle(workingSet[0], workingSet[3], workingSet[1]));
        hull.Add(new Triangle(workingSet[1], workingSet[3], workingSet[2]));
        
        for (int i = 4; i < workingSet.Count; i++)
        {
            Vector3 point = workingSet[i];
            HashSet<Triangle> toRemove = new HashSet<Triangle>();
            HashSet<Edge> newEdges = new HashSet<Edge>();
            
            foreach (var triangle in hull)
            {
                if (triangle.IsPointAbove(point))
                {
                    toRemove.Add(triangle);
                    foreach (var edge in triangle.GetEdges())
                    {
                        if (!edges.Contains(edge))
                            newEdges.Add(edge);
                    }
                }
            }
            
            if (toRemove.Count == 0) continue;
            
            hull.ExceptWith(toRemove);
            
            foreach (var edge in newEdges)
            {
                hull.Add(new Triangle(edge.A, edge.B, point));
            }
        }
        
        List<int> indices = new List<int>();
        
        foreach (var triangle in hull)
        {
            indices.AddRange(triangle.GetIndices(workingSet));
        }
        
        return indices;
    }
}

public class Triangle
{
    public Vector3 A, B, C;
    
    public Triangle(Vector3 a, Vector3 b, Vector3 c)
    {
        A = a; B = b; C = c;
    }
    
    public bool IsPointAbove(Vector3 point)
    {
        Vector3 normal = Vector3.Cross(B - A, C - A);
        return Vector3.Dot(normal, point - A) > 0;
    }
    
    public List<Edge> GetEdges()
    {
        return new List<Edge> { new Edge(A, B), new Edge(B, C), new Edge(C, A) };
    }
    
    public List<int> GetIndices(List<Vector3> points)
    {
        return new List<int> { points.IndexOf(A), points.IndexOf(B), points.IndexOf(C) };
    }
}

public struct Edge
{
    public Vector3 A, B;
    
    public Edge(Vector3 a, Vector3 b)
    {
        A = a; B = b;
    }
    
    public override bool Equals(object obj)
    {
        if (!(obj is Edge)) return false;
        Edge other = (Edge)obj;
        return (A == other.A && B == other.B) || (A == other.B && B == other.A);
    }
    
    public override int GetHashCode()
    {
        return A.GetHashCode() ^ B.GetHashCode();
    }
}
