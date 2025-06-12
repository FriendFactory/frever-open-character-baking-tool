#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bridge;
using Bridge.Authorization.Models;
using Bridge.Models.AsseManager;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ExportCharacterTool
{
    public sealed class ExportCharacterToolWindow: EditorWindow
    {
        [MenuItem("Tools/Frever/Export Character Tool")]
        public static void ShowWindow()
        {
            GetWindow<ExportCharacterToolWindow>();
        }

        [MenuItem("Tools/Frever/Stop endless baking process")]
        public static void StopEndlessBakingProcess()
        {
            LoadContext().StopEndlessBakingProcess = true;
        }

        public static void ExecuteBaking()
        {
            ExecuteBackendSourceBaking(LoadContext());
        }

        private ExportingContext _context;
        private IBridge _bridge;
        private FFEnvironment _environment = FFEnvironment.Stage;
        private readonly List<long> _characterIds = new();
        private CharacterSource _characterSource = CharacterSource.CustomList;
        private bool _isLoggingIn;
        private string _email;
        private string _verificationCode;
        private bool _runEndlessBuildProcess;
        private string _idsTextToParse;
        private Vector2 _scrollPos;

        private async void Awake()
        {
            await Init();
        }

        private async Task Init()
        {
            _bridge = new ServerBridge();
            await TryToAutologin();
            Debug.Log(_bridge.Environment);
            _context = Resources.Load<ExportingContext>("ExportingContext");
        }

        private void OnGUI()
        {
            if (EditorApplication.isPlaying) return;
            if (_bridge == null)
            {
                if (GUILayout.Button("Try Again"))
                {
                    Init();
                }
                return;
            }
            
            if (_isLoggingIn)
            {
                EditorGUILayout.LabelField("Logging");
                return;
            }

            if (!_bridge.IsLoggedIn)
            {
                EditorGUILayout.LabelField("LOGIN");
                _environment = (FFEnvironment)EditorGUILayout.EnumPopup("Environment" ,_environment);
                if (_bridge.Environment != _environment)
                {
                    _bridge.ChangeEnvironment(_environment);    
                }
                
                _email = EditorGUILayout.TextField("Email", _email);
                if (GUILayout.Button("Send verification code"))
                {
                    _bridge.RequestEmailVerificationCode(_email);
                }
                _verificationCode = EditorGUILayout.TextField("VerificationCode", _verificationCode);
                if (string.IsNullOrEmpty(_email) || _verificationCode is not { Length: 6 })
                {
                    return;
                }
                Login(_email, _verificationCode);
                return;
            }
            
            _characterSource = (CharacterSource)EditorGUILayout.EnumPopup("CharacterSource", _characterSource);
            
            switch (_characterSource)
            {
                case CharacterSource.LoadedCharactersFromBackend:
                    RenderCommonExportSettings();
                    RenderBackendSourceGUI();
                    break;
                case CharacterSource.CustomList:
                    RenderCommonExportSettings();
                    RenderCustomIdListSourceGUI();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        
        private async Task TryToAutologin()
        {
            if (_bridge.LastLoggedEnvironment != null)
            {
                _isLoggingIn = true;
                await _bridge.LoginToLastSavedUserAsync();
                _isLoggingIn = false;
            }
        }

        private async void Login(string email, string verificationCode)
        {
            _isLoggingIn = true;
            var resp = await _bridge.LogInAsync(new EmailCredentials
            {
                Email = email,
                VerificationCode = verificationCode
            }, true);
            Debug.Log($"Login result: {resp.IsSuccess}");
            _isLoggingIn = false;
        }

        private void RenderCommonExportSettings()
        {
            _context.UploadToServer = EditorGUILayout.Toggle("Upload to Server", _context.UploadToServer);
            _context.ActivateOnUploading = EditorGUILayout.Toggle("Activate on Uploading", _context.ActivateOnUploading);
            _context.SaveCharactersOutsideTheProject = EditorGUILayout.Toggle("Save characters outside the project", _context.SaveCharactersOutsideTheProject);
            _context.CleanupExportFolderOnComplete = EditorGUILayout.Toggle("Cleanup Export Folder On Complete", _context.CleanupExportFolderOnComplete);
        }
        
        private void RenderCustomIdListSourceGUI()
        {
            EditorGUILayout.LabelField("Character Ids", EditorStyles.boldLabel);

            if (GUILayout.Button("Add Element"))
            {
                _characterIds.Add(0);
            }

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, GUILayout.MinHeight(200f));
            for (var i = 0; i < _characterIds.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                _characterIds[i] = EditorGUILayout.LongField($"Element {i}", _characterIds[i]);
                if (GUILayout.Button("Remove"))
                {
                    _characterIds.RemoveAt(i);
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();

            GUI.enabled = _characterIds.Any();
            if (GUILayout.Button("Load"))
            {
                _context.CharacterIds = _characterIds.ToArray();
                _context.CharacterSource = CharacterSource.CustomList;
                OpenExportScene();
                _context.ExportingProcessStep = ExportingStep.ExportPrefab;
                EditorApplication.EnterPlaymode();
            }
            GUI.enabled = true;
            
            EditorGUILayout.Space();
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Parse IDs from text");
            if (GUILayout.Button("Parse"))
            {
                var ids = ExtractNumbers(_idsTextToParse);
                _characterIds.Clear();
                _characterIds.AddRange(ids);
            }
            _idsTextToParse = EditorGUILayout.TextArea(_idsTextToParse);
        }

        private void RenderBackendSourceGUI()
        {
            if (_runEndlessBuildProcess)
            {
                EditorGUILayout.LabelField("Building characters");
                return;
            }

            if (GUILayout.Button("Start exporting"))
            {
                ExecuteBackendSourceBaking(_context);
            }
        }

        private static void ExecuteBackendSourceBaking(ExportingContext context)
        {
            PlayerSettings.colorSpace = ColorSpace.Gamma;
            OpenExportScene();
            context.CharacterIds = Array.Empty<long>();
            context.CharacterSource = CharacterSource.LoadedCharactersFromBackend;
            context.StopEndlessBakingProcess = false;
            context.ExportingProcessStep = ExportingStep.ExportPrefab;
            EditorApplication.EnterPlaymode();
        }

        private static void OpenExportScene()
        {
            const string scenePath = "Assets/Scenes/ExportCharacter.unity";
            if(EditorSceneManager.GetActiveScene().path == scenePath) return;
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        }

        private static ExportingContext LoadContext()
        {
            return Resources.Load<ExportingContext>("ExportingContext");
        }
        
        public static List<long> ExtractNumbers(string text)
        {
            // Split the text by commas or spaces
            char[] delimiters = new char[] { ',', ' ', '\n' };
            string[] numberStrings = text.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);

            // Convert the split strings to long numbers and store them in a list
            List<long> numbers = new List<long>();
            foreach (string numberString in numberStrings)
            {
                if (long.TryParse(numberString, out long number))
                {
                    numbers.Add(number);
                }
            }

            return numbers;
        }
    }
}
#endif