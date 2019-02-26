using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshFilter))]

public class MeshCreatorStudy : MonoBehaviour
{
    MeshFilter myMeshFilter;

    public Vector3[] vertice;
    // Use this for initialization
    void Start () {
        myMeshFilter = GetComponent<MeshFilter>();
   //     for(int i = 0; i < 1; ++i)
          CreateDoubleTexture();
        //CreateTriangle();
        Create();
	}

    void Update()
    {
        //CreateTriangle();
    }


    void Create()
    {
        Mesh tempMesh = new Mesh();
        tempMesh.vertices = new Vector3[]
        {
            new Vector3(-1, 0, 0),
            new Vector3(0, 1, 0),
            new Vector3(1, 0, 0),
            new Vector3(0, -1, 0),

            new Vector3(2, -1, 0) ,
            new Vector3(1, -2, 0)
        };

        tempMesh.triangles = new int[]
        {
            0,1,2,
            2,3,0,

            2,4,3,
            4,5,3
        };
        tempMesh.uv = new Vector2[]
        {
            new Vector2(0,0),
            new Vector2(0,1),
            new Vector2(1,1),
            new Vector2(1,0),

            new Vector2(2,1),
            new Vector2(2,0)

        };

        myMeshFilter.mesh = tempMesh;
    }

    void CreateTriangle()
    {
        Mesh tempMesh = new Mesh();
        tempMesh.vertices = new Vector3[]
        {
            new Vector3(-1,0,0),
            new Vector3(1,0,0),
            new Vector3(-1,2,0),
            new Vector3(1,2,0),
        };
        tempMesh.vertices = vertice;

        tempMesh.triangles = new int[]
        {
            0,2,1,
            2,3,1
        };

        tempMesh.uv = new Vector2[]
        {
            new Vector2(0,0),
            new Vector2(1,0),
            new Vector2((vertice[2].x + 1) * 0.5f,1),
            new Vector2(1 + (vertice[3].x - 1) * 0.5f,1)
        };

        myMeshFilter.mesh = tempMesh;
    }

    void CreateDoubleTexture()
    {
        MeshRenderer myRender = GetComponent<MeshRenderer>();
        Texture2D myTexture = myRender.material.mainTexture as Texture2D;
        Texture2D newTexture = new Texture2D(myTexture.width * 2, myTexture.height);

        for (int y = 0; y < myTexture.height; y++)
            for (int x = 0; x < myTexture.width; x++)
            {
                newTexture.SetPixel(x, y,
                new Color(myTexture.GetPixel(x, y).r, myTexture.GetPixel(x, y).g, myTexture.GetPixel(x, y).b)
                );

            }
        for (int y = 0; y < myTexture.height; y++)
        {
            for(int x = 0; x < myTexture.width; x++)
                newTexture.SetPixel(myTexture.width + x, y,
                new Color( myTexture.GetPixel(x,y).r, myTexture.GetPixel(x, y).g, myTexture.GetPixel(x, y).b)
                );
        }

        newTexture.Apply();

        myRender.material.mainTexture = newTexture;
    }

}
