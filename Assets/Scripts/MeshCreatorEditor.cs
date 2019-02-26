using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(MeshCreator))]
public class MeshCreatorEditor : Editor
{
    MeshCreator myMeshCreator = null;
    bool mouseLeftDown = false;
    bool isEditing = false;
    bool isRemoving = false;
    int minVert = 0, editingVert = -1;
    Vector3 minMidVertPos = Vector3.zero;

    void OnEnable()
    {
        myMeshCreator = (MeshCreator)target;
        myMeshCreator.sortingOrder = myMeshCreator.GetComponent<MeshRenderer>().sortingOrder;
        if (myMeshCreator.IsLoaded == false)
            myMeshCreator.LoadMeshData();
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        GUILayout.Space(10);
        GUILayout.Space(20);
        GUILayout.BeginHorizontal();

        if (GUILayout.Button("Reset To Triangle"))
        {
            SaveUndo("Reset " + target.name);

            myMeshCreator.Reset();
        }
        if (GUILayout.Button("Reset To Rectangle"))
        {
            SaveUndo("Reset To Rectangle " + target.name);

            myMeshCreator.ResetToRect();
        }
        if (GUILayout.Button("Refresh Texture"))
            myMeshCreator.SetTexture();
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (isEditing)
        {
            GUI.color = Color.red;
            if (GUILayout.Button("Edit Tool"))
            {
                isEditing = false;
                Tools.current = Tool.View;
            }
            GUI.color = Color.white;
        }
        else
        {
            if (GUILayout.Button("Edit Tool"))
            {
                if (isRemoving)
                    isRemoving = false;
                isEditing = true;
            }
        }

        if (isRemoving)
        {
            GUI.color = Color.red;
            if (GUILayout.Button("Remove Tool"))
            {
                isRemoving = false;
            }
            GUI.color = Color.white;
        }
        else
        {
            if (GUILayout.Button("Remove Tool"))
            {
                if (isEditing)
                    isEditing = false;
                isRemoving = true;
                myMeshCreator.CheckRemoveAbleAll();

            }
        }
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Set PolygonCollider2D"))
        {
            myMeshCreator.SetPolygonCollider();
        }
        GUILayout.EndHorizontal();

        myMeshCreator.MeshUpdate();
    }

    public void OnSceneGUI()
    {
        if (myMeshCreator.ShowMeshLine || isEditing)
            Gizmo();
        if (isEditing || isRemoving)
            MouseCheck();

        // MouseCheck()를 하면 우클릭, 휠클릭을 인식하지 못하기 때문에 EditingMode에서만.
        if (isEditing)
        {
            Tools.current = Tool.None;
            ControllHandle();
        }
        else if (isRemoving)
        {
            RemoveHandle();
            Tools.current = Tool.None;
        }

        if (isEditing == true && mouseLeftDown == true)
        {

            if (editingVert != -1)
                myMeshCreator.VertexModify(editingVert);
            Selection.activeGameObject = myMeshCreator.gameObject;
        }
        else if (isRemoving == true && mouseLeftDown == true)
        {
            Selection.activeGameObject = myMeshCreator.gameObject;
        }


    }



    void MouseCheck()                   // 마우스 좌클릭 체크.
    {
        Event currentEvent = Event.current;

        switch (currentEvent.rawType)
        {
            case EventType.MouseDown:
                if (currentEvent.button == 0 && mouseLeftDown == false)
                {
                    mouseLeftDown = true;
                    GUIUtility.hotControl = 0;
                    SaveUndo();
                    currentEvent.Use();

                    if (isEditing)
                    {
                        if (minMidVertPos != Vector3.zero && AbleEditting(minMidVertPos))
                        {
                            int frontVertex = 0, backVertex = 0;
                            myMeshCreator.FindLinkedVertex(minMidVertPos, out frontVertex, out backVertex);
                            myMeshCreator.InsertTriangle(frontVertex, backVertex, MouseInfo.Position - myMeshCreator.transform.position);
                            minMidVertPos = Vector3.zero;
                            editingVert = myMeshCreator.GetMesh().vertices.Length - 1;
                        }
                        else if (AbleEditting(minVert))
                            editingVert = minVert;
                        else
                            editingVert = -1;
                    }
                    else if (isRemoving && AbleEditting(minVert))
                    {
                        if (minMidVertPos == Vector3.zero && myMeshCreator.GetRemoveAble()[minVert] == true)
                        {
                            myMeshCreator.RemoveVertex(minVert);
                            myMeshCreator.CheckRemoveAbleAll();
                        }
                    }
                }
                break;

            case EventType.MouseUp:
                if (currentEvent.button == 0)
                {
                    mouseLeftDown = false;
                    // GUIUtility.hotControl = GUIUtility.GetControlID(FocusType.Passive);
                    GUIUtility.hotControl = 0;  // 왠진 모르겟지만 0도 되서 그냥 0으로.
                    currentEvent.Use();
                }
                break;
        }
    }


    void SaveUndo(string undoName)      // 현재 MeshFilter와 MeshCreator의 상태를 스택에 쌓아서 Undo를 구현합니다.
    {
        Undo.RecordObject(myMeshCreator.gameObject.GetComponent<MeshFilter>(), "");
        Undo.RecordObject(myMeshCreator, undoName);
    }
    void SaveUndo()
    {
        string undoName = "Modify " + target.name;
        Undo.RecordObject(myMeshCreator.gameObject.GetComponent<MeshFilter>(), "");
        Undo.RecordObject(myMeshCreator, undoName);
    }


    void Gizmo()
    {
        Handles.color = Color.green;
        int[] triangles = myMeshCreator.GetMesh().triangles;
        Vector3[] vertices = myMeshCreator.GetMesh().vertices;
        int[] outLineVertices = myMeshCreator.GetOutLineVertice().ToArray();

        // tri 배열을 순환하면서 얻은 데이터로 vertices를 순환.
        for (int i = 0; i < myMeshCreator.GetMesh().triangles.Length; ++i)
        {
            if (i % 3 == 0)
            {
                Handles.DrawLine(
                    vertices[triangles[i]] + myMeshCreator.transform.position,
                    vertices[triangles[i + 2]] + myMeshCreator.transform.position
                    );
                continue;
            }
            Handles.DrawLine(
                myMeshCreator.GetMesh().vertices[myMeshCreator.GetMesh().triangles[i - 1]] + myMeshCreator.transform.position,
                myMeshCreator.GetMesh().vertices[myMeshCreator.GetMesh().triangles[i]] + myMeshCreator.transform.position
                );
        }

        Handles.color = Color.red;
        // OutLine을 그림.
        for (int i = 1; i < outLineVertices.Length; ++i)
        {
            for (int j = 0; j < 2; ++j)
                Handles.DrawLine(
                    vertices[outLineVertices[i - 1]] + myMeshCreator.transform.position,
                    vertices[outLineVertices[i]] + myMeshCreator.transform.position);
        }

    }
    void ControllHandle()
    {
        Handles.color = Color.green;
        int[] triangles = myMeshCreator.GetMesh().triangles;
        Vector3[] vertices = myMeshCreator.GetMesh().vertices;

        Vector3 minVertPos = Vector3.zero;
        // Focus Index 계산
        {
            minVert = myMeshCreator.FindMinDistanceVertex();
            minMidVertPos = myMeshCreator.FindMinDistanceMidVertex();

            if (Vector3.Distance(vertices[minVert] + myMeshCreator.transform.position, MouseInfo.Position) <
                    Vector3.Distance(minMidVertPos + myMeshCreator.transform.position, MouseInfo.Position))
                minMidVertPos = Vector3.zero;
        }


        // 기즈모
        {
            // Vertex 점 찍기
            for (int i = 0; i < vertices.Length; ++i)
            {
                Handles.color = Color.green;
                Handles.CubeHandleCap(0, vertices[i] + myMeshCreator.transform.position, Quaternion.identity, Camera.current.orthographicSize * 0.02f, EventType.Repaint);
            }

            Handles.color = Color.red;
            if (minMidVertPos == Vector3.zero)
                minVertPos = vertices[minVert];
            else
                minVertPos = minMidVertPos;

            // 가장 가까운 Vertex에 점 찍기
            if (HandleUtility.DistanceToRectangle(
            minVertPos + myMeshCreator.transform.position,
            Quaternion.identity, Camera.current.orthographicSize * 0.02f * 0.5f)
                < 50)
                Handles.CubeHandleCap(0, minVertPos + myMeshCreator.transform.position, Quaternion.identity, Camera.current.orthographicSize * 0.02f, EventType.Repaint);
        }

    }
    void RemoveHandle()
    {
        myMeshCreator.CheckRemoveAbleAll();
        Handles.color = Color.green;
        int[] triangles = myMeshCreator.GetMesh().triangles;
        Vector3[] vertices = myMeshCreator.GetMesh().vertices;
        bool[] removeAble = myMeshCreator.GetRemoveAble().ToArray();

        Vector3 minVertPos = Vector3.zero;
        // Focus Index 계산
        {
            minVert = myMeshCreator.FindMinDistanceVertex();
            minMidVertPos = myMeshCreator.FindMinDistanceMidVertex();

            if (Vector3.Distance(vertices[minVert] + myMeshCreator.transform.position, MouseInfo.Position) <
                  Vector3.Distance(minMidVertPos + myMeshCreator.transform.position, MouseInfo.Position))
                minMidVertPos = Vector3.zero;
        }


        // 기즈모
        {
            // Vertex 점 찍기
            for (int i = 0; i < vertices.Length; ++i)
            {
                if (removeAble[i] == true)
                    Handles.color = Color.green;
                else
                    Handles.color = Color.gray;
                Handles.CubeHandleCap(0, vertices[i] + myMeshCreator.transform.position, Quaternion.identity, Camera.current.orthographicSize * 0.02f, EventType.Repaint);
            }


            if (removeAble[minVert] == true)
                Handles.color = Color.red;
            else
                Handles.color = Color.black;

            // 가장 가까운 Vertex에 점 찍기
            if (HandleUtility.DistanceToRectangle(
                    vertices[minVert] + myMeshCreator.transform.position,
                    Quaternion.identity, Camera.current.orthographicSize * 0.02f * 0.5f)
                < 50)
                Handles.CubeHandleCap(0, vertices[minVert] + myMeshCreator.transform.position, Quaternion.identity, Camera.current.orthographicSize * 0.02f, EventType.Repaint);
        }
    }


    // MeshCreator 로 옮기기.
    bool AbleEditting(int index)         // 거리를 계산해서 수정 가능한 버텍스인지 확인합니다.
    {
        if (HandleUtility.DistanceToRectangle(
           myMeshCreator.GetMesh().vertices[index] + myMeshCreator.transform.position,
           Quaternion.identity, Camera.current.orthographicSize * 0.02f * 0.5f)
               < 50)
            return true;
        else
            return false;
    }
    bool AbleEditting(int index1, int index2)    //  삭제 예정. 두 버텍스의 중앙값 -> 중앙값을 구한뒤 두 버텍스 구하기로 변경했음 
    {
        if (HandleUtility.DistanceToRectangle(
           (myMeshCreator.GetMesh().vertices[index1] + myMeshCreator.GetMesh().vertices[index2]) * 0.5f + myMeshCreator.transform.position,
           Quaternion.identity, Camera.current.orthographicSize * 0.02f * 0.5f)
               < 50)
            return true;
        else
            return false;
    }
    bool AbleEditting(Vector3 vertexPosition)
    {
        if (HandleUtility.DistanceToRectangle(
           vertexPosition + myMeshCreator.transform.position,
           Quaternion.identity, Camera.current.orthographicSize * 0.02f * 0.5f)
               < 50)
            return true;
        else
            return false;
    }
}
