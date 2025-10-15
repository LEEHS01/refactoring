// Models/MonitorB/AIAnalysisData.cs
using System;
using System.Collections.Generic;

namespace Models.MonitorB
{
    /// <summary>
    /// AI 분석 데이터 (AI값, 측정값, 편차값)
    /// </summary>
    public class AIAnalysisData
    {
        public List<float> AIValues { get; set; }
        public List<float> MeasuredValues { get; set; }
        public List<float> DifferenceValues { get; set; }
        public float WarningThreshold { get; set; }

        // ⭐ 실제 시간 정보 추가
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }

        public AIAnalysisData()
        {
            AIValues = new List<float>();
            MeasuredValues = new List<float>();
            DifferenceValues = new List<float>();
            WarningThreshold = 100f;
            StartTime = DateTime.Now;
            EndTime = DateTime.Now;
        }
    }

    /// <summary>
    /// 처리된 차트 데이터 (정규화, 이상치 처리 완료)
    /// </summary>
    public class ProcessedChartData
    {
        public List<float> ProcessedValues { get; set; }
        public List<int> AnomalousIndices { get; set; }
        public float MaxValue { get; set; }

        public ProcessedChartData()
        {
            ProcessedValues = new List<float>();
            AnomalousIndices = new List<int>();
            MaxValue = 0f;
        }
    }
}