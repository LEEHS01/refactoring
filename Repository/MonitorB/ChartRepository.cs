using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Services;
using Models.MonitorB;

namespace Repositories.MonitorB
{
    public class ChartRepository
    {
        private static ChartRepository _instance;
        public static ChartRepository Instance => _instance ??= new ChartRepository();

        private ChartRepository() { }

        // Repositories/MonitorB/ChartRepository.cs

        public async Task<ChartData> GetSensorChartDataAsync(int obsId, int boardId, int hnsId)
        {
            try
            {
                // 현재 시간을 10분 단위로 내림
                DateTime now = DateTime.Now;
                DateTime endTime = new DateTime(
                    now.Year, now.Month, now.Day,
                    now.Hour,
                    (now.Minute / 10) * 10,  // 10분 단위로 내림 (16:04 → 16:00)
                    0
                );

                DateTime startTime = endTime.AddHours(-12);

                Debug.Log($"[ChartRepository] 차트 데이터 조회: {startTime:yyyy-MM-dd HH:mm:ss} ~ {endTime:yyyy-MM-dd HH:mm:ss}");

                string query = $@"
            EXEC GET_CHARTVALUE 
            @obsidx={obsId}, 
            @start_dt='{startTime:yyyyMMddHHmmss}',
            @end_dt='{endTime:yyyyMMddHHmmss}',
            @interval=10
        ";

                string result = await DatabaseService.Instance.ExecuteQueryAsync(query);

                if (string.IsNullOrEmpty(result))
                {
                    Debug.LogWarning($"[ChartRepository] 쿼리 결과가 비어있습니다.");
                    return CreateEmptyChartData(startTime, endTime);
                }

                Debug.Log($"[ChartRepository] 쿼리 결과 길이: {result.Length}자");

                var wrappedJson = "{\"items\":" + result + "}";
                var response = JsonUtility.FromJson<ChartDataResponse>(wrappedJson);

                if (response?.items == null || response.items.Count == 0)
                {
                    Debug.LogWarning("[ChartRepository] 파싱된 데이터가 없습니다.");
                    return CreateEmptyChartData(startTime, endTime);
                }

                Debug.Log($"[ChartRepository] 전체 데이터: {response.items.Count}개");

                var sensorData = response.items
                    .Where(d => d.boardIdx == boardId && d.hnsIdx == hnsId)
                    .OrderBy(d => d.time)
                    .ToList();

                Debug.Log($"[ChartRepository] Board={boardId}, HNS={hnsId} 필터링 결과: {sensorData.Count}개");

                if (sensorData.Count == 0)
                {
                    Debug.LogWarning($"[ChartRepository] Board={boardId}, HNS={hnsId}에 해당하는 데이터가 없습니다!");
                    return CreateEmptyChartData(startTime, endTime);
                }

                return ConvertToChartData(sensorData, startTime, endTime);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ChartRepository] 차트 데이터 조회 실패: {ex.Message}\n{ex.StackTrace}");
                return CreateEmptyChartData(DateTime.Now.AddHours(-12), DateTime.Now);
            }
        }

        // 시간 라벨 생성 (2시간 간격, 정시로 딱 떨어지게)
        // Repositories/MonitorB/ChartRepository.cs

        private List<DateTime> GenerateTimeLabels(DateTime startTime, DateTime endTime, int labelCount)
        {
            List<DateTime> labels = new List<DateTime>();

            // startTime과 endTime을 그대로 사용하되, 균등 분할
            double totalHours = (endTime - startTime).TotalHours;
            double intervalHours = totalHours / (labelCount - 1);

            for (int i = 0; i < labelCount; i++)
            {
                DateTime label = startTime.AddHours(intervalHours * i);
                labels.Add(label);
            }

            Debug.Log($"[ChartRepository] 시간 라벨 생성: {startTime:HH:mm} ~ {endTime:HH:mm}, 간격 {intervalHours:F2}시간");

            return labels;
        }


        /*private ChartData ConvertToChartData(List<ChartDataPoint> dataPoints, DateTime startTime, DateTime endTime)
        {
            ChartData chartData = new ChartData();
            chartData.startTime = startTime;
            chartData.endTime = endTime;
            chartData.timeLabels = GenerateTimeLabels(startTime, endTime, 7);
            chartData.values = dataPoints.Select(d => d.value).ToList();

            if (chartData.values.Count > 0)
            {
                chartData.maxValue = chartData.values.Max();
                chartData.minValue = chartData.values.Min();
                Debug.Log($"[ChartRepository] 값 범위: {chartData.minValue:F2} ~ {chartData.maxValue:F2}");
            }

            return chartData;
        }*/

        private ChartData ConvertToChartData(List<ChartDataPoint> dataPoints, DateTime startTime, DateTime endTime)
        {
            ChartData chartData = new ChartData();
            chartData.startTime = startTime;
            chartData.endTime = endTime;
            chartData.timeLabels = GenerateTimeLabels(startTime, endTime, 7);
            chartData.values = dataPoints.Select(d => d.value).ToList();

            if (chartData.values.Count > 0)
            {
                chartData.maxValue = chartData.values.Max();
                chartData.minValue = chartData.values.Min();
                Debug.Log($"[ChartRepository] 값 범위: {chartData.minValue:F2} ~ {chartData.maxValue:F2}");
            }

            // 시간 검증
            Debug.Log($"[ChartRepository] 시간 라벨:");
            foreach (var label in chartData.timeLabels)
            {
                Debug.Log($"  - {label:yyyy-MM-dd HH:mm:ss}");
            }

            // 데이터 포인트 시간 검증 (처음 5개, 마지막 5개)
            Debug.Log($"[ChartRepository] 데이터 포인트 샘플:");
            for (int i = 0; i < Math.Min(5, dataPoints.Count); i++)
            {
                Debug.Log($"  [{i}] {dataPoints[i].time:yyyy-MM-dd HH:mm:ss} = {dataPoints[i].value:F2}");
            }
            if (dataPoints.Count > 10)
            {
                Debug.Log("  ...");
                for (int i = Math.Max(0, dataPoints.Count - 5); i < dataPoints.Count; i++)
                {
                    Debug.Log($"  [{i}] {dataPoints[i].time:yyyy-MM-dd HH:mm:ss} = {dataPoints[i].value:F2}");
                }
            }

            return chartData;
        }
        private ChartData CreateEmptyChartData(DateTime startTime, DateTime endTime)
        {
            ChartData chartData = new ChartData();
            chartData.startTime = startTime;
            chartData.endTime = endTime;
            chartData.timeLabels = GenerateTimeLabels(startTime, endTime, 7);
            chartData.values = new List<float>();
            return chartData;
        }

        [Serializable]
        private class ChartDataResponse
        {
            public List<ChartDataPoint> items;
        }
    }
}