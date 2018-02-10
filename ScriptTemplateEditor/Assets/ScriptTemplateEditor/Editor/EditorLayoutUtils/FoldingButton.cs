#pragma warning disable 0414

using System;
using UnityEngine;

namespace Yosh.Editor
{
	public sealed class FoldingButton : IDisposable
	{
		private bool _isFolding;
		public FoldingButton(ref bool isFolding, string title, int fontSize, Color color, params GUILayoutOption[] options)
		{
			using (new ColorGroup(color)) {
				if (!GUILayout.Toggle(true, new GUIContent(string.Format("<b><size={0}>{1}{2}</size></b>", fontSize, !isFolding ? "▼" : "▶", title)), "dragtab", options))
					isFolding = !isFolding;
			}

			_isFolding = isFolding;

		}

		public void Dispose()
		{

		}
	}
}