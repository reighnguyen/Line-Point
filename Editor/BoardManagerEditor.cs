using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(BoardManager), false)]
public sealed class BoardManagerEditor : Editor
{
    BoardManager boardMgr;
    private void OnEnable()
    {
        boardMgr = target as BoardManager;
    }

    public override void OnInspectorGUI()
    {
        if(GUILayout.Button("Generate Board"))
        {
            boardMgr.GenerateBoard();
        }
        if (GUILayout.Button("Clear Board"))
        {
            boardMgr.ClearBoard();
        }
        base.OnInspectorGUI();
    }

}
