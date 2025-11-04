using System;
using System.Collections.Generic;
using UnityEngine;

namespace Models.MonitorB
{
    [Serializable]
    public class ChartData
    {
        public DateTime startTime;
        public DateTime endTime;
        public List<DateTime> timeLabels;  // 7개 시간 라벨
        public List<DateTime> allTimes;    //  모든 포인트의 시간 (72개)
        public List<float> values;         // 센서 측정값들
        public float maxValue;             // 최댓값
        public float minValue;             // 최솟값

        public ChartData()
        {
            timeLabels = new List<DateTime>();
            allTimes = new List<DateTime>();
            values = new List<float>();
            maxValue = 0f;
            minValue = 0f;
        }
    }
}