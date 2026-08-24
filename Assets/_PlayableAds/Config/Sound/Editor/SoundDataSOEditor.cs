using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SoundDataSO))]
public class SoundDataSOEditor : Editor
{
    private SoundDataSO _soundDataSO;
    private string _searchFilter = "";
    private Vector2 _scrollPosition;
    private List<SoundData> _filteredList;
    private bool _isDirty = false;

    // UI Colors
    private static readonly Color HeaderColor = new Color(0.3f, 0.5f, 0.7f, 1f);
    private static readonly Color AddButtonColor = new Color(0.4f, 0.8f, 0.4f, 1f);
    private static readonly Color DeleteButtonColor = new Color(0.9f, 0.3f, 0.3f, 1f);

    private void OnEnable()
    {
        _soundDataSO = (SoundDataSO)target;
        RefreshFilteredList();
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawHeader();
        DrawSearchBar();
        DrawAddButton();

        EditorGUILayout.Space(10);

        DrawSoundDataList();

        if (_isDirty)
        {
            EditorUtility.SetDirty(_soundDataSO);
            serializedObject.ApplyModifiedProperties();
            _isDirty = false;
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawHeader()
    {
        EditorGUILayout.Space(5);

        // Header with background
        var headerRect = EditorGUILayout.GetControlRect(false, 30);
        EditorGUI.DrawRect(headerRect, HeaderColor);

        var labelStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 14,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.white }
        };

        EditorGUI.LabelField(headerRect, $"Sound Data Manager ({_soundDataSO.soundDataList.Count} items)", labelStyle);

        EditorGUILayout.Space(5);
    }

    private void DrawSearchBar()
    {
        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.LabelField("Search:", GUILayout.Width(60));

        EditorGUI.BeginChangeCheck();
        _searchFilter = EditorGUILayout.TextField(_searchFilter, EditorStyles.toolbarSearchField);

        if (EditorGUI.EndChangeCheck())
        {
            RefreshFilteredList();
        }

        // Clear button
        if (GUILayout.Button("Clear", GUILayout.Width(50)))
        {
            _searchFilter = "";
            RefreshFilteredList();
            GUI.FocusControl(null);
        }

        EditorGUILayout.EndHorizontal();

        // Show result count
        if (!string.IsNullOrEmpty(_searchFilter))
        {
            EditorGUILayout.LabelField($"Found: {_filteredList.Count} / {_soundDataSO.soundDataList.Count}", EditorStyles.miniLabel);
        }
    }

    private void DrawAddButton()
    {
        EditorGUILayout.Space(5);

        var originalColor = GUI.backgroundColor;
        GUI.backgroundColor = AddButtonColor;

        if (GUILayout.Button("+ Add New Sound Data", GUILayout.Height(30)))
        {
            AddNewSoundData();
        }

        GUI.backgroundColor = originalColor;

        EditorGUILayout.Space(5);
    }

    private void DrawSoundDataList()
    {
        if (_filteredList == null || _filteredList.Count == 0)
        {
            EditorGUILayout.HelpBox("No sound data found. Click 'Add New Sound Data' to create one.", MessageType.Info);
            return;
        }

        _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(Mathf.Min(500, _filteredList.Count * 90)));

        for (int i = 0; i < _filteredList.Count; i++)
        {
            DrawSoundDataItem(_filteredList[i], i);
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawSoundDataItem(SoundData soundData, int index)
    {
        // Background box
        var boxRect = EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        // Highlight search matches
        if (!string.IsNullOrEmpty(_searchFilter) &&
            soundData.Name.ToString().ToLower().Contains(_searchFilter.ToLower()))
        {
            EditorGUI.DrawRect(boxRect, new Color(1f, 1f, 0.5f, 0.1f));
        }

        EditorGUILayout.BeginHorizontal();

        // Sound Data fields
        EditorGUILayout.BeginVertical();

        EditorGUI.BeginChangeCheck();

        soundData.Name = (AudioClipName)EditorGUILayout.EnumPopup("Audio Clip Name", soundData.Name);
        soundData.Clip = (AudioClip)EditorGUILayout.ObjectField("Audio Clip", soundData.Clip, typeof(AudioClip), false);
        soundData.VolumeDefault = EditorGUILayout.Slider("Default Volume", soundData.VolumeDefault, 0f, 1f);

        if (EditorGUI.EndChangeCheck())
        {
            _isDirty = true;
        }

        EditorGUILayout.EndVertical();

        // Action buttons
        EditorGUILayout.BeginVertical(GUILayout.Width(80));

        // Play button (if clip exists)
        if (soundData.Clip != null)
        {
            if (GUILayout.Button("▶ Play", GUILayout.Height(25)))
            {
                PlayAudioClip(soundData.Clip, soundData.VolumeDefault);
            }
        }

        // Duplicate button
        if (GUILayout.Button("Copy", GUILayout.Height(20)))
        {
            DuplicateSoundData(soundData);
        }

        // Delete button
        var originalColor = GUI.backgroundColor;
        GUI.backgroundColor = DeleteButtonColor;

        if (GUILayout.Button("Delete", GUILayout.Height(20)))
        {
            if (EditorUtility.DisplayDialog("Delete Sound Data",
                $"Are you sure you want to delete '{soundData.Name}'?", "Delete", "Cancel"))
            {
                DeleteSoundData(soundData);
            }
        }

        GUI.backgroundColor = originalColor;

        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(5);
    }

    private void RefreshFilteredList()
    {
        if (string.IsNullOrEmpty(_searchFilter))
        {
            _filteredList = new List<SoundData>(_soundDataSO.soundDataList);
        }
        else
        {
            _filteredList = _soundDataSO.GetSoundDataByName(_searchFilter);
        }
    }

    private void AddNewSoundData()
    {
        Undo.RecordObject(_soundDataSO, "Add Sound Data");

        var newData = new SoundData
        {
            Name = AudioClipName.None,
            Clip = null,
            VolumeDefault = 1f
        };

        _soundDataSO.soundDataList.Add(newData);
        RefreshFilteredList();
        _isDirty = true;

        // Scroll to bottom
        _scrollPosition.y = float.MaxValue;
    }

    private void DuplicateSoundData(SoundData original)
    {
        Undo.RecordObject(_soundDataSO, "Duplicate Sound Data");

        var duplicate = new SoundData
        {
            Name = original.Name,
            Clip = original.Clip,
            VolumeDefault = original.VolumeDefault
        };

        int index = _soundDataSO.soundDataList.IndexOf(original);
        _soundDataSO.soundDataList.Insert(index + 1, duplicate);

        RefreshFilteredList();
        _isDirty = true;
    }

    private void DeleteSoundData(SoundData soundData)
    {
        Undo.RecordObject(_soundDataSO, "Delete Sound Data");

        _soundDataSO.soundDataList.Remove(soundData);
        RefreshFilteredList();
        _isDirty = true;
    }

    private void PlayAudioClip(AudioClip clip, float volume)
    {
        // Play audio in editor
        var audioSource = EditorUtility.CreateGameObjectWithHideFlags("Audio Preview", HideFlags.HideAndDontSave, typeof(AudioSource));
        var source = audioSource.GetComponent<AudioSource>();
        source.clip = clip;
        source.volume = volume;
        source.Play();

        // Destroy after clip finishes
        EditorApplication.delayCall += () =>
        {
            System.Threading.Tasks.Task.Delay((int)(clip.length * 1000)).ContinueWith(_ =>
            {
                EditorApplication.delayCall += () =>
                {
                    if (audioSource != null)
                        DestroyImmediate(audioSource);
                };
            });
        };
    }
}
