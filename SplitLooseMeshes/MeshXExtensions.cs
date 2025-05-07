using Elements.Assets;
using Elements.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SplitLooseMeshes;

// Inspired by SubmeshLooseParts.cs: https://github.com/Xlinka/NeosPlus/blob/main/NEOSPlus/Logix/DynamicMesh/MeshOperations/SubmeshLooseParts.cs
public static class MeshExtensions
{
    public static List<MeshX> SplitByLooseParts(this MeshX meshX, out List<int> materialIndices, bool mergeDoubles = false, double cellSize = 0.001)
    {
        try
        {
            if (mergeDoubles)
                meshX = meshX.GetMergedDoubles(cellSize);

            List<MeshX> output = new List<MeshX>();
            materialIndices = new List<int>();

            int materialIndex = 0;
            foreach (var submesh in meshX.Submeshes)
            {
                if (submesh is not TriangleSubmesh tm)
                {
                    throw new ArgumentException($"Submesh { submesh.ToString() } was not a TriangleSubmesh, but was a { submesh.Topology.ToString() }");
                }

                var trySubAsoos = new int[tm.IndicieCount / 3];
                var current = 0;
                
                for (var i = 0; i < tm.IndicieCount / 3; i++)
                    GetNeighbor(ref trySubAsoos, tm, i, ref current, true);
                
                for (var i = 0; i < current; i++)
                {
                    MeshX looseMesh = new MeshX();
                    looseMesh.AddSubmesh(SubmeshTopology.Triangles);
                    for (var t = 0; t < tm.IndicieCount / 3; t++)
                    {
                        if ((trySubAsoos[t] - 1) != i) 
                            continue;
                        looseMesh.AddTriangle(tm.GetTriangle(t));
                    }
                    output.Add(looseMesh);
                    materialIndices.Add(materialIndex);
                }
                
                materialIndex++;
            }

            return output;

        }
        catch (Exception e)
        {
            UniLog.Error(e.Message);
        }

        materialIndices = Enumerable.Empty<int>().ToList();
        return Enumerable.Empty<MeshX>().ToList();
    }

    private static void GetNeighbor(ref int[] trySubAccess, TriangleSubmesh m, int index, ref int currentIndex,
        bool isNSub = false)
    {
        if (trySubAccess[index] != 0)
            return;
        if (isNSub) currentIndex++;
        trySubAccess[index] = currentIndex;
        var e = m.GetTriangle(index);
        foreach (var t in m.Mesh.Triangles.Where(x =>
                     x.Submesh == m
                     &&
                     (
                         x.Vertex0Index == e.Vertex0Index ||
                         x.Vertex0Index == e.Vertex1Index ||
                         x.Vertex0Index == e.Vertex2Index ||
                         x.Vertex1Index == e.Vertex0Index ||
                         x.Vertex1Index == e.Vertex1Index ||
                         x.Vertex1Index == e.Vertex2Index ||
                         x.Vertex2Index == e.Vertex0Index ||
                         x.Vertex2Index == e.Vertex1Index ||
                         x.Vertex2Index == e.Vertex2Index
                     ))) GetNeighbor(ref trySubAccess, m, t.Index, ref currentIndex);
    }
}
