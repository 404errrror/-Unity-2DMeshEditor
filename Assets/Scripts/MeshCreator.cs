using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshFilter))]
public class MeshCreator : MonoBehaviour
{
    public Sprite sprite;
    [Space(10)]
    [SerializeField]
    Color color = Color.white;
    [Space(10)]
    [Tooltip("적용이 안된다면 Material의 Render Queue를 확인해주세요.")]
    public int sortingOrder;
    [Space(10)]
    public Vector2 offset;
    public Vector2 scale;
    [Space(10)]
    public bool showMeshLine;

#if UNITY_EDITOR
    // Undo.RecordObject()는 직렬화 된 데이터만 저장하므로.
    [SerializeField] [HideInInspector] List<Vector3> verticesList;
    [SerializeField] [HideInInspector] List<Vector2> uvList;
    [SerializeField] [HideInInspector] List<int> trianglesList;
    [SerializeField] [HideInInspector] Vector3 lowestVertex;
    [SerializeField] [HideInInspector] List<int> outlineIndex;
                                       List<bool> removeAble;
#endif

    MeshFilter myMesh;
    MeshRenderer myMeshRender;

    bool isLoaded = false;
    public bool IsLoaded
    {
        get { return isLoaded; }
    }
    public bool ShowMeshLine
    {
        get { return showMeshLine; }
    }


    public void Initialize()
    {
        verticesList = new List<Vector3>();
        uvList = new List<Vector2>();
        trianglesList = new List<int>();
        outlineIndex = new List<int>();
        myMesh = GetComponent<MeshFilter>();
        myMeshRender = GetComponent<MeshRenderer>();
        removeAble = new List<bool>();
    }

    public void MeshUpdate()
    {
        myMeshRender.sortingOrder = sortingOrder;
        myMeshRender.sharedMaterial.color = color;

        for (int i = 0; i < uvList.Count; ++i)
        {
            uvList[i] = UvCalculate(verticesList[i]);
        }
        Debug.Log("2");

        ApplyList();
    }

    public void Reset()
    {
        verticesList.Clear();
        uvList.Clear();
        trianglesList.Clear();
        outlineIndex.Clear();
        offset = Vector2.zero;
        scale = Vector2.one;

        if (sprite == null)
        {
            Debug.LogError("Sprite가 설정되지 않았습니다. Inspector창에 Mesh Creator하위에 있는 sprite를 설정해주세요.");
            return;
        }
        if (myMeshRender.sharedMaterial == null)
        {
            Debug.LogError("Material이 설정되지 않았습니다. Mesh Renderer의 Material를 설정해주세요.");
            return;
        }

        SetTexture();

        // 메쉬 생성.
        // 꼭 시계방향으로 생성하기
        InsertTriangle(
            new Vector3(0.5f, -0.5f, 0f),
            new Vector3(-0.5f, -0.5f, 0f),
            new Vector3(0, 0.5f, 0f)
            );

        color = Color.white;
    }
    public void ResetToRect()
    {
        verticesList.Clear();
        uvList.Clear();
        trianglesList.Clear();
        outlineIndex.Clear();
        offset = Vector2.zero;
        scale = Vector2.one;

        if (sprite == null)
        {
            Debug.LogError("Sprite가 설정되지 않았습니다. Inspector창에 Mesh Creator하위에 있는 Sprite를 설정해주세요.");
            return;
        }
        if (myMeshRender.sharedMaterial == null)
        {
            Debug.LogError("Material이 설정되지 않았습니다. Mesh Renderer의 Material를 설정해주세요.");
            return;
        }

        SetTexture();

        // 메쉬 생성.
        // 꼭 시계방향으로 생성하기
        InsertTriangle(
            new Vector3(0.5f, 0.5f, 0f),
            new Vector3(0.5f, -0.5f, 0f),
            new Vector3(-0.5f, -0.5f, 0f)
            );
        InsertTriangle(
            0, 2,
          new Vector3(-0.5f, 0.5f, 0f)
          );
        color = Color.white;
    }

    public void LoadMeshData()
    {
        isLoaded = true;
        myMesh = GetComponent<MeshFilter>();
        myMeshRender = GetComponent<MeshRenderer>();
    }


    public void VertexModify(int index)                     // index의 Vertex를 변경시킵니다.
    {
        Vector3 modifyPosition = MouseInfo.Position - transform.position;
        modifyPosition.z = 0;       // z값은 바뀌면 안되므로

        verticesList[index] = modifyPosition;
        uvList[index] = UvCalculate(verticesList[index]);

        ApplyList();
    }
    public void RemoveVertex(int index)                     // Vertex를 삭제합니다.
    {
        // Outline 삭제
        for (int i = 0; i < outlineIndex.Count; ++i)
        {
            if (outlineIndex[i] == index)
                outlineIndex.Remove(outlineIndex[i]);

        }
        for (int i = 0; i < outlineIndex.Count; ++i)
            if (outlineIndex[i] > index)                        // 만약 3번째 Vertex를 삭제하게 되면 4번째 Vertex가 3번째로 바뀌게 되므로.
                --outlineIndex[i];
        if (index == 0)
            outlineIndex.Add(outlineIndex[0]);                  // 항상 마지막으로 돌아가야하므로.

        // Triangles 삭제
        for (int i = 0; i < trianglesList.Count; ++i)
        {
            if (trianglesList[i] == index)
            {
                if (i % 3 == 0)
                {
                    trianglesList.RemoveRange(i, 3);
                    Debug.Log("fds");
                }
                else if (i % 3 == 1)
                    trianglesList.RemoveRange(i - 1, 3);
                else
                    trianglesList.RemoveRange(i - 2, 3);

                if (i > 2)
                    i -= 3;
            }

        }
        for (int i = 0; i < trianglesList.Count; ++i)
            if (trianglesList[i] > index)
                --trianglesList[i];

        uvList.Remove(uvList[index]);
        verticesList.Remove(verticesList[index]);


        ApplyList();

    }


    public int FindMinDistanceVertex()                                                         // 마우스와 가장 가까운 Vertex 검색하고 그 index를 반환합니다. 
    {
        int minIndex = 0;
        float minDistance = Vector3.Distance(MouseInfo.Position, verticesList[0] + transform.position);

        for (int i = 0; i < verticesList.Count; ++i)
        {
            float distance = Vector3.Distance(MouseInfo.Position, verticesList[i] + transform.position);
            if (minDistance > distance)
            {
                minIndex = i;
                minDistance = distance;
            }
        }

        return minIndex;
    }
    public void FindMinDistanceMidVertex(int minVertex, out int minMidVertex)                   // 삭제 예정. 이유는 AbleEditting와 같음. 
    {
        int frontVertex = 0, backVertex = 0;

        if (minVertex == 0)
        {
            frontVertex = outlineIndex[outlineIndex.Count - 2];     // -1 은 0 이므로.
            backVertex = outlineIndex[1];
        }
        else
            for (int i = 0; i < outlineIndex.Count; ++i)
            {
                if (outlineIndex[i] == minVertex)
                {
                    frontVertex = outlineIndex[i - 1];
                    backVertex = outlineIndex[i + 1];
                    break;
                }
            }

        float frontDist, backDist;
        frontDist = Vector3.Distance(verticesList[frontVertex] + transform.position, MouseInfo.Position);
        backDist = Vector3.Distance(verticesList[backVertex] + transform.position, MouseInfo.Position);

        // Return
        if (frontDist < backDist)
            minMidVertex = frontVertex;
        else
            minMidVertex = backVertex;

    }
    public Vector3 FindMinDistanceMidVertex()                                                      // 엣지의 중심점 중 마우스와 가장 가까운 점의 위치를 반환합니다. 
    {
        Vector3 minVertex = (verticesList[outlineIndex[0]] + verticesList[outlineIndex[1]]) * 0.5f + transform.position;
        float minDistance = Vector3.Distance(minVertex, MouseInfo.Position);

        for (int i = 1; i < outlineIndex.Count - 1; ++i)
        {
            Vector3 nowVectex = (verticesList[outlineIndex[i]] + verticesList[outlineIndex[i + 1]]) * 0.5f + transform.position;
            float nowDistance = Vector3.Distance(nowVectex, MouseInfo.Position);
            if (nowDistance < minDistance)
            {
                minVertex = nowVectex;
                minDistance = nowDistance;
            }
        }

        return minVertex - transform.position;
    }
    public bool FindLinkedVertex(Vector3 midVertexPosition, out int vertex1, out int vertex2)   // 중심점과 연결된 버텍스의 인덱스를 반환합니다. 
    {

        for (int i = 0; i < outlineIndex.Count - 1; ++i)
        {
            Vector3 nowMidPos = (verticesList[outlineIndex[i]] + verticesList[outlineIndex[i + 1]]) * 0.5f;
            if (nowMidPos == midVertexPosition)
            {
                vertex1 = outlineIndex[i];
                vertex2 = outlineIndex[i + 1];
                return true;
            }
        }

        if ((verticesList[outlineIndex[0]] + verticesList[outlineIndex[outlineIndex.Count - 1]]) * 0.5f == midVertexPosition)
        {
            vertex1 = outlineIndex[0];
            vertex2 = outlineIndex[outlineIndex.Count - 1];
            return true;
        }
        else
        {
            vertex1 = -1;
            vertex2 = -1;
            return false;
        }
    }

    public float VertexDistance(int vertexIndex)             // 마우스와 Vertex의 거리를 반환합니다.
    {
        return Vector3.Distance(MouseInfo.Position, verticesList[vertexIndex] + transform.position);
    }

    public Mesh GetMesh()
    {
        return myMesh.sharedMesh;
    }
    List<int> OutLineLoad()                               // 현재 Mesh에서 OutLine계산해서 List를 반환합니다.
    {
        // 알고리즘
        // 한 점을 찾고 그 점과 이어지는 점을 찾는다.
        // 찾았다면 삼각형 리스트에서 그 선분이 단 한개만 있다면 그 선은 외각선이다.
        // 예를들면 1 - 2가 연결되있다고 치자. 삼각형 리스트에 1 - 2 또는 2 - 1 가 단 한개만 존재한다면 이 선은 외각선이다.


        List<int> outlineList = new List<int>();
        int searchIndex = 0;

        for (int i = 0; i < verticesList.Count; ++i)
        {
            outlineList.Add(searchIndex);

            // 검색중인 버텍스와 그 버텍스와 연결된 버텍스를 찾는다.
            for (int j = 0; j < trianglesList.Count; ++j)
            {
                if (trianglesList[j] == searchIndex)                             // 그 버텍스를 찾았다면
                {
                    int linkedCount = 0;
                    int linkedIndex;

                    if (j % 3 == 2)                                             // 그 버텍스와 연결된 버텍스를 찾는다. 1 - 2는 물론 1 - ? - 2 도 될 수 있으므로 if문 처리.
                        linkedIndex = j - 2;
                    else
                        linkedIndex = j + 1;

                    for (int k = 0; k < trianglesList.Count; ++k)               // 삼각형 리스트를 순환하면서 그 버텍스와 연결된 지점들을 찾는다.
                    {
                        if (k % 3 == 2)                                         // 앞서 말했던 것과 같이 1 - ? - 2 도 될 수 있으므로.
                        {
                            if (trianglesList[k] == trianglesList[j] &&
                                trianglesList[k - 2] == trianglesList[linkedIndex] ||
                                trianglesList[k] == trianglesList[linkedIndex] &&           // 1 - 2 가 아니라 2 - 1로 뒤집어서 연결되 있을 수 도 있으므로
                                trianglesList[k - 2] == trianglesList[linkedIndex])
                            {
                                ++linkedCount;
                            }
                        }

                        else
                        {
                            if (trianglesList[k] == trianglesList[j] &&
                                trianglesList[k + 1] == trianglesList[linkedIndex] ||
                                trianglesList[k] == trianglesList[linkedIndex] &&
                                trianglesList[k + 1] == trianglesList[j])
                            {
                                ++linkedCount;
                            }
                        }

                        if (linkedCount > 1)        // 1보다 크면 외각선이 아니므로 더이상 돌 필요가 없다.
                            break;
                    }

                    // 중복으로 연결된 구간이 있다면 다른 연결된 지점을 찾는다.
                    if (linkedCount > 1)
                        continue;
                    else
                    {
                        //outlineList.Add(searchIndex);
                        searchIndex = trianglesList[linkedIndex];           // 이제 연결된 버텍스를 검색한다.
                        break;
                    }
                }

            }
        }
        outlineList.Add(0);     // 처음으로 연결되야 하므로.
        return outlineList;
    }
    // 삭제예정. 사실 이딴짓 할 필요없었다. 시작할 때마다 초기화하는 바람에 저장안된 것처럼 보였을 뿐.
    public List<int> GetOutLineVertice()
    {
        return outlineIndex;
    }
    public MeshRenderer GetMeshRenderer()                           // 오브젝트의 MeshRenderer를 반환합니다.
    {
        return myMeshRender;
    }

    public int FindOutLineIndex(int vertexIndex)           // 해당 Vertex가 outLineList에 어디에 위치하는지 인덱스를 반환합니다. 
    {
        // 0 번째는 무조건 0으로. 
        // 0 번째는 다른 수와 달리 2개가 존재하고 0번째에 넣는 것이 아닌 맨 끝에 넣어야한다.
        for (int i = 1; i < outlineIndex.Count; ++i)
        {
            if (vertexIndex == outlineIndex[i])
                return i;
        }

        return -1;
    }


    public void CheckRemoveAbleAll()                        // 삭제할 수 있는 Vertex인지 검사합니다.
    {
        removeAble.Clear();
        for (int i = 0; i < verticesList.Count; ++i)
        {
            int linkedCount = 0;        // 선이 연결 된 것이 아닌, 삼각형이 연결된 수를 의미.
            foreach (var triIt in trianglesList)
            {
                if (triIt == i)
                    ++linkedCount;
            }

            if (linkedCount > 1)
                removeAble.Add(false);
            else
                removeAble.Add(true);
        }
    }
    public List<bool> GetRemoveAble()                             // Vertex가 삭제 될 수 있는지에 관한 데이터 리스트를 반환합니다. 
    {
        return removeAble;
    }

    public void SetTexture()                                // 텍스쳐를 설정합니다.
    {

        Texture2D myTexture = new Texture2D((int)sprite.rect.width, (int)sprite.rect.height);

        for (int y = 0; y < sprite.rect.height; ++y)
            for (int x = 0; x < sprite.rect.width; ++x)
                myTexture.SetPixel(x, y, sprite.texture.GetPixel((int)sprite.rect.xMin + x, (int)sprite.rect.yMin + y));


        myTexture.Apply();

        myMeshRender.material = new Material(myMeshRender.sharedMaterial);      // 메터리얼 하나 생성. 공유된 메터리얼에 적용시키면 망함.
        myMeshRender.sharedMaterial.mainTexture = myTexture;
    }

    void FindLowestVertex()                          // 버텍스 리스트중에 가장 낮은 x와 y를 각각 찾아서 lowestVertex에 대입합니다.
    {
        lowestVertex = verticesList[0];
        for (var i = 1; i < verticesList.Count; ++i)
        {
            if (verticesList[i].x < lowestVertex.x)
                lowestVertex.x = verticesList[i].x;
            if (verticesList[i].y < lowestVertex.y)
                lowestVertex.y = verticesList[i].y;
        }
    }
    Vector2 UvCalculate(Vector3 vertex)                 // vertex에 해당되는 uv값을 계산합니다. 
    {
        return new Vector2((vertex.x - 0.5f) / scale.x, (vertex.y - 0.5f) / scale.y) - offset;
    }
    void ApplyList()                                 // verteices, uv, triangle 리스트들을 오브젝트 메쉬에 적용시킵니다.
    {
        myMesh.mesh = new Mesh();
        myMesh.sharedMesh.vertices = verticesList.ToArray();
        myMesh.sharedMesh.triangles = trianglesList.ToArray();
        myMesh.sharedMesh.uv = uvList.ToArray();
    }


    void InsertMesh(Vector3 first, Vector3 second, Vector3 third, Vector3 fourth)   // 사각형 Mesh를 추가합니다.
    {
        int vertexAmount = verticesList.Count;
        int triAmount = trianglesList.Count;

        verticesList.Add(first);
        verticesList.Add(second);
        verticesList.Add(third);
        verticesList.Add(fourth);

        outlineIndex.Add(1);
        outlineIndex.Add(0);
        outlineIndex.Add(2);
        outlineIndex.Add(3);
        outlineIndex.Add(1);

        trianglesList.Add(vertexAmount + 1);
        trianglesList.Add(vertexAmount);
        trianglesList.Add(vertexAmount + 2);
        trianglesList.Add(vertexAmount + 1);
        trianglesList.Add(vertexAmount + 3);
        trianglesList.Add(vertexAmount + 2);

        for (int i = 0; i < 4; ++i)
            uvList.Add(UvCalculate(verticesList[vertexAmount + i]));


        ApplyList();
    }
    void InsertMesh(int firstIndex, int secondIndex, Vector3 third, Vector3 fourth)
    {
        int vertexAmount = verticesList.Count;      // vertexAmount = thirdIndex
        int triAmount = trianglesList.Count;
        int insertIndex = FindOutLineIndex(secondIndex);

        verticesList.Add(third);
        verticesList.Add(fourth);

        outlineIndex.Insert(insertIndex, vertexAmount);///////////////
        outlineIndex.Insert(insertIndex + 1, vertexAmount + 1);

        trianglesList.Add(secondIndex);
        trianglesList.Add(firstIndex);
        trianglesList.Add(vertexAmount);
        trianglesList.Add(secondIndex);
        trianglesList.Add(vertexAmount + 1);
        trianglesList.Add(vertexAmount);

        for (int i = 0; i < 2; ++i)
            uvList.Add(UvCalculate(verticesList[vertexAmount + i]));

        ApplyList();
    }
    void InsertTriangle(Vector3 first, Vector3 second, Vector3 third)               // 초기화 할때만 써야함.
    {
        verticesList.Add(first);
        verticesList.Add(second);
        verticesList.Add(third);


        outlineIndex.Add(0);
        outlineIndex.Add(1);
        outlineIndex.Add(2);
        outlineIndex.Add(0);

        trianglesList.Add(0);
        trianglesList.Add(1);
        trianglesList.Add(2);

        for (int i = 0; i < 3; ++i)
            uvList.Add(UvCalculate(verticesList[i]));
    }
    public void InsertTriangle(int firstIndex, int secondIndex, Vector3 third)      // 삼각형 Mesh를 추가합니다.
    {
        int vertexAmount = verticesList.Count;      // vertexAmount = thirdIndex
        int insertIndex = -1;

        verticesList.Add(third);


        // 시계 방향으로 삼각형을 그리기 위해
        for (int i = 0; i < outlineIndex.Count; ++i)
        {
            if (outlineIndex[i] == firstIndex)
            {
                if (outlineIndex[i + 1] == secondIndex)
                {
                    trianglesList.Add(secondIndex);
                    trianglesList.Add(firstIndex);
                    trianglesList.Add(vertexAmount);

                    insertIndex = FindOutLineIndex(secondIndex);
                    break;
                }
                else
                {

                    trianglesList.Add(firstIndex);
                    trianglesList.Add(secondIndex);
                    trianglesList.Add(vertexAmount);

                    insertIndex = FindOutLineIndex(firstIndex);
                    break;
                }
            }
        }
        outlineIndex.Insert(insertIndex, vertexAmount);
        uvList.Add(UvCalculate(verticesList[vertexAmount]));

        ApplyList();
    }

    public void SetPolygonCollider()
    {
        PolygonCollider2D coll = transform.GetComponent<PolygonCollider2D>();
        Vector2[] outlinePoint = new Vector2[outlineIndex.Count];

        if (coll == null)
            coll = Undo.AddComponent<PolygonCollider2D>(gameObject);
        else
            Undo.RecordObject(coll, "Set PolygonCollider2D");

        for (int i = 0; i < outlinePoint.Length; ++i)
            outlinePoint[i] = verticesList[outlineIndex[i]];

        coll.points = outlinePoint;
    }
}
