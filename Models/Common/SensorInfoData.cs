using System;
using UnityEngine;

namespace Models.MonitorB
{
    [Serializable]
    public class SensorInfoData
    {
        // ⭐ JsonUtility용 - nullable 제거!
        [SerializeField] public int BOARDIDX;
        [SerializeField] public int HNSIDX;
        [SerializeField] public int OBSIDX;
        [SerializeField] public string HNSNM;
        [SerializeField] public float VAL;  // ⭐ nullable 제거 (기본값 0)
        [SerializeField] public string UNIT;
        [SerializeField] public string USEYN;
        [SerializeField] public string INSPECTIONFLAG;
        [SerializeField] public float HIHI;
        [SerializeField] public float HI;

        // ⭐ 프로퍼티 (하위 호환성 + camelCase 접근)
        public int boardIdx => BOARDIDX;
        public int hnsIdx => HNSIDX;
        public int obsId => OBSIDX;
        public string sensorName => HNSNM ?? "";
        public float currentValue => VAL;
        public string unit => UNIT ?? "";
        public bool isActive => USEYN?.Trim() == "1";
        public bool isInspection => INSPECTIONFLAG?.Trim() == "1";
        public float criticalThreshold => HIHI;
        public float warningThreshold => HI;

        /// <summary>
        /// 센서 값이 유효한지 체크 (0이 아니고, 점검 중이 아님)
        /// </summary>
        public bool HasValidValue()
        {
            return !isInspection && VAL != 0;
        }

        /// <summary>
        /// 포맷팅된 값 반환
        /// </summary>
        public string GetFormattedValue()
        {
            if (isInspection)
                return "점검중";

            if (!isActive)
                return "N/A";

            return VAL.ToString("F2");
        }
    }
}