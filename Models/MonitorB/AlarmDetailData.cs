using System;
using System.Collections.Generic;

namespace Models.MonitorB
{
    /// <summary>
    /// 알람 상세 정보 - 특정 알람 시점의 센서 데이터
    /// </summary>
    public class AlarmDetailData
    {
        public DateTime AlarmTime { get; set; }
        public int ObsId { get; set; }
        public string ObsName { get; set; }
        public string AreaName { get; set; }

        // 알람 발생 센서 정보
        public int AlarmBoardId { get; set; }
        public int AlarmHnsId { get; set; }
        public float? AlarmCurrVal { get; set; }  // TB_ALARM_DATA.CURRVAL

        // 센서별 데이터
        public List<AlarmSensorData> ToxinSensors { get; set; } = new();     // 생태독성
        public List<AlarmSensorData> QualitySensors { get; set; } = new();   // 수질
        public List<AlarmSensorData> ChemicalSensors { get; set; } = new();  // 법정hns
    }

    /// <summary>
    /// 개별 센서 데이터 (알람 시점)
    /// </summary>
    public class AlarmSensorData
    {
        public int BoardId { get; set; }
        public int HnsId { get; set; }
        public string SensorName { get; set; }
        public string Unit { get; set; }
        public float CurrentValue { get; set; }      // 알람 시점의 값
        public SensorStatus Status { get; set; }
        public bool IsActive { get; set; }

        // 미니 차트 데이터 (12시간)
        public List<float> ChartValues { get; set; } = new();
        public List<DateTime> ChartTimes { get; set; } = new();
    }

    public enum SensorStatus
    {
        Normal = 0,    // 정상 (Green)
        Warning = 1,   // 경계 (Yellow)
        Critical = 2,  // 경보 (Red)
        Error = 3      // 설비이상 (Purple)
    }
}