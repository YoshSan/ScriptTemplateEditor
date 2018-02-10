using UnityEngine;
using UnityEditor;
using System.Collections;

namespace Yosh.Editor
{
	public static class LayoutTemplate
	{
		/// <summary>
		/// 検索フィルター用テキストフィールド（Editor拡張用）
		/// </summary>
		/// <param name="filter"></param>
		/// <returns></returns>
		public static string SerchField(string filter, string label = "Filter", int labelWidth = 50)
		{
			GUILayout.BeginHorizontal();
			GUILayout.Label(label + ":", GUILayout.Width(labelWidth));
			filter = GUILayout.TextField(filter, "SearchTextField", GUILayout.Width(150));
			GUI.enabled = !string.IsNullOrEmpty(filter);
			if (GUILayout.Button("Clear", "SearchCancelButton")) {
				filter = string.Empty;
			}
			GUI.enabled = true;
			GUILayout.EndHorizontal();

			return filter;
		}

		/// <summary>
		/// ダイアログを表示する
		/// </summary>
		public static void ShowDisplayDialog(string title, string message, System.Action onOk, System.Action onCancel, string okTitle = "OK", string cancelTitle = "キャンセル")
		{
			if (EditorUtility.DisplayDialog(title, message, okTitle, cancelTitle)) {
				if (onOk != null)
					onOk();
			}
			else {
				if (onCancel != null)
					onCancel();
			}

		}

		/// <summary>
		/// ダイアログを表示する
		/// </summary>
		public static bool ShowDisplayDialog(string title, string message, string okTitle = "OK", string cancelTitle = "キャンセル")
		{
			if (EditorUtility.DisplayDialog(title, message, okTitle, cancelTitle))
				return true;
			else
				return false;

		}

		/// <summary>
		/// 閉じるボタン
		/// </summary>
		/// <returns></returns>
		public static bool CloseButton()
		{
			var layout = EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector).GetStyle("TL SelectionBarCloseButton");
			return GUILayout.Button("", layout);
		}

	}
}