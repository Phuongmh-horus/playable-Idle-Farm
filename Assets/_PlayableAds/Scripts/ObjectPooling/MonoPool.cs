// using Cysharp.Threading.Tasks;
// using System;
// using System.Collections.Generic;
// using UnityEngine;
// using Object = UnityEngine.Object;

// public class MonoPool<T> where T : Component
// {
//     private Stack<T> _holder = new();
//     private Stack<T> _temp = new();

//     private List<T> _currentData = new();
//     private T _mainPF;
//     private Transform _container;
//     public List<T> Data => _currentData;
//     public Stack<T> Holder => _holder;

//     public MonoPool(T pf, Transform parent)
//     {
//         _mainPF = pf;
//         _container = parent;
//     }

//     public void AddRangeData(List<T> data)
//     {
//         foreach (var component in data)
//         {
//             _holder.Push(component);
//         }
//         _currentData.AddRange(data);
//     }

//     /// <summary>
//     /// Khởi tạo trước 1 lượng pf
//     /// </summary>
//     /// <param name="num"></param>
//     public void InitPool(int num = 5)
//     {
//         if (_holder.Count < num)
//         {
//             for (int i = 0; i < num; i++)
//             {
//                 T t = Object.Instantiate(_mainPF, _container);
//                 t.gameObject.SetActive(false);
//                 _holder.Push(t);
//             }
//         }
//     }

//     // Cảnh báo!!! Nên chắc chắn sẽ init success trước khi xử lý dữ liệu
//     public async UniTask InitPoolAsync(int num = 5) // Khuyến nghị sử dụng
//     {
//         if (_holder.Count < num)
//         {
//             for (int i = 0; i < num; i++)
//             {
//                 T t = Object.Instantiate(_mainPF, _container);
//                 t.gameObject.SetActive(false);
//                 _holder.Push(t);
//                 await UniTask.NextFrame();
//             }
//         }
//     }

//     /// <summary>
//     /// Mỗi khi reset tất cả dữ liệu hiển thị
//     /// </summary>
//     public void BeginSetup()
//     {
//         _temp.Clear();
//         _currentData.Clear();
//         foreach (var item in _holder)
//         {
//             _temp.Push(item);
//         }
//     }

//     /// <summary>
//     /// Lấy ra 1 item để setup
//     /// </summary>
//     /// <returns></returns>
//     public T Get(bool isActive = true, Transform parent = null)
//     {
//         T result;
//         var itemParent = _container;
//         if (parent) itemParent = parent;
//         if (_temp.Count > 0)
//         {
//             result = _temp.Pop();
//             //Debug.Log(result);
//             result.transform.SetParent(itemParent, true);
//         }
//         else
//         {
//             if (_mainPF == null)
//             {
//                 Debug.LogError("Không tìm thấy _mainPF code kiểm tra");
//                 return null;
//             }
//             result = Object.Instantiate(_mainPF, itemParent);
//             _holder.Push(result);
//         }
//         _currentData.Add(result);
//         result.gameObject.SetActive(isActive);
//         return result;
//     }

//     public T GetFromHolder(bool isActive = true, Transform parent = null)
//     {
//         T result;
//         var itemParent = _container;
//         if (parent) itemParent = parent;
//         if (_holder.Count > 0)
//         {
//             result = _holder.Pop();
//             //Debug.Log(result);
//             result.transform.SetParent(itemParent, true);
//         }
//         else
//         {
//             if (_mainPF == null)
//             {
//                 Debug.LogError("Không tìm thấy _mainPF code kiểm tra");
//                 return null;
//             }
//             result = Object.Instantiate(_mainPF, itemParent);
//         }
//         _currentData.Add(result);
//         result.gameObject.SetActive(isActive);
//         return result;
//     }

//     public async UniTask<T> GetAsync(bool isActive = true, Transform parent = null) // Khuyến nghị sử dụng
//     {
//         T result;
//         var itemParent = _container;
//         if (parent) itemParent = parent;
//         if (_temp.Count > 0)
//         {
//             result = _temp.Pop();
//         }
//         else
//         {
//             result = Object.Instantiate(_mainPF, itemParent);
//             _holder.Push(result);
//             await UniTask.NextFrame();
//         }
//         result.gameObject.SetActive(isActive);
//         _currentData.Add(result);
//         return result;
//     }

//     /// <summary>
//     /// Release các item không dùng
//     /// </summary>
//     public void EndSetup()
//     {
//         foreach (var item in _temp)
//         {
//             item.gameObject.SetActive(false);
//         }
//     }

//     /// <summary>
//     /// Disable all item
//     /// </summary>
//     public void ReleasePool()
//     {
//         foreach (var item in _holder)
//         {
//             item.gameObject.SetActive(false);
//         }
//     }

//     /// <summary>
//     /// Run action
//     /// </summary>
//     /// <param name="action"></param>
//     public void Invoke(Action<T> action)
//     {
//         foreach (var data in _currentData)
//         {
//             action?.Invoke(data);
//         }
//     }

//     public void ReleaseItem(T elementToRemove)
//     {
//         // Chuyển stack thành danh sách
//         List<T> list = new List<T>(_holder);

//         // Xóa phần tử trong T sách
//         list.Remove(elementToRemove);

//         // Tạo lại stack từ danh sách
//         _holder = new Stack<T>(list);

//         _currentData.Remove(elementToRemove);
//     }
//     public void RePoolItem(T elementToRemove)
//     {
//         elementToRemove.gameObject.SetActive(false);
//         _holder.Push(elementToRemove);
//     }
//     public void Dispose()
//     {
//         _holder.Clear();
//         _temp.Clear();
//         _currentData.Clear();
//     }
// }
