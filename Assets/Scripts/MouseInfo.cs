using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[InitializeOnLoad]
public class MouseInfo : Editor {

    static Vector3 mousePos;

    static MouseInfo()
    {
        SceneView.onSceneGUIDelegate += UpdateMouseInfo;
    }

    public static Vector3 Position
    {
        get { return mousePos; }
    }


    static void UpdateMouseInfo(SceneView sceneView)
    {
        Event currentEvent = Event.current;

        if(currentEvent.isMouse)
        {
            // 마우스 위치 업데이트
            mousePos = new Vector3(Event.current.mousePosition.x, Event.current.mousePosition.y);
            mousePos.y = SceneView.currentDrawingSceneView.camera.pixelHeight - mousePos.y;
            mousePos = SceneView.currentDrawingSceneView.camera.ScreenToWorldPoint(mousePos);
        }
    }

}
