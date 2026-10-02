using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace Game.Utilities
{
#if UNITY_EDITOR
    [Serializable]
    public class TMPTextReferenceData
    {
        public TMP_Text text;
        public string path;
        public bool isSelected;

        // Text content
        public string newTextContent;

        // Font properties
        public TMP_FontAsset newFont;
        public float newFontSize;

        // Auto sizing
        public bool newEnableAutoSizing;
        public float newFontSizeMin;
        public float newFontSizeMax;

        // Text properties
        public bool newEnableWordWrapping;
        public bool newRichText;
        public bool newParseEscapeCharacters;
        public bool newEnableKerning;

        // UI properties
        public bool newRaycastTarget = true;
        public bool newMaskable = true;
    }
#endif

    /// <summary>
    /// Tool chinh hang loat TMP_Text trong mot prefab (font, size, auto-size, raycast...).
    /// Chi dung trong Editor, xem <see cref="Game.Editor.TMPTextReferenceManagerEditor"/>.
    ///
    /// TOAN BO du lieu va ham nam trong #if UNITY_EDITOR va phai giu nguyen nhu vay.
    /// Ly do: day la component runtime nhung luu mot entry cho MOI TMP_Text trong prefab,
    /// kem duong dan dang chuoi, NOI DUNG TEXT day du va mot tham chieu TMP_FontAsset.
    /// Tat ca chi phuc vu thao tac chinh sua trong Editor, khong co gi o runtime doc den.
    ///
    /// Class rong van duoc bien dich vao build (khong boc ca class) de prefab nao lo gan
    /// component nay khong bi "The referenced script is missing". Trong player build no
    /// khong con field nao nen Unity khong ghi du lieu do vao ban build.
    ///
    /// Muon don sach han thi Remove Component khoi prefab.
    /// </summary>
    public class TMPTextReferenceManager : MonoBehaviour
    {
#if UNITY_EDITOR
        [HideInInspector]
        public List<TMPTextReferenceData> textReferences = new List<TMPTextReferenceData>();

        public void RefreshReferences()
        {
            textReferences.Clear();
            TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);

            foreach (TMP_Text txt in texts)
            {
                textReferences.Add(new TMPTextReferenceData
                {
                    text = txt,
                    path = GetGameObjectPath(txt.transform),
                    newTextContent = txt.text,
                    newFont = txt.font,
                    newFontSize = txt.fontSize,
                    newEnableAutoSizing = txt.enableAutoSizing,
                    newFontSizeMin = txt.fontSizeMin,
                    newFontSizeMax = txt.fontSizeMax,
                    newEnableWordWrapping = txt.enableWordWrapping,
                    newRichText = txt.richText,
                    newParseEscapeCharacters = txt.parseCtrlCharacters,
                    newEnableKerning = txt.enableKerning,
                    newRaycastTarget = txt.raycastTarget,
                    newMaskable = txt.maskable
                });
            }
        }

        private string GetGameObjectPath(Transform transform)
        {
            string path = transform.name;
            Transform parent = transform.parent;

            while (parent != null && parent != this.transform)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }

            return path;
        }

        public void ApplyAllProperties()
        {
            foreach (var data in textReferences)
            {
                if (data.text != null)
                {
                    ApplyDataToText(data);
                }
            }
        }

        public void ApplySelectedProperties()
        {
            foreach (var data in textReferences)
            {
                if (data.isSelected && data.text != null)
                {
                    ApplyDataToText(data);
                }
            }
        }

        private void ApplyDataToText(TMPTextReferenceData data)
        {
            // Font properties
            if (data.newFont != null)
            {
                data.text.font = data.newFont;
            }
            data.text.fontSize = data.newFontSize;

            // Auto sizing
            data.text.enableAutoSizing = data.newEnableAutoSizing;
            data.text.fontSizeMin = data.newFontSizeMin;
            data.text.fontSizeMax = data.newFontSizeMax;

            // Text properties
            data.text.enableWordWrapping = data.newEnableWordWrapping;
            data.text.richText = data.newRichText;
            data.text.parseCtrlCharacters = data.newParseEscapeCharacters;
            data.text.enableKerning = data.newEnableKerning;

            // UI properties
            data.text.raycastTarget = data.newRaycastTarget;
            data.text.maskable = data.newMaskable;
        }

        // Selection methods
        public void SelectAll(bool value)
        {
            foreach (var data in textReferences)
            {
                data.isSelected = value;
            }
        }

        public int GetSelectedCount()
        {
            int count = 0;
            foreach (var data in textReferences)
            {
                if (data.isSelected) count++;
            }
            return count;
        }

        // Bulk set methods for ALL
        public void SetAllAutoSizing(bool value)
        {
            foreach (var data in textReferences)
            {
                data.newEnableAutoSizing = value;
            }
        }

        public void SetAllWordWrapping(bool value)
        {
            foreach (var data in textReferences)
            {
                data.newEnableWordWrapping = value;
            }
        }

        public void SetAllRichText(bool value)
        {
            foreach (var data in textReferences)
            {
                data.newRichText = value;
            }
        }

        public void SetAllParseEscapeCharacters(bool value)
        {
            foreach (var data in textReferences)
            {
                data.newParseEscapeCharacters = value;
            }
        }

        public void SetAllKerning(bool value)
        {
            foreach (var data in textReferences)
            {
                data.newEnableKerning = value;
            }
        }

        public void SetAllRaycastTarget(bool value)
        {
            foreach (var data in textReferences)
            {
                data.newRaycastTarget = value;
            }
        }

        public void SetAllMaskable(bool value)
        {
            foreach (var data in textReferences)
            {
                data.newMaskable = value;
            }
        }

        public void SetAllUIProperties(bool raycast, bool maskable)
        {
            foreach (var data in textReferences)
            {
                data.newRaycastTarget = raycast;
                data.newMaskable = maskable;
            }
        }

        // Bulk set methods for SELECTED
        public void SetSelectedAutoSizing(bool value)
        {
            foreach (var data in textReferences)
            {
                if (data.isSelected)
                {
                    data.newEnableAutoSizing = value;
                }
            }
        }

        public void SetSelectedWordWrapping(bool value)
        {
            foreach (var data in textReferences)
            {
                if (data.isSelected)
                {
                    data.newEnableWordWrapping = value;
                }
            }
        }

        public void SetSelectedRichText(bool value)
        {
            foreach (var data in textReferences)
            {
                if (data.isSelected)
                {
                    data.newRichText = value;
                }
            }
        }

        public void SetSelectedParseEscapeCharacters(bool value)
        {
            foreach (var data in textReferences)
            {
                if (data.isSelected)
                {
                    data.newParseEscapeCharacters = value;
                }
            }
        }

        public void SetSelectedKerning(bool value)
        {
            foreach (var data in textReferences)
            {
                if (data.isSelected)
                {
                    data.newEnableKerning = value;
                }
            }
        }

        public void SetSelectedRaycastTarget(bool value)
        {
            foreach (var data in textReferences)
            {
                if (data.isSelected)
                {
                    data.newRaycastTarget = value;
                }
            }
        }

        public void SetSelectedMaskable(bool value)
        {
            foreach (var data in textReferences)
            {
                if (data.isSelected)
                {
                    data.newMaskable = value;
                }
            }
        }

        public void SetSelectedUIProperties(bool raycast, bool maskable)
        {
            foreach (var data in textReferences)
            {
                if (data.isSelected)
                {
                    data.newRaycastTarget = raycast;
                    data.newMaskable = maskable;
                }
            }
        }

        public void SetSelectedFont(TMP_FontAsset font)
        {
            foreach (var data in textReferences)
            {
                if (data.isSelected)
                {
                    data.newFont = font;
                }
            }
        }
#endif
    }
}
