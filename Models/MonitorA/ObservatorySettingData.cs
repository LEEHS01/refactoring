using System;
using System.Collections.Generic;

namespace HNS.Common.Models
{
    /// <summary>
    /// 관측소 설정 데이터
    /// </summary>
    [Serializable]
    public class ObservatorySettingData
    {
        public int ObsId { get; set; }
        public int AreaId { get; set; }
        public string AreaName { get; set; }
        public string ObsName { get; set; }

        // 독성도 설정
        public float ToxinWarningValue { get; set; }
        public bool ToxinBoardFixed { get; set; }

        // 화학물질 보드 설정
        public bool ChemicalBoardFixed { get; set; }
        public List<SensorSettingData> ChemicalSensors { get; set; }

        // 수질 보드 설정
        public bool QualityBoardFixed { get; set; }
        public List<SensorSettingData> QualitySensors { get; set; }

        // CCTV URL
        public string CctvEquipmentUrl { get; set; }
        public string CctvOutdoorUrl { get; set; }

        public ObservatorySettingData()
        {
            ChemicalSensors = new List<SensorSettingData>();
            QualitySensors = new List<SensorSettingData>();
            CctvEquipmentUrl = "";
            CctvOutdoorUrl = "";
        }
    }

    /// <summary>
    /// 센서 설정 데이터
    /// </summary>
    [Serializable]
    public class SensorSettingData
    {
        public int SensorId { get; set; }
        public int BoardId { get; set; }
        public string SensorName { get; set; }
        public bool IsUsing { get; set; }
        public float WarningValue { get; set; }
        public float SeriousValue { get; set; }

        public SensorSettingData()
        {
            SensorName = "";
            IsUsing = false;
            WarningValue = 0f;
            SeriousValue = 0f;
        }
    }

    /// <summary>
    /// 시스템 설정 데이터
    /// </summary>
    [Serializable]
    public class SystemSettingData
    {
        public ToxinStatus AlarmThreshold { get; set; }
        public string DatabaseUrl { get; set; }

        public SystemSettingData()
        {
            AlarmThreshold = ToxinStatus.Yellow;
            DatabaseUrl = "";
        }
    }

    /// <summary>
    /// CCTV 타입
    /// </summary>
    public enum CctvType
    {
        Equipment = 1,  // 설비
        Outdoor = 2     // 실외
    }

    /// <summary>
    /// 독성 상태 (알람 등급)
    /// </summary>
    public enum ToxinStatus
    {
        Green = 0,    // 정상
        Yellow = 1,   // 경계
        Red = 2,      // 경보
        Purple = 3    // 설비이상
    }
}