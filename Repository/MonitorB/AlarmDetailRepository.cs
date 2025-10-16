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

        public async Task<AlarmDetailData> GetAlarmDetailAsync(
            int obsId,
            int alarmBoardId,
            int alarmHnsId,
            DateTime alarmTime,
            float? alarmCurrVal,
            string obsName,
            string areaName)
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
                Debug.Log($"🔥 센서 설정 조회 완료: {sensors.Count}개");

                var startTime = alarmTime.AddHours(-12);
                var chartData = await GetChartDataAsync(obsId, startTime, alarmTime);
                Debug.Log($"🔥 차트 데이터 조회 완료: {chartData.Count}개");

                foreach (var sensor in sensors)
                {
                    var sensorData = CreateSensorData(sensor, chartData,
                        alarmBoardId, alarmHnsId, alarmCurrVal);

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

                Debug.Log($"🔥 최종 결과: 생태독성={detailData.ToxinSensors.Count}, 수질={detailData.QualitySensors.Count}, 법정HNS={detailData.ChemicalSensors.Count}");

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
                WarningThreshold = item.ALAHIVAL,
                CriticalThreshold = item.ALAHIHIVAL
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
            DateTime startTime,
            DateTime endTime)
        {
            var query = $@"
        EXEC GET_CHARTVALUE 
            @obsidx = {obsId},
            @start_dt = '{startTime:yyyyMMddHHmmss}',
            @end_dt = '{endTime:yyyyMMddHHmmss}',
            @interval = 10";

            var result = await DatabaseService.Instance.ExecuteQueryAsync(query);

            if (string.IsNullOrEmpty(result))
            {
                Debug.LogWarning($"차트 데이터가 없습니다. ObsId={obsId}");
                return new List<ChartDataPoint>();
            }

            var wrappedJson = "{\"items\":" + result + "}";
            var response = JsonUtility.FromJson<ChartDataResponse>(wrappedJson);

            return response.items.Select(item => new ChartDataPoint
            {
                BoardId = item.BOARDIDX,
                HnsId = item.HNSIDX,
                ObsDt = DateTime.ParseExact(item.OBSDT, "yyyyMMddHHmmss", null),
                Val = item.VAL
            }).ToList();
        }

        private AlarmSensorData CreateSensorData(
            SensorSetting setting,
            List<ChartDataPoint> chartData,
            int alarmBoardId,
            int alarmHnsId,
            float? alarmCurrVal)
        {
            var sensorChartData = chartData
                .Where(d => d.BoardId == setting.BoardId && d.HnsId == setting.HnsId)
                .OrderBy(d => d.ObsDt)
                .ToList();

            var chartValues = sensorChartData.Select(d => d.Val).ToList();
            var chartTimes = sensorChartData.Select(d => d.ObsDt).ToList();

            float currentValue = 0f;
            if (chartValues.Count > 0)
            {
                currentValue = chartValues.Last();

                if (setting.BoardId == alarmBoardId &&
                    setting.HnsId == alarmHnsId &&
                    alarmCurrVal.HasValue)
                {
                    chartValues[chartValues.Count - 1] = alarmCurrVal.Value;
                    currentValue = alarmCurrVal.Value;
                }
            }

            var status = DetermineSensorStatus(currentValue,
                setting.WarningThreshold, setting.CriticalThreshold);

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

        private SensorStatus DetermineSensorStatus(
            float value,
            float warningThreshold,
            float criticalThreshold)
        {
            if (value >= criticalThreshold)
                return SensorStatus.Critical;
            if (value >= warningThreshold)
                return SensorStatus.Warning;
            return SensorStatus.Normal;
        }

        #region 내부 클래스

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

        private class SensorSetting
        {
            public int BoardId { get; set; }
            public int HnsId { get; set; }
            public string SensorName { get; set; }
            public string Unit { get; set; }
            public bool IsActive { get; set; }
            public float WarningThreshold { get; set; }
            public float CriticalThreshold { get; set; }
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