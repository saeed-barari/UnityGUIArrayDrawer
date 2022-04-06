using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.DemiEditor;
using UnityEditor;
using UnityEngine;

static public class GUIArrayDrawer
{
    class Data
    {
        public bool isExpanded = false;
        public bool foldOut = true;
        public bool[] isChildExpanded;
        public Rect[] childRects;
        public bool movingItemSelected => movingItemIndex != -1;
        public bool isMoveAnimation = false;
        public int movingItemIndex = -1;
        public int movingTargetIndex;
        public int selectedIndex = 0;
    }
    static public float animationSpeed = 0.05f;
    static public Color SELECTED_BLUE = new Color(0, 0, 1, 0.4f);
    static Dictionary<string, Data> dic = new Dictionary<string, Data>();

    /// <summary>
    /// draws a reorderable list. it changes the list! be sure to make backup!
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="id">unique ID</param>
    /// <param name="onDraw">main element layout</param>
    /// <param name="displayName">title for each element</param>
    /// <param name="foldOut">whether or not display title and foldout option</param>
    /// <returns>selected index</returns>
    static public int DrawList<T>(string id, string label, List<T> list, Action<T> onDraw,
        Func<int, string> displayName = null, bool foldOut = true) where T : new()
    {
        if (!dic.ContainsKey(id))
            dic.Add(id, new Data()
            {
                isChildExpanded = new bool[list.Count],
                childRects = new Rect[list.Count],
                foldOut = foldOut
            });

        var data = dic[id];
        if (!data.foldOut)
            for (var index = 0; index < data.isChildExpanded.Length; index++)
                data.isChildExpanded[index] = true;

        TaskEditorHelper.DrawInBoxVertical(TaskEditor.BACKGROUND_COL, "", DrawElements, () =>
        {
            data.isExpanded = EditorGUILayout.Foldout(data.isExpanded, new GUIContent(label), true);
            if (GUILayout.Button("+", GUILayout.Width(45), GUILayout.ExpandHeight(true)))
            {
                list.Add(new T());
            }
        });

        return data.selectedIndex;

        void DrawElements()
        {
            if (!data.isExpanded)
                return;

            var oldGUI = GUI.enabled;
            if(data.isMoveAnimation)
                GUI.enabled = false;
            
            for (int i = 0; i < list.Count; i++)
            {
                if (list.Count - 1 < i || i < 0)
                    continue;

                DrawSelectionButton(i);
                var rect = EditorGUILayout.BeginVertical();
                {

                    GUILayout.BeginHorizontal();
                    {
                        if (data.isChildExpanded.Length != list.Count)
                            data.isChildExpanded = new bool[list.Count];

                        if (data.foldOut)
                            data.isChildExpanded[i] = EditorGUILayout.Foldout(
                                data.isChildExpanded[i],
                                displayName == null ? $"Element {i + 1}" : displayName(i), true);
                    }
                    GUILayout.EndHorizontal();

                    if (data.isChildExpanded[i])
                    {
                        // if in animation, we'll want control over the layouts to apply smooth animation
                        var col = GUI.color;
                        if (data.movingItemIndex == i)
                            GUI.color = Color.cyan;

                        EditorGUILayout.BeginHorizontal();
                        {
                            DrawReorders(i);

                            EditorGUILayout.BeginVertical();
                            EditorGUI.indentLevel++;
                            onDraw(list[i]);
                            EditorGUI.indentLevel--;
                            EditorGUILayout.EndVertical();

                            DrawDelete(i);
                        }
                        EditorGUILayout.EndHorizontal();

                        GUILayout.Space(2);
                        TaskEditorHelper.GuiLine(2, TaskEditor.BACKGROUND_COL);
                        GUILayout.Space(2);
                        GUI.color = col;
                    }
                }
                EditorGUILayout.EndVertical();

                if(rect != Rect.zero)
                    data.childRects[i] = rect;
            }

            GUI.enabled = oldGUI;

            void DrawSelectionButton(int index)
            {
                if(data.childRects == null || data.childRects.Length -1 < index) return;

                var col = index == data.selectedIndex ? SELECTED_BLUE : new Color(0, 0, 0, 0);
                EditorGUI.DrawRect(data.childRects[index], col);
                if(Event.current.type == EventType.MouseDown)
                {
                    Debug.Log($"rect {data.childRects[index]} mouse at {Event.current.mousePosition}");
                    if(data.childRects[index].Contains(Event.current.mousePosition))
                    {
                        Debug.Log($"clicked!!");
                        data.selectedIndex = index;
                        EditorWindow.focusedWindow.Repaint();
                    }
                }
                
                if(data.childRects[index] == Rect.zero)
                    EditorWindow.focusedWindow.Repaint();

            }

            void DrawReorders(int index)
            {
                var old = GUI.enabled;
                if (GUILayout.Button(data.movingItemSelected ? "▷" : "◁", GUILayout.Width(28)))
                {
                    if (data.movingItemSelected)
                    {
                        // moveTo
                        data.movingTargetIndex = index;
                        DeEditorCoroutines.StartCoroutine(MoveItem(data, list));

                    }
                    else
                    {
                        data.movingItemIndex = index;
                    }
                }

                GUI.enabled = old;
            }

            bool DrawDelete(int index)
            {
                if (GUILayout.Button("X", GUILayout.Width(20)))
                {
                    list.RemoveAt(index);
                    return true;
                }
                return false;
            }
            
            IEnumerator MoveItem(Data data, List<T> list)
            {
                data.isMoveAnimation = true;
                var Inc = data.movingTargetIndex > data.movingItemIndex ? 1 : -1;

                // actually applying array replacement
                for (int i = data.movingItemIndex; i != data.movingTargetIndex; i += Inc)
                {
                    var tmp = list[i];
                    list[i] = list[i + Inc];
                    list[i + Inc] = tmp;
                    data.movingItemIndex += Inc;
                    yield return DeEditorCoroutines.WaitForSeconds(animationSpeed);
                    EditorWindow.focusedWindow.Repaint();
                }

                data.movingItemIndex = -1;
                data.isMoveAnimation = false;
            }
        }
    }
}