using System;
using UnityEngine;

namespace Models.MonitorB
{
    [Serializable]
    public class SensorInfoData
    {
        // ⭐ JsonUtility용 - 대문자로 매핑
        [SerializeField] private int BOARDIDX;
        [SerializeField] private int HNSIDX;
        [SerializeField] private int OBSIDX;
        [SerializeField] private string HNSNM;
        [SerializeField] private float VAL;
        [SerializeField] private string UNIT;
        [SerializeField] private string INSPECTIONFLAG;

        // ⭐ 프로퍼티로 접근 (소문자)
        public int boardIdx => BOARDIDX;
        public int hnsIdx => HNSIDX;
        public int obsId => OBSIDX;
        public string sensorName => HNSNM;
        public float currentValue => VAL;
        public string unit => UNIT;
        public bool isInspection => INSPECTIONFLAG == "Y";

        /// <summary>
        /// 포맷팅된 값 반환
        /// </summary>
        public string GetFormattedValue()
        {
            if (isInspection)
                return "점검중";

            return currentValue.ToString("F2");
        }
    }
}