using UnityEngine;
using System;
using System.Collections;
using System.Text;
using UnityEngine.Networking;
using Newtonsoft.Json;

namespace Services
{
    /// <summary>
    /// �����ͺ��̽� ����
    /// DB ����� �����ϴ� �̱��� ����
    /// ���� DbManager�� API ����� MVVM�� �°� ����
    /// </summary>
    public class DatabaseService : MonoBehaviour
    {
        #region Singleton

        private static DatabaseService _instance;
        public static DatabaseService Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<DatabaseService>();

                    if (_instance == null)
                    {
                        Debug.LogError("[DatabaseService] ���� DatabaseService�� �����ϴ�!");
                    }
                }
                return _instance;
            }
        }

        #endregion

        [Header("Database Configuration")]
        [SerializeField] private string _apiUrl = "http://192.168.1.20:2000/";
        [SerializeField] private int _timeout = 30;
        [SerializeField] private bool _logQueries = true;

        [Header("Runtime Status")]
        [SerializeField] private bool _isInitialized = false;
        [SerializeField] private bool _isConnected = false;
        [SerializeField] private int _activeRequests = 0;

        public bool IsInitialized => _isInitialized;
        public bool IsConnected => _isConnected;
        public int ActiveRequests => _activeRequests;
        public string ApiUrl => _apiUrl;

        #region Unity �����ֱ�

        private void Awake()
        {
            // �̱��� ����
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);

            // PlayerPrefs���� URL �б� (������ ����)
            string storedUrl = PlayerPrefs.GetString("dbAddress");
            if (!string.IsNullOrEmpty(storedUrl))
            {
                _apiUrl = storedUrl;
                Debug.Log($"[DatabaseService] PlayerPrefs���� URL �ε�: {_apiUrl}");
            }
            else
            {
                Debug.LogWarning($"[DatabaseService] PlayerPrefs�� URL ����, �⺻�� ���: {_apiUrl}");
            }

            Initialize();
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        #endregion

        #region �ʱ�ȭ

        private void Initialize()
        {
            Debug.Log($"[DatabaseService] �ʱ�ȭ ���� - API URL: {_apiUrl}");

            try
            {
                // ���� �׽�Ʈ�� ���� ��� ������ ����
                _isInitialized = true;
                _isConnected = true; // �ϴ� true�� ���� (���� ������ ù ���� ���� �� Ȯ��)

                Debug.Log("[DatabaseService] �ʱ�ȭ �Ϸ�");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DatabaseService] �ʱ�ȭ ����: {ex.Message}");
                _isInitialized = false;
                _isConnected = false;
            }
        }

        #endregion

        #region ���� ���� (���� DbManager ���)

        /// <summary>
        /// SQL ���� ���� (�ڷ�ƾ ���)
        /// ���� DbManager�� ResponseAPIString�� ������ ���
        /// </summary>
        public void ExecuteQuery(string query, Action<string> onSuccess, Action<string> onError = null)
        {
            if (!_isInitialized)
            {
                string error = "DatabaseService�� �ʱ�ȭ���� �ʾҽ��ϴ�.";
                Debug.LogError($"[DatabaseService] {error}");
                onError?.Invoke(error);
                return;
            }

            StartCoroutine(ExecuteQueryCoroutine(query, onSuccess, onError));
        }

        /// <summary>
        /// SQL ���� ���� �ڷ�ƾ
        /// </summary>
        private IEnumerator ExecuteQueryCoroutine(string query, Action<string> onSuccess, Action<string> onError)
        {
            _activeRequests++;

            if (_logQueries)
            {
                Debug.Log($"[DatabaseService] ���� ����: {query}");
            }

            // JSON ��û ������ ���� (���� DbManager ���)
            var requestData = new
            {
                SQLType = "SELECT",
                SQLquery = query
            };

            string jsonData = JsonConvert.SerializeObject(requestData, Formatting.Indented);
            byte[] jsonBytes = Encoding.UTF8.GetBytes(jsonData);

            // HTTP POST ��û
            using (UnityWebRequest request = new UnityWebRequest(_apiUrl, "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(jsonBytes);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.timeout = _timeout;

                yield return request.SendWebRequest();

                _activeRequests--;

                if (request.result == UnityWebRequest.Result.Success)
                {
                    string response = request.downloadHandler.text;

                    if (_logQueries)
                    {
                        Debug.Log($"[DatabaseService] ���� ���� - ���� ����: {response.Length}");
                    }

                    onSuccess?.Invoke(response);
                }
                else
                {
                    string error = $"HTTP ��û ����: {request.error}";
                    Debug.LogError($"[DatabaseService] {error}");
                    onError?.Invoke(error);
                }
            }
        }

        #endregion

        #region Ÿ�Ժ� ���� ����

        /// <summary>
        /// ���� ���� �� JSON�� Ư�� Ÿ������ ������ȭ
        /// </summary>
        public void ExecuteQuery<T>(string query, Action<T> onSuccess, Action<string> onError = null)
        {
            ExecuteQuery(query,
                (response) =>
                {
                    try
                    {
                        T result = JsonConvert.DeserializeObject<T>(response);
                        onSuccess?.Invoke(result);
                    }
                    catch (Exception ex)
                    {
                        string error = $"JSON �Ľ� ����: {ex.Message}";
                        Debug.LogError($"[DatabaseService] {error}");
                        onError?.Invoke(error);
                    }
                },
                onError
            );
        }

        #endregion

        #region ��ƿ��Ƽ �޼���

        /// <summary>
        /// API URL ���� (��Ÿ��)
        /// </summary>
        public void SetApiUrl(string newUrl)
        {
            _apiUrl = newUrl;
            Debug.Log($"[DatabaseService] API URL ����: {_apiUrl}");
        }

        /// <summary>
        /// ���� �α� Ȱ��ȭ/��Ȱ��ȭ
        /// </summary>
        public void SetQueryLogging(bool enabled)
        {
            _logQueries = enabled;
            Debug.Log($"[DatabaseService] ���� �α�: {(enabled ? "Ȱ��ȭ" : "��Ȱ��ȭ")}");
        }

        /// <summary>
        /// Ÿ�Ӿƿ� ����
        /// </summary>
        public void SetTimeout(int seconds)
        {
            _timeout = seconds;
            Debug.Log($"[DatabaseService] Ÿ�Ӿƿ� ����: {_timeout}��");
        }

        #endregion

        #region ���� Ȯ��

        /// <summary>
        /// �����ͺ��̽� ���� �׽�Ʈ
        /// </summary>
        public void TestConnection(Action<bool> callback)
        {
            StartCoroutine(TestConnectionCoroutine(callback));
        }

        private IEnumerator TestConnectionCoroutine(Action<bool> callback)
        {
            Debug.Log("[DatabaseService] ���� �׽�Ʈ ��...");

            bool isSuccess = false;

            ExecuteQuery("SELECT 1;",
                (response) =>
                {
                    isSuccess = true;
                    _isConnected = true;
                    Debug.Log("[DatabaseService] ���� �׽�Ʈ ����");
                },
                (error) =>
                {
                    isSuccess = false;
                    _isConnected = false;
                    Debug.LogError($"[DatabaseService] ���� �׽�Ʈ ����: {error}");
                }
            );

            // ��û �Ϸ� ���
            yield return new WaitUntil(() => _activeRequests == 0);

            callback?.Invoke(isSuccess);
        }

        #endregion

        #region Inspector �����

#if UNITY_EDITOR
        [ContextMenu("���� �׽�Ʈ")]
        private void DebugTestConnection()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[DatabaseService] Play ��忡���� ���� �����մϴ�.");
                return;
            }

            TestConnection((success) =>
            {
                Debug.Log($"[DatabaseService] ���� �׽�Ʈ ���: {(success ? "����" : "����")}");
            });
        }

        [ContextMenu("API URL ���")]
        private void DebugPrintApiUrl()
        {
            Debug.Log($"[DatabaseService] ���� API URL: {_apiUrl}");
        }
#endif

        #endregion
    }
}