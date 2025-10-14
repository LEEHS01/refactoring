using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine.Networking;
using Newtonsoft.Json;

namespace Services
{
    /// <summary>
    /// 데이터베이스 서비스
    /// DB 통신을 전담하는 싱글톤 서비스
    /// 기존 DbManager의 API 방식을 MVVM에 맞게 래핑
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
                        Debug.LogError("[DatabaseService] 씬에 DatabaseService가 없습니다!");
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

        #region Unity 생명주기

        private void Awake()
        {
            // 싱글톤 설정
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);

            // PlayerPrefs에서 URL 읽기 (원본과 동일)
            string storedUrl = PlayerPrefs.GetString("dbAddress");
            if (!string.IsNullOrEmpty(storedUrl))
            {
                _apiUrl = storedUrl;
                Debug.Log($"[DatabaseService] PlayerPrefs에서 URL 로드: {_apiUrl}");
            }
            else
            {
                Debug.LogWarning($"[DatabaseService] PlayerPrefs에 URL 없음, 기본값 사용: {_apiUrl}");
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

        #region 초기화

        private void Initialize()
        {
            Debug.Log($"[DatabaseService] 초기화 시작 - API URL: {_apiUrl}");

            try
            {
                // 연결 테스트는 실제 사용 시점에 수행
                _isInitialized = true;
                _isConnected = true; // 일단 true로 설정 (실제 연결은 첫 쿼리 실행 시 확인)

                Debug.Log("[DatabaseService] 초기화 완료");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DatabaseService] 초기화 실패: {ex.Message}");
                _isInitialized = false;
                _isConnected = false;
            }
        }

        #endregion

        #region 쿼리 실행 (기존 DbManager 방식)

        /// <summary>
        /// SQL 쿼리 실행 (코루틴 방식)
        /// 기존 DbManager의 ResponseAPIString과 동일한 방식
        /// </summary>
        public void ExecuteQuery(string query, Action<string> onSuccess, Action<string> onError = null)
        {
            if (!_isInitialized)
            {
                string error = "DatabaseService가 초기화되지 않았습니다.";
                Debug.LogError($"[DatabaseService] {error}");
                onError?.Invoke(error);
                return;
            }

            StartCoroutine(ExecuteQueryCoroutine(query, onSuccess, onError));
        }

        /// <summary>
        /// SQL 쿼리 실행 코루틴
        /// </summary>
        private IEnumerator ExecuteQueryCoroutine(string query, Action<string> onSuccess, Action<string> onError)
        {
            _activeRequests++;

            if (_logQueries)
            {
                Debug.Log($"[DatabaseService] 쿼리 실행: {query}");
            }

            // JSON 요청 데이터 생성 (기존 DbManager 방식)
            var requestData = new
            {
                SQLType = "SELECT",
                SQLquery = query
            };

            string jsonData = JsonConvert.SerializeObject(requestData, Formatting.Indented);
            byte[] jsonBytes = Encoding.UTF8.GetBytes(jsonData);

            // HTTP POST 요청
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
                        Debug.Log($"[DatabaseService] 쿼리 성공 - 응답 길이: {response.Length}");
                    }

                    onSuccess?.Invoke(response);
                }
                else
                {
                    string error = $"HTTP 요청 실패: {request.error}";
                    Debug.LogError($"[DatabaseService] {error}");
                    onError?.Invoke(error);
                }
            }
        }

        #endregion

        #region 타입별 쿼리 실행

        /// <summary>
        /// 쿼리 실행 후 JSON을 특정 타입으로 역직렬화
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
                        string error = $"JSON 파싱 실패: {ex.Message}";
                        Debug.LogError($"[DatabaseService] {error}");
                        onError?.Invoke(error);
                    }
                },
                onError
            );
        }

        #endregion

        #region 비동기 쿼리 실행 (async/await)

        /// <summary>
        /// SQL 쿼리 실행 (비동기)
        /// Repository에서 async/await 방식으로 사용
        /// </summary>
        public async System.Threading.Tasks.Task<string> ExecuteQueryAsync(string query)
        {
            if (!_isInitialized)
            {
                string error = "DatabaseService가 초기화되지 않았습니다.";
                Debug.LogError($"[DatabaseService] {error}");
                return string.Empty;
            }

            if (_logQueries)
            {
                Debug.Log($"[DatabaseService] 쿼리 실행 (비동기): {query}");
            }

            // TaskCompletionSource로 콜백 → Task 변환
            var tcs = new System.Threading.Tasks.TaskCompletionSource<string>();

            StartCoroutine(ExecuteQueryCoroutineAsync(query,
                result => tcs.SetResult(result),
                error => tcs.SetResult(string.Empty)
            ));

            return await tcs.Task;
        }

        /// <summary>
        /// 비동기용 쿼리 실행 코루틴
        /// </summary>
        private IEnumerator ExecuteQueryCoroutineAsync(string query, Action<string> onSuccess, Action<string> onError)
        {
            _activeRequests++;

            // JSON 요청 데이터 생성
            var requestData = new
            {
                SQLType = "SELECT",
                SQLquery = query
            };

            string jsonData = JsonConvert.SerializeObject(requestData, Formatting.Indented);
            byte[] jsonBytes = Encoding.UTF8.GetBytes(jsonData);

            // HTTP POST 요청
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
                        Debug.Log($"[DatabaseService] 쿼리 성공 (비동기) - 응답 길이: {response.Length}");
                    }

                    onSuccess?.Invoke(response);
                }
                else
                {
                    string error = $"HTTP 요청 실패: {request.error}";
                    Debug.LogError($"[DatabaseService] {error}");
                    onError?.Invoke(error);
                }
            }
        }

        #endregion

        #region Procedure Execution (범용)

        /// <summary>
        /// 프로시저 실행 (범용) - Repository에서 사용
        /// </summary>
        /// <typeparam name="T">반환 타입</typeparam>
        /// <param name="procedureName">프로시저 이름</param>
        /// <param name="parameters">파라미터 딕셔너리</param>
        /// <param name="onSuccess">성공 콜백</param>
        /// <param name="onError">실패 콜백</param>
        public IEnumerator ExecuteProcedure<T>(
            string procedureName,
            Dictionary<string, object> parameters,
            Action<List<T>> onSuccess,
            Action<string> onError)
        {
            string query = BuildProcedureQuery(procedureName, parameters);

            if (_logQueries)
            {
                Debug.Log($"[DatabaseService] 프로시저 실행: {procedureName}");
            }

            yield return StartCoroutine(
                ExecuteQueryList<T>(query, procedureName, onSuccess, onError)
            );
        }

        /// <summary>
        /// 프로시저 쿼리 빌더
        /// </summary>
        private string BuildProcedureQuery(string procedureName, Dictionary<string, object> parameters)
        {
            if (parameters == null || parameters.Count == 0)
            {
                return $"EXEC {procedureName};";
            }

            var paramStrings = new List<string>();
            foreach (var param in parameters)
            {
                string value;

                if (param.Value == null)
                {
                    value = "NULL";
                }
                else if (param.Value is string)
                {
                    value = $"'{param.Value}'";
                }
                else if (param.Value is DateTime dateTime)
                {
                    value = $"'{dateTime:yyyy-MM-dd HH:mm:ss}'";
                }
                else if (param.Value is bool boolean)
                {
                    value = boolean ? "1" : "0";
                }
                else
                {
                    value = param.Value.ToString();
                }

                paramStrings.Add($"@{param.Key} = {value}");
            }

            return $"EXEC {procedureName} {string.Join(", ", paramStrings)};";
        }

        /// <summary>
        /// 쿼리 실행 후 List<T>로 역직렬화
        /// </summary>
        private IEnumerator ExecuteQueryList<T>(
            string query,
            string procedureName,
            Action<List<T>> onSuccess,
            Action<string> onError)
        {
            bool isCompleted = false;

            ExecuteQuery(query,
                (response) =>
                {
                    try
                    {
                        List<T> result = JsonConvert.DeserializeObject<List<T>>(response);

                        if (result == null)
                        {
                            result = new List<T>();
                            Debug.LogWarning($"[DatabaseService] {procedureName} - 결과가 null, 빈 리스트 반환");
                        }

                        if (_logQueries)
                        {
                            Debug.Log($"[DatabaseService] {procedureName} 성공: {result.Count}개 행 반환");
                        }

                        onSuccess?.Invoke(result);
                    }
                    catch (Exception ex)
                    {
                        string error = $"JSON 파싱 실패: {ex.Message}";
                        Debug.LogError($"[DatabaseService] {procedureName} - {error}");
                        onError?.Invoke(error);
                    }
                    finally
                    {
                        isCompleted = true;
                    }
                },
                (error) =>
                {
                    Debug.LogError($"[DatabaseService] {procedureName} - HTTP 오류: {error}");
                    onError?.Invoke(error);
                    isCompleted = true;
                }
            );

            // 완료 대기
            yield return new WaitUntil(() => isCompleted);
        }

        #endregion

        #region 유틸리티 메서드

        /// <summary>
        /// API URL 변경 (런타임)
        /// </summary>
        public void SetApiUrl(string newUrl)
        {
            _apiUrl = newUrl;
            Debug.Log($"[DatabaseService] API URL 변경: {_apiUrl}");
        }

        /// <summary>
        /// 쿼리 로그 활성화/비활성화
        /// </summary>
        public void SetQueryLogging(bool enabled)
        {
            _logQueries = enabled;
            Debug.Log($"[DatabaseService] 쿼리 로그: {(enabled ? "활성화" : "비활성화")}");
        }

        /// <summary>
        /// 타임아웃 설정
        /// </summary>
        public void SetTimeout(int seconds)
        {
            _timeout = seconds;
            Debug.Log($"[DatabaseService] 타임아웃 설정: {_timeout}초");
        }

        #endregion

        #region 상태 확인

        /// <summary>
        /// 데이터베이스 연결 테스트
        /// </summary>
        public void TestConnection(Action<bool> callback)
        {
            StartCoroutine(TestConnectionCoroutine(callback));
        }

        private IEnumerator TestConnectionCoroutine(Action<bool> callback)
        {
            Debug.Log("[DatabaseService] 연결 테스트 중...");

            bool isSuccess = false;

            ExecuteQuery("SELECT 1;",
                (response) =>
                {
                    isSuccess = true;
                    _isConnected = true;
                    Debug.Log("[DatabaseService] 연결 테스트 성공");
                },
                (error) =>
                {
                    isSuccess = false;
                    _isConnected = false;
                    Debug.LogError($"[DatabaseService] 연결 테스트 실패: {error}");
                }
            );

            // 요청 완료 대기
            yield return new WaitUntil(() => _activeRequests == 0);

            callback?.Invoke(isSuccess);
        }

        #endregion

        #region Inspector 디버깅

#if UNITY_EDITOR
        [ContextMenu("연결 테스트")]
        private void DebugTestConnection()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[DatabaseService] Play 모드에서만 실행 가능합니다.");
                return;
            }

            TestConnection((success) =>
            {
                Debug.Log($"[DatabaseService] 연결 테스트 결과: {(success ? "성공" : "실패")}");
            });
        }

        [ContextMenu("API URL 출력")]
        private void DebugPrintApiUrl()
        {
            Debug.Log($"[DatabaseService] 현재 API URL: {_apiUrl}");
        }
#endif

        #endregion
    }
}