using System;
using UnityEditor;

namespace Yosh.Editor
{
	public sealed class IndentGroup : IDisposable
	{
		private readonly int _level;
		public IndentGroup(int level)
		{
			_level = level;
			EditorGUI.indentLevel += _level;
		}

		public void Dispose()
		{
			EditorGUI.indentLevel -= _level;
		}
	}
}