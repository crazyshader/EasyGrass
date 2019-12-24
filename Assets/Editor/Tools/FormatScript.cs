using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EasyFramework.Editor
{
    public class FormatScript : EditorWindow
    {
        private bool needFormat;
        private bool isInsertSpaces;
        private bool isInsertTables;
        private int needFormatFileCount;
        private int selectLineEndingIndex;
        private int selectEncodingIndex;
        private int spaceCount = 4;
        private string formattingFileName;
        private string fileExtension = "*.cs";
        private string[] fileSuffixs;
        private readonly List<string> formattingFiles = new List<string>();
        private Object fileLists = null;

        // 以下两个数组一一对应
        private readonly string[] encodingTypes =
        {
            "UTF-8",
            "UTF-32",
            "Unicode",
            "GB2312",
    };

        private readonly Encoding[] encodings =
        {
        Encoding.UTF8,
        Encoding.UTF32,
        Encoding.Unicode,
        Encoding.GetEncoding("GB2312"),
    };

        // 以下两个数组一一对应
        private readonly string[] lineEndingTypes =
        {
            "Windows(CRLF)",
            "Unix(LF)",
    };

        private readonly string[] lineEndings =
        {
        "\r\n",
        "\n"
    };

        [MenuItem("Window/FormatScript &c")]
        private static void Init()
        {
            var window = GetWindow<FormatScript>();
            window.titleContent = new GUIContent("FormatScript");
            window.maximized = true;
            window.Show();
        }

        private void Update()
        {
            if (!needFormat || needFormatFileCount == 0)
                return;

            if (formattingFiles.Count == 0)
            {
                needFormat = false;
                //EditorUtility.DisplayDialog("FormatDialog", "Format Finish", "ok");
                Debug.Log("Format Finished!");
                return;
            }

            formattingFileName = formattingFiles[0];
            HandleCurFile(formattingFileName);
            formattingFiles.RemoveAt(0);
            DrawProgress();
            Repaint();
        }

        private void OnGUI()
        {
            DrawControlPad();
        }

        private void HandleCurFile(string fileName)
        {
            try
            {
                string content = File.ReadAllText(fileName);

                // 替换换行符
                content = content.Replace("\r", "");
                content = content.Replace("\n", lineEndings[selectLineEndingIndex]);

                // 处理制表符
                if (isInsertSpaces && !isInsertTables)
                {
                    content = content.Replace("\t", new string(' ', spaceCount));
                }
                if (!isInsertSpaces && isInsertTables)
                {
                    content = content.Replace(new string(' ', spaceCount), "\t");
                }

                // 按对应编码写入文件
                File.WriteAllText(fileName, content, encodings[selectEncodingIndex]);
            }
            catch (Exception ex)
            {
                Debug.LogErrorFormat("FormatScript HandleCurFile Format file faild, fileName: {0} msg: {1}", fileName, ex.Message);
            }
        }

        private void DrawProgress()
        {
            if (string.IsNullOrEmpty(formattingFileName)) return;

            float progress = (float)(needFormatFileCount - formattingFiles.Count) / (float)needFormatFileCount;
            EditorUtility.DisplayProgressBar("Format Script...", formattingFileName, progress);
            Debug.LogFormat("Format Script:{0}", formattingFileName);

            if (progress >= 1f)
            {
                AssetDatabase.SaveAssets();
                EditorUtility.UnloadUnusedAssetsImmediate();
                EditorUtility.ClearProgressBar();
            }
        }

        private void DrawControlPad()
        {
            EditorGUILayout.BeginVertical();

            EditorGUILayout.Space();
            HandleFilePath();

            EditorGUILayout.Space();
            HandleFileSuffix();

            EditorGUILayout.Space();
            HandleFileEncoding();

            EditorGUILayout.Space();
            HandleFileLineEnding();

            EditorGUILayout.Space();
            HandleFileTab();

            EditorGUILayout.Space();
            if (GUILayout.Button("Start"))
            {
                StartFormat();
            }

            EditorGUILayout.EndVertical();
        }

        private void StartFormat()
        {
            if (fileLists == null)
            {
                EditorUtility.DisplayDialog("FormatDialog", "You need select a exist script or folder", "ok");
                return;
            }
            string filePath = AssetDatabase.GetAssetPath(fileLists);
            if (string.IsNullOrEmpty(filePath))
            {
                EditorUtility.DisplayDialog("FormatDialog", "You need select a exist script or folder", "ok");
                return;
            }

            if (string.IsNullOrEmpty(fileExtension))
            {
                EditorUtility.DisplayDialog("FormatDialog", "You need select some file suffix", "ok");
                return;
            }
            fileSuffixs = fileExtension.Split(';');
            formattingFiles.Clear();
            foreach (var curSuffix in fileSuffixs)
            {
                if (string.IsNullOrEmpty(curSuffix))
                {
                    continue;
                }

                if (!Directory.Exists(filePath))
                {
                    if (curSuffix.Substring(1) == Path.GetExtension(filePath))
                    {
                        formattingFiles.Add(filePath);
                    }
                }
                else
                {
                    string[] filenames = Directory.GetFiles(filePath, curSuffix, SearchOption.AllDirectories);
                    if (filenames.Length > 0)
                    {
                        formattingFiles.AddRange(filenames);
                    }
                }
            }
            needFormatFileCount = formattingFiles.Count;
            if (needFormatFileCount == 0)
            {
                EditorUtility.DisplayDialog("FormatDialog", "Can't find any file in seleted", "ok");
                return;
            }
            needFormat = true;
        }

        private void HandleFilePath()
        {
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField("Folder, e.g. Assets/Scripts/");
            fileLists = EditorGUILayout.ObjectField(fileLists, typeof(Object), false);
            EditorGUILayout.EndHorizontal();
        }

        private void HandleFileSuffix()
        {
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField("File extension，e.g. *.txt;*.cs");
            fileExtension = EditorGUILayout.TextField(fileExtension);

            EditorGUILayout.EndHorizontal();
        }

        private void HandleFileEncoding()
        {
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField("Encoding");
            selectEncodingIndex = EditorGUILayout.Popup(selectEncodingIndex, encodingTypes);

            EditorGUILayout.EndHorizontal();
        }

        private void HandleFileLineEnding()
        {
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField("LineEnding");
            selectLineEndingIndex = EditorGUILayout.Popup(selectLineEndingIndex, lineEndingTypes);

            EditorGUILayout.EndHorizontal();
        }

        private void HandleFileTab()
        {
            EditorGUILayout.BeginVertical();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("InsertTables");
            isInsertTables = EditorGUILayout.Toggle(isInsertTables) & !isInsertSpaces;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("InsertSpaces");
            isInsertSpaces = EditorGUILayout.Toggle(isInsertSpaces) & !isInsertTables;
            EditorGUILayout.EndHorizontal();

            if (isInsertSpaces || isInsertTables)
            {
                EditorGUILayout.Space();
                EditorGUILayout.BeginHorizontal();

                EditorGUILayout.LabelField("SpaceCount");
                spaceCount = EditorGUILayout.IntField(spaceCount);

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
        }
    }
}

