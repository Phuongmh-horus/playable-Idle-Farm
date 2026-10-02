using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Utilities
{
#if UNITY_EDITOR
    [Serializable]
    public class ImageReferenceData
    {
        public Image image;
        public Sprite newSprite;
        public string path;
        public bool newRaycastTarget = true;
        public bool newMaskable = true;
        public bool isSelected;
    }
#endif

    /// <summary>
    /// Tool chỉnh hàng loạt Image trong một prefab (sprite, raycastTarget, maskable).
    /// Chỉ dùng trong Editor, xem <see cref="Game.Editor.ImageReferenceManagerEditor"/>.
    ///
    /// TOÀN BỘ dữ liệu và hàm nằm trong #if UNITY_EDITOR và phải giữ nguyên như vậy.
    /// Lý do: đây là component runtime nhưng lưu một entry cho MỖI Image trong prefab, kèm
    /// đường dẫn dạng chuỗi và một tham chiếu Sprite. Trên PopupDailyGift.prefab là 46 entry,
    /// tức 46 object heap + 46 chuỗi được cấp phát MỖI LẦN instantiate prefab, cộng với chừng
    /// đó dữ liệu thừa trong bản build - toàn bộ chỉ phục vụ thao tác chỉnh sửa trong Editor.
    ///
    /// Class rỗng vẫn được biên dịch vào build (không bọc cả class) để prefab đã gắn component
    /// này không bị "The referenced script is missing". Trong player build nó không còn field
    /// nào nên Unity không ghi dữ liệu đó vào bản build, và component chỉ còn là vỏ rỗng.
    ///
    /// Muốn dọn sạch hẳn thì Remove Component khỏi prefab - không có gì ở runtime dùng nó.
    /// </summary>
    public class ImageReferenceManager : MonoBehaviour
    {
#if UNITY_EDITOR
        [HideInInspector]
        public List<ImageReferenceData> imageReferences = new List<ImageReferenceData>();

        public void RefreshReferences()
        {
            imageReferences.Clear();
            Image[] images = GetComponentsInChildren<Image>(true);

            foreach (Image img in images)
            {
                imageReferences.Add(new ImageReferenceData
                {
                    image = img,
                    newSprite = img.sprite,
                    path = GetGameObjectPath(img.transform),
                    newRaycastTarget = img.raycastTarget,
                    newMaskable = img.maskable
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

        public void ApplyAllSprites()
        {
            foreach (var data in imageReferences)
            {
                if (data.image != null && data.newSprite != null)
                {
                    data.image.sprite = data.newSprite;
                }
            }
        }

        public void ApplyAllProperties()
        {
            foreach (var data in imageReferences)
            {
                if (data.image != null)
                {
                    if (data.newSprite != null)
                    {
                        data.image.sprite = data.newSprite;
                    }
                    data.image.raycastTarget = data.newRaycastTarget;
                    data.image.maskable = data.newMaskable;
                }
            }
        }

        public void SetAllRaycastTarget(bool value)
        {
            foreach (var data in imageReferences)
            {
                data.newRaycastTarget = value;
            }
        }

        public void SetAllMaskable(bool value)
        {
            foreach (var data in imageReferences)
            {
                data.newMaskable = value;
            }
        }

        public void SelectAll(bool value)
        {
            foreach (var data in imageReferences)
            {
                data.isSelected = value;
            }
        }

        public void ApplySelectedProperties()
        {
            foreach (var data in imageReferences)
            {
                if (data.isSelected && data.image != null)
                {
                    if (data.newSprite != null)
                    {
                        data.image.sprite = data.newSprite;
                    }
                    data.image.raycastTarget = data.newRaycastTarget;
                    data.image.maskable = data.newMaskable;
                }
            }
        }

        public void SetSelectedRaycastTarget(bool value)
        {
            foreach (var data in imageReferences)
            {
                if (data.isSelected)
                {
                    data.newRaycastTarget = value;
                }
            }
        }

        public void SetSelectedMaskable(bool value)
        {
            foreach (var data in imageReferences)
            {
                if (data.isSelected)
                {
                    data.newMaskable = value;
                }
            }
        }

        public void SetSelectedSprite(Sprite sprite)
        {
            foreach (var data in imageReferences)
            {
                if (data.isSelected)
                {
                    data.newSprite = sprite;
                }
            }
        }

        public int GetSelectedCount()
        {
            int count = 0;
            foreach (var data in imageReferences)
            {
                if (data.isSelected) count++;
            }
            return count;
        }

        public void SetSelectedBoth(bool raycast, bool maskable)
        {
            foreach (var data in imageReferences)
            {
                if (data.isSelected)
                {
                    data.newRaycastTarget = raycast;
                    data.newMaskable = maskable;
                }
            }
        }

        public void SetAllBoth(bool raycast, bool maskable)
        {
            foreach (var data in imageReferences)
            {
                data.newRaycastTarget = raycast;
                data.newMaskable = maskable;
            }
        }
#endif
    }
}
