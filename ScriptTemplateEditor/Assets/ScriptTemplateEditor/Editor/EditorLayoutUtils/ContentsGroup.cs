using System;
using UnityEngine;
using UnityEditor;

namespace Yosh.Editor
{
	public sealed class ContentsGroup : IDisposable
	{
		public ContentsGroup() : this(new Color(0.1f, 0.1f, 0.2f)) { }

		public ContentsGroup(Color color)
		{
			GUILayout.Space(8f);
			GUILayout.BeginHorizontal();
			GUILayout.Space(4f);
			using (new ColorGroup(color)) {
				EditorGUILayout.BeginHorizontal(EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector).GetStyle("TE NodeBoxSelected"), GUILayout.MinHeight(10f));
			}
			GUILayout.BeginVertical();
			GUILayout.Space(4f);
		}

		public void Dispose()
		{
			GUILayout.Space(3f);
			GUILayout.EndVertical();
			EditorGUILayout.EndHorizontal();
			GUILayout.Space(3f);
			GUILayout.EndHorizontal();
			GUILayout.Space(3f);
		}
	}
}