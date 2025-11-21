using HNS.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;


namespace Models.MonitorA
{
    /// <summary>
    /// 센서 아이템 데이터 모델
    /// ObsMonitoring에서 사용하는 센서 정보
    /// </summary>
    [Serializable]
    public class SensorItemData
    {
        public int BoardId;         // 보드 ID (1:독성도, 2:화학물질, 3:수질)
        public int HnsId;           // 센서 ID
        public string HnsName;      // 센서명
        public string Unit;         // 단위
        public float Serious;       // 경계 임계값 (hi)
        public float Warning;       // 경보 임계값 (hihi)
        public bool IsActive;       // 활성화 여부 (useyn)
        public bool IsFixing;       // 점검 여부 (fix)
        public ToxinStatus Status;  // 현재 상태
        public float CurrentValue;  // 현재 측정값
        public string StateCode;    // 상태 코드 (00, 20~25)

        // ⭐ 트렌드 차트용 데이터
        public List<float> Values;  // 측정값 리스트 (차트용)

        /// <summary>
        /// 마지막 측정값 반환
        /// </summary>
        public float GetLastValue()
        {
            if (Values != null && Values.Count > 0)
                return Values.Last();
            return CurrentValue;
        }
    }
}