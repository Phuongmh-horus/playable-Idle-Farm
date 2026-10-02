using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ApplovinMaxAdsConfigSO))]
public class ApplovinMaxAdsConfigSOEditor : Editor
{
    private SerializedProperty _unitIds;

    private void OnEnable()
    {
        _unitIds = serializedObject.FindProperty("unitIds");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawDefaultInspector();

        if (HasInvalidUnitIdTypes())
        {
            EditorGUILayout.HelpBox(
                "Unit Ids chỉ được phép sử dụng type Banner hoặc AppOpen.\nInterstitial và Rewarded phải được cấu hình trong Waterfall Unit Ids.",
                MessageType.Error);
        }

        serializedObject.ApplyModifiedProperties();
    }

    private bool HasInvalidUnitIdTypes()
    {
        if (_unitIds == null)
            return false;

        for (int i = 0; i < _unitIds.arraySize; i++)
        {
            var element = _unitIds.GetArrayElementAtIndex(i);
            var typeProp = element.FindPropertyRelative("type");
            var adsType = (AdsType)typeProp.enumValueIndex;

            if (adsType != AdsType.Banner && adsType != AdsType.AppOpen && adsType != AdsType.OpenApp)
            {
                return true;
            }
        }

        return false;
    }
}
