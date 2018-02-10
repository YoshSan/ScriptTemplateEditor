using System;
using UnityEngine;

namespace Yosh.Editor
{
	/// <summary>
	/// EditorWindow
	/// </summary>
	public sealed class ColorGroup : IDisposable
	{
		private readonly Color _defaultColor;
		public ColorGroup(Color color)
		{
			_defaultColor = GUI.backgroundColor;
			GUI.color = color;
		}

		public void Dispose()
		{
			GUI.color = _defaultColor;
		}
	}
}