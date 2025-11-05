using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Models.MonitorB;
using Repositories.MonitorB;

namespace ViewModels.MonitorB
{
    /// <summary>
    /// 센서 차트 ViewModel (캐싱 기능 추가)
    /// - 10분간 차트 데이터 캐싱
    /// - 알람 발생 시 캐시 무효화
    /// </summary>
    public class SensorChartViewModel : MonoBehaviour
    {
        public static SensorChartViewModel Instance { get; private set; }

        public event Action<ChartData> OnChartDataLoaded;
        public event Action<string> OnError;

        public ChartData currentChartData { get; private set; }

        private int currentBoardId = -1;
        private int currentHnsId = -1;

        public ChartData CurrentChartData => currentChartData;

        #region 캐싱 관련

        // 차트 데이터 캐시
        private Dictionary<string, CachedChartData> _chartCache = new Dictionary<string, CachedChartData>();

        // 캐시 만료 시간 (10분)
        private const int CACHE_EXPIRY_MINUTES = 10;

        /// <summary>
        /// 캐시된 차트 데이터
        /// </summary>
        private class CachedChartData
        {
            public ChartData Data { get; set; }
            public DateTime LoadedAt { get; set; }

            public bool IsExpired(int expiryMinutes)
            {
                var age = DateTime.Now - LoadedAt;
                return age.TotalMinutes >= expiryMinutes;
            }
        }

        #endregion

        #region Unity 생명주기

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("[SensorChartViewModel] 초기화 완료");
        }

        #endregion

        #region 차트 데이터 로드

        /// <summary>
        /// 특정 센서의 차트 데이터 로드 (캐싱 적용)
        /// </summary>
        public async void LoadChartData(int obsId, int boardId, int hnsId)
        {
            if (obsId <= 0 || boardId <= 0 || hnsId <= 0)
            {
                Debug.LogError($"[SensorChartViewModel] 잘못된 파라미터: obsId={obsId}, boardId={boardId}, hnsId={hnsId}");
                OnError?.Invoke("잘못된 센서 정보입니다.");
                return;
            }

            currentBoardId = boardId;
            currentHnsId = hnsId;

            string cacheKey = GetCacheKey(obsId, boardId, hnsId);

            // 캐시 확인
            if (_chartCache.ContainsKey(cacheKey))
            {
                var cachedData = _chartCache[cacheKey];

                // 캐시가 유효하면 재사용
                if (!cachedData.IsExpired(CACHE_EXPIRY_MINUTES))
                {
                    var cacheAge = (DateTime.Now - cachedData.LoadedAt).TotalMinutes;
                    Debug.Log($"[SensorChartViewModel] 캐시된 차트 데이터 사용 (캐시 생성: {cacheAge:F1}분 전)");

                    currentChartData = cachedData.Data;
                    OnChartDataLoaded?.Invoke(cachedData.Data);
                    return;
                }
                else
                {
                    Debug.Log($"[SensorChartViewModel] 캐시 만료 - 새로 로드");
                    _chartCache.Remove(cacheKey);
                }
            }

            // 캐시가 없거나 만료되었으면 새로 로드
            Debug.Log($"[SensorChartViewModel] 차트 데이터 로드: Board={boardId}, HNS={hnsId}");

            try
            {
                var chartData = await ChartRepository.Instance.GetSensorChartDataAsync(obsId, boardId, hnsId);

                if (chartData == null)
                {
                    Debug.LogWarning($"[SensorChartViewModel] 차트 데이터 없음");
                    OnError?.Invoke("차트 데이터를 불러올 수 없습니다.");
                    return;
                }

                // 캐시에 저장
                _chartCache[cacheKey] = new CachedChartData
                {
                    Data = chartData,
                    LoadedAt = DateTime.Now
                };

                currentChartData = chartData;
                Debug.Log($"[SensorChartViewModel] 차트 데이터 로드 완료: {chartData.values.Count}개 포인트 (캐시 저장)");

                OnChartDataLoaded?.Invoke(chartData);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SensorChartViewModel] 차트 로드 실패: {ex.Message}");
                OnError?.Invoke($"차트 로드 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// 캐시 키 생성
        /// </summary>
        private string GetCacheKey(int obsId, int boardId, int hnsId)
        {
            return $"{obsId}_{boardId}_{hnsId}";
        }

        #endregion

        #region 캐시 관리

        /// <summary>
        /// 캐시 무효화 (모든 캐시 삭제)
        /// - 알람 발생 시 호출
        /// </summary>
        public void InvalidateCache()
        {
            int cacheCount = _chartCache.Count;
            _chartCache.Clear();
            Debug.Log($"[SensorChartViewModel] 차트 캐시 무효화 ({cacheCount}개 삭제)");
        }

        /// <summary>
        /// 특정 센서의 캐시만 무효화
        /// </summary>
        public void InvalidateCache(int obsId, int boardId, int hnsId)
        {
            string cacheKey = GetCacheKey(obsId, boardId, hnsId);

            if (_chartCache.ContainsKey(cacheKey))
            {
                _chartCache.Remove(cacheKey);
                Debug.Log($"[SensorChartViewModel] 차트 캐시 무효화: {cacheKey}");
            }
        }

        /// <summary>
        /// 만료된 캐시 정리
        /// </summary>
        public void CleanExpiredCache()
        {
            var expiredKeys = _chartCache
                .Where(kvp => kvp.Value.IsExpired(CACHE_EXPIRY_MINUTES))
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in expiredKeys)
            {
                _chartCache.Remove(key);
            }

            if (expiredKeys.Count > 0)
            {
                Debug.Log($"[SensorChartViewModel] 만료된 캐시 {expiredKeys.Count}개 삭제");
            }
        }

        /// <summary>
        /// 캐시 상태 출력 (디버그용)
        /// </summary>
        [ContextMenu("Print Cache Status")]
        public void PrintCacheStatus()
        {
            Debug.Log("===== SensorChartViewModel Cache Status =====");
            Debug.Log($"총 캐시 수: {_chartCache.Count}개");

            foreach (var kvp in _chartCache)
            {
                var age = (DateTime.Now - kvp.Value.LoadedAt).TotalMinutes;
                Debug.Log($"  - {kvp.Key}: {age:F1}분 전 생성");
            }

            Debug.Log("===========================================");
        }

        #endregion

        #region 차트 데이터 계산

        /// <summary>
        /// 정규화된 차트 값 반환 (0~1 범위)
        /// </summary>
        public List<float> GetNormalizedValues()
        {
            if (currentChartData == null || currentChartData.values.Count == 0)
                return new List<float>();

            float max = currentChartData.maxValue;

            // 최댓값에 +1 여백 추가 (상단 공간 확보)
            float displayMax = max + 1f;

            if (displayMax <= 0)
            {
                return currentChartData.values.Select(_ => 0f).ToList();
            }

            return currentChartData.values.Select(v => v / displayMax).ToList();
        }

        /// <summary>
        /// 세로축 라벨 값 계산 (6개)
        /// </summary>
        public List<float> GetVerticalLabels(int labelCount = 6)
        {
            List<float> labels = new List<float>();

            if (currentChartData == null)
            {
                for (int i = 0; i < labelCount; i++)
                    labels.Add(0);
                return labels;
            }

            // 최댓값에 +1 여백 추가
            float displayMax = currentChartData.maxValue + 1f;

            float interval = displayMax / (labelCount - 1);

            for (int i = 0; i < labelCount; i++)
            {
                labels.Add(interval * i);
            }

            return labels;
        }

        #endregion

        #region Unity 컨텍스트 메뉴 (디버그)

        [ContextMenu("Invalidate All Cache")]
        private void ContextMenu_InvalidateCache()
        {
            InvalidateCache();
        }

        [ContextMenu("Clean Expired Cache")]
        private void ContextMenu_CleanExpiredCache()
        {
            CleanExpiredCache();
        }

        #endregion
    }
}