using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Diagnostics;
using Debug = UnityEngine.Debug;
namespace Yosh.Editor
{
	/// <summary>
	/// スクリプトテンプレート生成ウィンドウ
	/// </summary>
	public class ScriptTemplateEditor : EditorWindowBase
	{
		public enum SCRIPT_TYPE
		{
			Csharp,
			Shader,
			ComputeShader
		}

		private class TemplateData
		{
			public string FullName = "";
			//Createメニューに表示する優先度 (昇順)
			public int Priority = 81;
			//Createメニューの表示名
			public string MenuTitle = "";
			//Createメニューの表示名
			public string FileName = "";
			//Createメニューの階層
			public List<string> Hierarchy = new List<string>();
			//ファイル化する文字列
			public string Text = "";
			//生成するファイルの種類
			public SCRIPT_TYPE Type = SCRIPT_TYPE.Csharp;

			public TemplateData() { }
			public TemplateData(TemplateData original)
			{
				FullName = original.FullName;
				Priority = original.Priority;
				MenuTitle = original.MenuTitle;
				FileName = original.FileName;
				Hierarchy = original.Hierarchy;
				Text = original.Text;
				Type = original.Type;
			}

			public TemplateData(string text, SCRIPT_TYPE type)
			{
				Text = text;
				Type = type;
			}

			public TemplateData(string fileName, string text)
			{
				FullName = fileName + ".txt";
				var priority = fileName.Substring(0, 2);
				fileName = fileName.Remove(0, 3);
				Priority = int.Parse(priority);

				var separator = new string[] { "__", "-" };
				var splits = fileName.Split(separator, StringSplitOptions.RemoveEmptyEntries);

				MenuTitle = splits[splits.Length - 2];
				FileName = splits[splits.Length - 1];

				foreach (var split in splits.Take(splits.Length - 2))
					Hierarchy.Add(split);

				Text = text;

			}

		}

		#region const

		private const string COMMAND_PATH = "Tool/ScriptTemplateEditor";
		private const string ASSETS = "Assets/";
		private const string TEMPLATE_PATH = "/Resources/ScriptTemplates/";
		private const int FOTTER_HEIGHT = 30;
		private const string TMP_FOCUS = "focus";
		private const string REGEX = "(?<=ScriptTemplates\\/)(\\d{2}-.*)\\.txt";
		private Dictionary<string, string> REPLACE_TAG =
			new Dictionary<string, string>()
			{
				{ "#SCRIPTNAME#","クラス名にこのタグを設定することで、ファイル名に置換されます" },
				//{ "#NAMESPACE#","" },
			};

		/// <summary>
		///  C:/Program Files/Unity/Editor/Data/Resources/ScriptTemplates/
		/// </summary>
		private static readonly string DIRECTORY_PATH = EditorApplication.applicationContentsPath + TEMPLATE_PATH;
		#endregion

		#region variable
		TemplateData _tData = new TemplateData();

		Vector2 _scroll;
		bool _isPreviewFolding = false;
		bool _isHierarchyFolding = false;
		bool _tagPreviewFolding = true;
		bool _templateFolding = true;
		Dictionary<string, TemplateData> _savedTemplateData = new Dictionary<string, TemplateData>();
		bool _isAdministrator = false;
		#endregion

		#region property		

		/// <summary>
		/// 再生中、コンパイル中は実行できない
		/// </summary>
		private bool CanCreate { get { return !EditorApplication.isPlaying && !Application.isPlaying && !EditorApplication.isCompiling; } }

		/// <summary>
		/// 生成するテンプレートファイルのファイル名
		/// </summary>
		private string GENERATE_FILE_NAME
		{
			get {
				var sb = new System.Text.StringBuilder();
				sb.AppendFormat("{0:D2}-", _tData.Priority);
				for (int i = 0; i < _tData.Hierarchy.Count; i++)
					sb.AppendFormat("{0}__", _tData.Hierarchy[i]);

				sb.Append(_tData.MenuTitle);
				sb.AppendFormat("-{0}", _tData.FileName);

				if (!_tData.FileName.Contains(TypeToString(_tData.Type)))
					sb.Append(TypeToString(_tData.Type));
				sb.Append(".txt");
				return sb.ToString();

			}
		}
		#endregion

		#region MenuItem

		/// <summary>
		/// 編集ウィンドウを開く
		/// </summary>
		[MenuItem(COMMAND_PATH + "Open")]
		static void OpenWindow()
		{
			GetWindow<ScriptTemplateEditor>("ScriptTemplateEditor");
		}

		/// <summary>
		/// ProjectViewで選択したファイルの内容を引用して編集ウィンドウを開く
		/// </summary>
		[MenuItem(ASSETS + "AddToTemplate",false,1)]
		static void OpenfromSelectionFile()
		{
			var obj = Selection.activeObject;
			SCRIPT_TYPE type = SCRIPT_TYPE.Csharp;
			bool isAvailable = CheckFileType(obj, ref type);

			if (!isAvailable) return;

			var window = GetWindow<ScriptTemplateEditor>("ScriptTemplateEditor");
			window._tData = CreateDataFromFile(obj, type);
		}

		/// <summary>
		/// ProjectViewで選択したファイルの内容を引用して編集ウィンドウを開く
		/// </summary>
		[MenuItem(ASSETS + "AddToTemplate", true, 1)]
		static bool OpenfromSelectionFileValidate()
		{
			var obj = Selection.activeObject;
			SCRIPT_TYPE type = SCRIPT_TYPE.Csharp;
			return CheckFileType(obj, ref type);
		}

		#endregion

		#region function

		protected override void OnEnable()
		{
			_isAdministrator = CheckAdministrator();
			base.OnEnable();
			SearchTemplateFiles();
		}

		/// <summary>
		/// Enum -> Sufix
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public static string TypeToString(SCRIPT_TYPE type)
		{
			switch (type) {
			case SCRIPT_TYPE.Csharp:
				return ".cs";
			case SCRIPT_TYPE.Shader:
				return ".shader";
			case SCRIPT_TYPE.ComputeShader:
				return ".compute";
			default:
				return "";
			}
		}

		/// <summary>
		/// Scriptの生成ルールを満たしているかの確認
		/// </summary>
		/// <returns> error == false</returns>
		private bool HasError()
		{
			bool result = false;
			_tData.Priority = Mathf.Max(0, _tData.Priority);
			if (string.IsNullOrEmpty(_tData.MenuTitle)) {
				EditorGUILayout.HelpBox("メニュー表示名を入力してください", MessageType.Error);
				result = true;
			}

			if (string.IsNullOrEmpty(_tData.FileName)) {
				EditorGUILayout.HelpBox("テンプレートファイル名を入力してください", MessageType.Error);
				result = true;
			}

			if (string.IsNullOrEmpty(_tData.Text)) {
				EditorGUILayout.HelpBox("スクリプト内容を入力してください", MessageType.Error);
				result = true;
			}

			return result;
		}

		/// <summary>
		/// Script生成の実行
		/// </summary>
		private void GenerateTemplate()
		{
			//階層の空文字チェック
			_tData.Hierarchy = _tData.Hierarchy
				.Where(str => !String.IsNullOrEmpty(str))
				.ToList();

			var directoryName = Path.GetDirectoryName(DIRECTORY_PATH + GENERATE_FILE_NAME);
			if (!Directory.Exists(directoryName))
				Directory.CreateDirectory(directoryName);

			File.WriteAllText(DIRECTORY_PATH + GENERATE_FILE_NAME, _tData.Text, System.Text.Encoding.UTF8);
			AssetDatabase.Refresh(ImportAssetOptions.ImportRecursive);

			Action onComplete = () =>
			{
				var title = "再起動確認";
				var message = "作成したテンプレートを反映するためには、再起動が必要です。\n再起動を行いますか？";
				if (LayoutTemplate.ShowDisplayDialog(title, message, "再起動"))
					RestartEditor();
				else
					DelayedCallNextFrame(() => SearchTemplateFiles());
			};

			ShowNotification("テンプレートを作成しました", 1, onComplete);

		}

		/// <summary>
		/// 指定したファイルを削除する
		/// </summary>
		/// <param name="fileName"></param>
		private void DeleteTemplate(TemplateData data)
		{
			var title = "ファイル削除確認";
			var message = string.Format("ファイル名：{0}を削除します。\n本当によろしいですか？", data.FileName);

			if (LayoutTemplate.ShowDisplayDialog(title, message)) {
				File.Delete(DIRECTORY_PATH + data.FullName);
				ShowNotification(string.Format("{0}を削除しました", data.FileName));
				DelayedCallNextFrame(() => SearchTemplateFiles());
			}
		}

		/// <summary>
		/// DIRECTORY_PATH 内のファイルを取得する
		/// </summary>
		private void SearchTemplateFiles()
		{
			_savedTemplateData.Clear();
			var files = Directory.GetFiles(DIRECTORY_PATH, "*", SearchOption.AllDirectories);

			foreach (var file in files) {
				var match = Regex.Match(file, REGEX);

				if (Regex.IsMatch(file, REGEX)) {
					var text = ReadFile(file);
					TemplateData data = new TemplateData(match.Groups[1].Value, text);

					if (!_savedTemplateData.ContainsKey(data.FileName))
						_savedTemplateData.Add(data.FileName, data);
				}

			}
		}

		/// <summary>
		/// ファイルを読み込む
		/// </summary>
		/// <param name="fileName"></param>
		private static string ReadFile(string fileName)
		{
			using (var streamReader = new StreamReader(fileName, System.Text.Encoding.UTF8))
				return streamReader.ReadToEnd();
		}

		/// <summary>
		/// 管理者権限での実行が行われているか確認する
		/// </summary>
		/// <returns></returns>
		private bool CheckAdministrator()
		{
			var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
			var principal = new System.Security.Principal.WindowsPrincipal(identity);
			return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
		}

		/// <summary>
		/// UnityEditorを再起動する
		/// http://baba-s.hatenablog.com/entry/2017/11/13/150507
		/// </summary>
		private void RestartEditor()
		{
			var filename = EditorApplication.applicationPath;
			var arguments = "-projectPath " + Application.dataPath.Replace("/Assets", string.Empty);
			var startInfo = new ProcessStartInfo
			{
				FileName = filename,
				Arguments = arguments,
			};
			Process.Start(startInfo);

			EditorApplication.Exit(0);
		}

		/// <summary>
		/// 選択されたファイルの型を判定します
		/// </summary>
		/// <returns></returns>
		static private bool CheckFileType(object selectionFile, ref SCRIPT_TYPE type)
		{
			if (selectionFile as MonoScript) {
				type = SCRIPT_TYPE.Csharp;
				return true;
			}

			if (selectionFile as Shader) {
				type = SCRIPT_TYPE.Shader;
				return true;
			}

			if (selectionFile as ComputeShader) {
				type = SCRIPT_TYPE.ComputeShader;
				return true;
			}
			return false;
		}

		static private TemplateData CreateDataFromFile(UnityEngine.Object obj, SCRIPT_TYPE type)
		{
			var path = AssetDatabase.GetAssetPath(obj);
			var text = ReadFile(path);
			return new TemplateData(text, type);
		}

		#endregion

		#region GUI function

		private void OnGUI()
		{
			DrawSettingPanel();
			DrawTemplateList();

			//生成ボタン
			if (!_isAdministrator)
				EditorGUILayout.HelpBox("テンプレートの作成には管理者権限が必要です。\nUnityEditorを管理者として実行してください。", MessageType.Error);
			using (new EditorGUI.DisabledGroupScope(HasError() || !_isAdministrator || !CanCreate)) {
				using (new ColorGroup(new Color32(218, 169, 112, 255)))
					if (GUILayout.Button("作成"))
						GenerateTemplate();
			}

			//置換タグの種類表示
			using (new FoldingButton(ref _tagPreviewFolding, "置換タグ一覧", 14, GUI.backgroundColor)) {
				if (!_tagPreviewFolding) {
					using (new EditorGUILayout.VerticalScope(GUI.skin.box)) {
						foreach (var tag in REPLACE_TAG) {
							GUI.SetNextControlName(TMP_FOCUS);
							EditorGUILayout.SelectableLabel(string.Format("{0}・・・{1}", tag.Key, tag.Value));
						}
					}
				}
			}

			//Script入力欄
			using (new FoldingButton(ref _isPreviewFolding, "Scriptをここに入力してください", 14, new Color32(133, 184, 217, 255), GUILayout.MinWidth(20))) {
				if (!_isPreviewFolding) {
					_scroll = EditorGUILayout.BeginScrollView(_scroll, GUI.skin.box);
					_tData.Text = EditorGUILayout.TextArea(_tData.Text, GUILayout.Height(position.height - FOTTER_HEIGHT));
					EditorGUILayout.EndScrollView();
				}
			}

		}

		/// <summary>
		/// 生成ファイル設定入力欄の描画
		/// </summary>
		private void DrawSettingPanel()
		{
			using (new ContentsGroup(GUI.backgroundColor)) {
				_tData.Type = (SCRIPT_TYPE) EditorGUILayout.EnumPopup("ファイル種類", _tData.Type);
				_tData.FileName = EditorGUILayout.TextField("テンプレートファイル名", _tData.FileName);
				_tData.MenuTitle = EditorGUILayout.TextField("メニュー表示名", _tData.MenuTitle);
				_tData.Priority = EditorGUILayout.IntField("表示優先度", _tData.Priority);

				using (new FoldingButton(ref _isHierarchyFolding, "階層名", 12, GUI.backgroundColor)) {

					if (_isHierarchyFolding) return;

					for (int i = 0; i < _tData.Hierarchy.Count; i++)
						_tData.Hierarchy[i] = EditorGUILayout.TextField("階層名", _tData.Hierarchy[i]);

					if (GUILayout.Button("階層を追加"))
						_tData.Hierarchy.Add("");
				}
			}
		}

		/// <summary>
		/// 保存済みのテンプレートを表示する
		/// </summary>
		private void DrawTemplateList()
		{
			if (!_isAdministrator) return;

			//作成済みテンプレート一覧
			using (new FoldingButton(ref _templateFolding, "作成済みテンプレート一覧", 14, GUI.backgroundColor)) {
				if (!_templateFolding) {
					using (new EditorGUILayout.VerticalScope(GUI.skin.box)) {
						foreach (var template in _savedTemplateData) {
							EditorGUILayout.BeginHorizontal();
							if (GUILayout.Button(string.Format("{0}", template.Key), GUILayout.Height(26))) {
								GUI.FocusControl(TMP_FOCUS);
								_tData = new TemplateData(template.Value);
							}

							if (LayoutTemplate.CloseButton())
								DeleteTemplate(template.Value);
							EditorGUILayout.EndHorizontal();
						}

					}
				}
			}
		}
		#endregion


	}

}