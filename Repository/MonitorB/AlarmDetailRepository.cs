using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Models.MonitorB;
using Newtonsoft.Json;
using Services;
using UnityEngine;

namespace Repositories.MonitorB
{
    public class AlarmDetailRepository
    {
        private static AlarmDetailRepository _instance;
        public static AlarmDetailRepository Instance => _instance ??= new AlarmDetailRepository();

        private AlarmDetailRepository() { }

        /// <summary>
        /// 알람 상세 데이터 조회
        /// ⭐⭐⭐ 알람 시점의 임계값을 받아서 사용
        /// </summary>
        public async Task<AlarmDetailData> GetAlarmDetailAsync(
            int obsId,
            int alarmBoardId,
            int alarmHnsId,
            DateTime alarmTime,
            float? alarmCurrVal,
            string obsName,
            string areaName,
            float? alarmWarningThreshold,   // ⭐ 추가: 알람 발생 시점의 경계 임계값
            float? alarmCriticalThreshold)  // ⭐ 추가: 알람 발생 시점의 경보 임계값
        {
            try
            {
                var detailData = new AlarmDetailData
                {
                    AlarmTime = alarmTime,
                    ObsId = obsId,
                    ObsName = obsName,
                    AreaName = areaName,
                    AlarmBoardId = alarmBoardId,
                    AlarmHnsId = alarmHnsId,
                    AlarmCurrVal = alarmCurrVal
                };

                var sensors = await GetSensorSettingsAsync(obsId);
                Debug.Log($"센서 설정 조회 완료: {sensors.Count}개");

                var chartData = await GetChartDataAsync(obsId, alarmTime);
                Debug.Log($"10분 단위 데이터 조회 완료: {chartData.Count}개");

                foreach (var sensor in sensors)
                {
                    // ⭐⭐⭐ 알람 발생 센서인 경우, 저장된 임계값 사용
                    float? warningThreshold = sensor.WarningThreshold;
                    float? criticalThreshold = sensor.CriticalThreshold;

                    if (sensor.BoardId == alarmBoardId && sensor.HnsId == alarmHnsId)
                    {
                        // 알람 로그에 저장된 임계값 사용
                        warningThreshold = alarmWarningThreshold;
                        criticalThreshold = alarmCriticalThreshold;

                        Debug.Log($"⭐ 알람 센서 {sensor.SensorName}: 저장된 임계값 사용 (Warning={warningThreshold}, Critical={criticalThreshold})");
                    }

                    var sensorData = CreateSensorData(
                        sensor,
                        chartData,
                        alarmBoardId,
                        alarmHnsId,
                        alarmCurrVal,
                        warningThreshold,   // ⭐ 알람 시점 임계값
                        criticalThreshold); // ⭐ 알람 시점 임계값

                    switch (sensor.BoardId)
                    {
                        case 1:
                            detailData.ToxinSensors.Add(sensorData);
                            break;
                        case 3:
                            detailData.QualitySensors.Add(sensorData);
                            break;
                        case 2:
                            detailData.ChemicalSensors.Add(sensorData);
                            break;
                    }
                }

                Debug.Log($"최종 결과: 생태독성={detailData.ToxinSensors.Count}, 수질={detailData.QualitySensors.Count}, 법정HNS={detailData.ChemicalSensors.Count}");

                return detailData;
            }
            catch (Exception ex)
            {
                Debug.LogError($"AlarmDetailRepository.GetAlarmDetailAsync 실패: {ex.Message}");
                throw;
            }
        }

        private async Task<List<SensorSetting>> GetSensorSettingsAsync(int obsId)
        {
            var query = $"EXEC GET_SETTING @obsidx = {obsId};";
            var result = await DatabaseService.Instance.ExecuteQueryAsync(query);

            if (string.IsNullOrEmpty(result))
            {
                Debug.LogWarning($"센서 설정 정보가 없습니다. ObsId={obsId}");
                return new List<SensorSetting>();
            }

            var wrappedJson = "{\"items\":" + result + "}";
            var response = JsonUtility.FromJson<SensorSettingResponse>(wrappedJson);

            var settings = response.items.Select(item => new SensorSetting
            {
                BoardId = item.BOARDIDX,
                HnsId = item.HNSIDX,
                SensorName = item.HNSNM,
                Unit = item.UNIT,
                IsActive = item.USEYN?.Trim() == "1",
                WarningThreshold = item.ALAHIVAL == 0 ? null : (float?)item.ALAHIVAL,
                CriticalThreshold = item.ALAHIHIVAL == 0 ? null : (float?)item.ALAHIHIVAL
            }).ToList();

            var filteredSettings = settings.Where(s =>
                !(s.BoardId == 2 && s.HnsId > 19) &&
                !(s.BoardId == 3 && s.HnsId > 7)
            ).ToList();

            Debug.Log($"필터링 전: {settings.Count}개, 필터링 후: {filteredSettings.Count}개");

            return filteredSettings;
        }

        private async Task<List<ChartDataPoint>> GetChartDataAsync(
            int obsId,
            DateTime alarmTime)
        {
            var endTime = new DateTime(
                alarmTime.Year,
                alarmTime.Month,
                alarmTime.Day,
                alarmTime.Hour,
                (alarmTime.Minute / 10) * 10,
                0);

            var startTime = endTime.AddHours(-12);

            var query = $@"
        SELECT 
            BOARDIDX,
            HNSIDX,
            OBSDT,
            ISNULL(VAL, 0) AS VAL
        FROM TB_HNS_DATA
        WHERE OBSIDX = {obsId}
          AND OBSDT >= '{startTime:yyyyMMddHHmmss}'
          AND OBSDT <= '{endTime:yyyyMMddHHmmss}'
        ORDER BY OBSDT ASC";

            Debug.Log($"🔍 차트 데이터 조회: {startTime:yyyy-MM-dd HH:mm} ~ {endTime:yyyy-MM-dd HH:mm}");

            var result = await DatabaseService.Instance.ExecuteQueryAsync(query);

            if (string.IsNullOrEmpty(result))
            {
                Debug.LogWarning($"⚠️ 차트 데이터가 없습니다. ObsId={obsId}");
                return new List<ChartDataPoint>();
            }

            var wrappedJson = "{\"items\":" + result + "}";
            var response = JsonUtility.FromJson<ChartDataResponse>(wrappedJson);

            var dataPoints = response.items.Select(item => new ChartDataPoint
            {
                BoardId = item.BOARDIDX,
                HnsId = item.HNSIDX,
                ObsDt = DateTime.ParseExact(item.OBSDT, "yyyyMMddHHmmss", null),
                Val = item.VAL
            }).ToList();

            Debug.Log($"✅ 조회된 차트 데이터: {dataPoints.Count}개");
            return dataPoints;
        }

        /// <summary>
        /// 센서 데이터 생성
        /// ⭐⭐⭐ 임계값을 매개변수로 받아 사용
        /// </summary>
        private AlarmSensorData CreateSensorData(
            SensorSetting setting,
            List<ChartDataPoint> chartData,
            int alarmBoardId,
            int alarmHnsId,
            float? alarmCurrVal,
            float? warningThreshold,   // ⭐ 매개변수로 변경
            float? criticalThreshold)  // ⭐ 매개변수로 변경
        {
            var sensorChartData = chartData
                .Where(d => d.BoardId == setting.BoardId && d.HnsId == setting.HnsId)
                .OrderBy(d => d.ObsDt)
                .ToList();

            Debug.Log($"{setting.SensorName}: 차트 데이터 {sensorChartData.Count}개");

            var chartValues = sensorChartData.Select(d => d.Val).ToList();
            var chartTimes = sensorChartData.Select(d => d.ObsDt).ToList();

            float currentValue = 0f;

            if (setting.BoardId == alarmBoardId &&
                setting.HnsId == alarmHnsId &&
                alarmCurrVal.HasValue)
            {
                currentValue = alarmCurrVal.Value;
                Debug.Log($"⭐ 알람 센서 {setting.SensorName}: CURRVAL={currentValue}");
            }
            else if (chartValues.Count > 0)
            {
                currentValue = chartValues.Last();
                Debug.Log($"{setting.SensorName}: 차트값={currentValue}");
            }
            else
            {
                Debug.LogWarning($"{setting.SensorName}: 데이터 없음");
            }

            var status = DetermineSensorStatus(
                setting.BoardId,
                setting.HnsId,
                currentValue,
                warningThreshold,   // ⭐ 알람 시점 임계값 사용
                criticalThreshold); // ⭐ 알람 시점 임계값 사용

            return new AlarmSensorData
            {
                BoardId = setting.BoardId,
                HnsId = setting.HnsId,
                SensorName = setting.SensorName,
                Unit = setting.Unit,
                CurrentValue = currentValue,
                Status = status,
                IsActive = setting.IsActive,
                ChartValues = chartValues,
                ChartTimes = chartTimes
            };
        }

        /// <summary>
        /// 센서 상태 판정
        /// ⭐ 독성도(BoardId=1): 0 초과 시 무조건 경계 (임계값 무시)
        /// ⭐ 다른 센서: 임계값 기준 판정
        /// </summary>
        private SensorStatus DetermineSensorStatus(
            int boardId,
            int hnsId,
            float value,
            float? warningThreshold,
            float? criticalThreshold)
        {
            // ⭐⭐⭐ 독성도 특수 처리: 임계값 무시하고 0 초과만 체크
            if (boardId == 1)
            {
                return value > 0 ? SensorStatus.Warning : SensorStatus.Normal;
            }

            // 일반 센서 로직 (수질, 법정HNS)
            if (criticalThreshold.HasValue && value >= criticalThreshold.Value)
                return SensorStatus.Critical;
            if (warningThreshold.HasValue && value >= warningThreshold.Value)
                return SensorStatus.Warning;
            return SensorStatus.Normal;
        }

        #region 내부 클래스 (Repository 전용 DTO)

        /// <summary>
        /// DB JSON 파싱용 (외부 노출 불필요)
        /// </summary>
        [Serializable]
        private class SensorSettingResponse
        {
            public List<SensorSettingItem> items;
        }

        [Serializable]
        private class SensorSettingItem
        {
            public int BOARDIDX;
            public int HNSIDX;
            public string HNSNM;
            public string UNIT;
            public string USEYN;
            public float ALAHIVAL;
            public float ALAHIHIVAL;
        }

        /// <summary>
        /// 내부 변환용 (외부 노출 불필요)
        /// </summary>
        private class SensorSetting
        {
            public int BoardId { get; set; }
            public int HnsId { get; set; }
            public string SensorName { get; set; }
            public string Unit { get; set; }
            public bool IsActive { get; set; }
            public float? WarningThreshold { get; set; }
            public float? CriticalThreshold { get; set; }
        }

        [Serializable]
        private class ChartDataResponse
        {
            public List<ChartDataItem> items;
        }

        [Serializable]
        private class ChartDataItem
        {
            public int BOARDIDX;
            public int HNSIDX;
            public string OBSDT;
            public float VAL;
        }

        private class ChartDataPoint
        {
            public int BoardId { get; set; }
            public int HnsId { get; set; }
            public DateTime ObsDt { get; set; }
            public float Val { get; set; }
        }

        #endregion
    }
}