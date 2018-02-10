using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Yosh.Editor
{
	public abstract class EditorWindowBase : EditorWindow
	{
		private static List<IEnumerator> _iEnumerator = new List<IEnumerator>();

		protected virtual void OnEnable()
		{
			EditorApplication.update += OnUpdate;
		}

		protected virtual void OnDisable()
		{
			EditorApplication.update -= OnUpdate;
			_iEnumerator.Clear();
			_iEnumerator = null;
		}

		private static void OnUpdate()
		{
			//管理しているIEnumeratorを進める
			foreach (var item in _iEnumerator.ToArray()) {
				item.MoveNext();
			}
		}


		/// <summary>
		/// 通知用のトーストを表示する
		/// </summary>
		/// <param name="message"> 表示文字列</param>
		/// <param name="duration"> 消えるまでの時間</param>
		protected void ShowNotification(string message, float duration = 1, System.Action action = null)
		{
			ShowNotification(new GUIContent(message), duration, action);
		}

		/// <summary>
		/// 通知用のトーストを表示する
		/// </summary>
		/// <param name="message"> 表示文字列</param>
		/// <param name="duration"> 消えるまでの時間</param>
		protected void ShowNotification(GUIContent message, float duration, System.Action action = null)
		{
			_iEnumerator.Add(NotificationCore(message, duration, action));
		}

		private IEnumerator NotificationCore(GUIContent message, float duration, System.Action action = null)
		{
			var timeSinceStartup = EditorApplication.timeSinceStartup;
			ShowNotification(message);

			while (EditorApplication.timeSinceStartup - timeSinceStartup < duration) {
				yield return null;
			}

			RemoveNotification();
			if (action != null) action();
		}

		protected void StartCoroutine(IEnumerator enumerator)
		{
			_iEnumerator.Add(enumerator);
		}

		public void DelayedCall(System.Action action, float duration)
		{
			_iEnumerator.Add(DelayedCallCore(action, duration));
		}

		private IEnumerator DelayedCallCore(System.Action action, float duration)
		{
			var timeSinceStartup = EditorApplication.timeSinceStartup;

			while (EditorApplication.timeSinceStartup - timeSinceStartup < duration) {
				yield return null;
			}
			action();
		}

		protected void DelayedCallNextFrame(System.Action action)
		{
			_iEnumerator.Add(DelayedCallNextFrameCore(action));
		}

		private IEnumerator DelayedCallNextFrameCore(System.Action action)
		{
			yield return null;
			action();
		}
	}
}