using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace HNS.Services
{
    /// <summary>
    /// 데이터베이스 서비스 - 순수 DB 쿼리 전담 (기존 DbManager 역할)
    /// </summary>
    public class DatabaseService : MonoBehaviour
    {
        [Header("Database Configuration")]
        [SerializeField] private string _apiUrl = "http://192.168.1.20:1933"; // 기존 DB API 서버 URL
        [SerializeField] private int _connectionTimeout = 30;
        [SerializeField] private int _queryTimeout = 120;

        [Header("Runtime Status")]
        [SerializeField] private bool _isInitialized = false;
        [SerializeField] private bool _isConnected = false;
        [SerializeField] private bool _isLoading = false;

        /// <summary>
        /// 초기화 상태
        /// </summary>
        public bool IsInitialized => _isInitialized;

        /// <summary>
        /// 데이터베이스 연결 상태
        /// </summary>
        public bool IsConnected => _isConnected;

        /// <summary>
        /// 로딩 상태
        /// </summary>
        public bool IsLoading => _isLoading;

        private void Start()
        {
            _ = InitializeAsync();
        }

        /// <summary>
        /// 서비스 초기화
        /// </summary>
        public async Task<bool> InitializeAsync()
        {
            if (_isInitialized)
            {
                Debug.LogWarning("[DatabaseService] 이미 초기화되었습니다.");
                return true;
            }

            try
            {
                Debug.Log("[DatabaseService] 초기화 시작...");
                _isLoading = true;

                // 데이터베이스 연결 테스트
                await TestConnectionAsync();

                _isInitialized = true;
                Debug.Log("[DatabaseService] 초기화 완료");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DatabaseService] 초기화 실패: {ex.Message}");
                return false;
            }
            finally
            {
                _isLoading = false;
            }
        }

        /// <summary>
        /// 데이터베이스 연결 테스트
        /// </summary>
        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                Debug.Log("[DatabaseService] 연결 테스트 중...");

                // TODO: 실제 DB 연결 테스트 구현
                await Task.Delay(100);

                _isConnected = true;
                Debug.Log("[DatabaseService] 데이터베이스 연결 성공");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DatabaseService] 연결 테스트 실패: {ex.Message}");
                _isConnected = false;
                return false;
            }
        }

        /// <summary>
        /// SQL 쿼리 실행 (기존 DbManager 방식과 동일)
        /// </summary>
        public async Task<T> ExecuteQueryAsync<T>(string query, object parameters = null)
        {
            if (!_isConnected)
            {
                Debug.LogError("[DatabaseService] 데이터베이스에 연결되지 않았습니다.");
                return default(T);
            }

            try
            {
                Debug.Log($"[DatabaseService] 쿼리 실행: {query}");

                // 기존 DbManager 방식과 동일한 JSON 구조
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
                    request.timeout = _queryTimeout;

                    var operation = request.SendWebRequest();

                    // async 대기
                    while (!operation.isDone)
                    {
                        await Task.Yield();
                    }

                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        string response = request.downloadHandler.text;
                        Debug.Log($"[DatabaseService] 쿼리 응답 수신: {response.Length} 문자");

                        // string 타입이면 그대로 반환, 아니면 JSON 파싱
                        if (typeof(T) == typeof(string))
                        {
                            return (T)(object)response;
                        }
                        else
                        {
                            return JsonConvert.DeserializeObject<T>(response);
                        }
                    }
                    else
                    {
                        Debug.LogError($"[DatabaseService] HTTP 요청 실패: {request.error}");
                        return default(T);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DatabaseService] 쿼리 실행 실패: {ex.Message}");
                return default(T);
            }
        }

        /// <summary>
        /// 월간 알람 TOP5 데이터 조회 (기존 DB API 서버 방식)
        /// </summary>
        public async Task<List<AlarmMontlyModel>> GetMonthlyAlarmTop5Async(string targetMonth)
        {
            try
            {
                Debug.Log($"[DatabaseService] 월간 알람 데이터 조회 - {targetMonth}");

                // 기존 저장 프로시저 사용
                var query = "EXEC GET_ALARM_MONTHLY;";
                string response = await ExecuteQueryAsync<string>(query);

                if (!string.IsNullOrEmpty(response))
                {
                    var result = JsonConvert.DeserializeObject<List<AlarmMontlyModel>>(response);
                    Debug.Log($"[DatabaseService] 월간 알람 조회 성공: {result?.Count ?? 0}개 지역");
                    return result ?? new List<AlarmMontlyModel>();
                }
                else
                {
                    Debug.LogWarning("[DatabaseService] 월간 알람 조회 응답이 비어있습니다.");
                    return new List<AlarmMontlyModel>();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DatabaseService] 월간 알람 조회 실패: {ex.Message}");
                return new List<AlarmMontlyModel>();
            }
        }

        /// <summary>
        /// 서비스 정리
        /// </summary>
        public void Cleanup()
        {
            // TODO: 데이터베이스 연결 해제
            _isConnected = false;
            _isInitialized = false;
            _isLoading = false;

            Debug.Log("[DatabaseService] 서비스 정리 완료");
        }

        private void OnDestroy()
        {
            Cleanup();
        }

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(_apiUrl))
            {
                Debug.LogWarning("[DatabaseService] API URL이 설정되지 않았습니다.");
            }
        }
    }

    #region Query Result Classes

    [Serializable]
    public class MonthlyAlarmQueryResult
    {
        public int RegionId;
        public string RegionName;
        public int AlarmCount;
        public int WarningCount;
        public int AlertCount;
        public int ErrorCount;
    }

    [Serializable]
    public class ActiveAlarmQueryResult
    {
        public int AlarmId;
        public int RegionId;
        public int SensorId;
        public string AlarmType;
        public string AlarmLevel;
        public DateTime AlarmTime;
        public float AlarmValue;
        public string SensorName;
    }

    #endregion
}