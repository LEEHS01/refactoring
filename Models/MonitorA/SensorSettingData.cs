using System;

namespace HNS.MonitorA.Models
{
    /// <summary>
    /// 센서 설정 데이터
    /// </summary>
    [Serializable]
    public class SensorSettingData
    {
        public int ObsId { get; set; }
        public int BoardId { get; set; }
        public int HnsId { get; set; }
        public string HnsName { get; set; }
        public bool IsUsing { get; set; }

        public bool IsFixed { get; set; }
        public float Serious { get; set; }
        public float Warning { get; set; }

        public SensorSettingData()
        {
            ObsId = -1;
            BoardId = -1;
            HnsId = -1;
            HnsName = string.Empty;
            IsUsing = false;
            Serious = 0f;
            Warning = 0f;
        }

        public SensorSettingData(int obsId, int boardId, int hnsId, string hnsName, bool isUsing, float serious, float warning)
        {
            ObsId = obsId;
            BoardId = boardId;
            HnsId = hnsId;
            HnsName = hnsName;
            IsUsing = isUsing;
            Serious = serious;
            Warning = warning;
        }
    }
}