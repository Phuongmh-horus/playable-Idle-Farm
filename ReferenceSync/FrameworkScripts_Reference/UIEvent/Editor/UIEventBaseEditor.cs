using UnityEditor;
using UnityEngine;
using System;
using System.Linq;

[CustomEditor(typeof(UIEventBase), true)]
public class UIEventBaseEditor : Editor
{
    private string searchFilter = "";
    private bool showLayerHideSearch = false;
    private Vector2 scrollPosition;

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        SerializedProperty layerHideProperty = serializedObject.FindProperty("LayerHide");
        SerializedProperty stateProperty = serializedObject.FindProperty("State");
        SerializedProperty priorityProperty = serializedObject.FindProperty("Priority");

        // Draw State and Priority with default property fields
        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(stateProperty);
        EditorGUILayout.PropertyField(priorityProperty);
        if (EditorGUI.EndChangeCheck())
        {
            serializedObject.ApplyModifiedProperties();
        }
        DrawPropertiesExcluding(serializedObject, "m_Script", "LayerHide", "State", "Priority", "EndEvent");

        // Custom LayerHide field with search
        EditorGUILayout.Space(5);
        DrawLayerHideSearchField(layerHideProperty);

        // Draw EndEvent (readonly display)
        EditorGUILayout.Space(5);
        //GUI.enabled = false;
        //EditorGUILayout.PropertyField(serializedObject.FindProperty("EndEvent"));
        GUI.enabled = true;

        // Draw remaining properties from derived classes

        // Apply all changes
        serializedObject.ApplyModifiedProperties();
    }

    private void DrawLayerHideSearchField(SerializedProperty layerHideProperty)
    {
        EditorGUILayout.BeginVertical("box");

        // Header with current value
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Layer Hide", EditorStyles.boldLabel, GUILayout.Width(EditorGUIUtility.labelWidth - 4));

        string currentValue = layerHideProperty.enumDisplayNames[layerHideProperty.enumValueIndex];
        if (GUILayout.Button(currentValue, EditorStyles.popup))
        {
            showLayerHideSearch = !showLayerHideSearch;
            if (showLayerHideSearch)
            {
                searchFilter = "";
            }
        }
        EditorGUILayout.EndHorizontal();

        // Show search and selection list when open
        if (showLayerHideSearch)
        {
            EditorGUILayout.Space(5);

            // Search field
            GUI.SetNextControlName("SearchField");
            string newSearchFilter = EditorGUILayout.TextField("Search:", searchFilter);
            if (newSearchFilter != searchFilter)
            {
                searchFilter = newSearchFilter;
            }

            // Auto-focus search field when opened
            if (Event.current.type == EventType.Repaint)
            {
                GUI.FocusControl("SearchField");
            }

            EditorGUILayout.Space(3);

            // Get all enum values
            string[] enumNames = layerHideProperty.enumDisplayNames;
            var filteredIndices = enumNames
                .Select((name, index) => new { name, index })
                .Where(x => string.IsNullOrEmpty(searchFilter) ||
                           x.name.IndexOf(searchFilter, StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();

            // Show filtered count
            EditorGUILayout.LabelField($"Found: {filteredIndices.Count} / {enumNames.Length}", EditorStyles.miniLabel);

            // Scrollable list
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.MaxHeight(200));

            foreach (var item in filteredIndices)
            {
                bool isSelected = layerHideProperty.enumValueIndex == item.index;

                GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
                if (isSelected)
                {
                    buttonStyle.normal.textColor = Color.cyan;
                    buttonStyle.fontStyle = FontStyle.Bold;
                }

                if (GUILayout.Button(item.name, buttonStyle))
                {
                    layerHideProperty.enumValueIndex = item.index;
                    serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(target);
                    showLayerHideSearch = false;
                    searchFilter = "";
                    GUI.FocusControl(null);
                }
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(3);

            // Close button
            if (GUILayout.Button("Close"))
            {
                showLayerHideSearch = false;
                searchFilter = "";
                GUI.FocusControl(null);
            }
        }

        EditorGUILayout.EndVertical();
    }
}
