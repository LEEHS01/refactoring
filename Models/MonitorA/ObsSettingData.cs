using System;

namespace HNS.MonitorA.Models
{
    /// <summary>
    /// 관측소 설정 데이터
    /// </summary>
    [Serializable]
    public class ObsSettingData
    {
        public int ObsId { get; set; }
        public int AreaId { get; set; }
        public string AreaName { get; set; }
        public string ObsName { get; set; }

        // 보드 고정 상태
        public bool ToxinBoardFixed { get; set; }
        public bool ChemicalBoardFixed { get; set; }
        public bool QualityBoardFixed { get; set; }

        // 독성도 경보값
        public float ToxinWarning { get; set; }

        // CCTV URL
        public string CctvEquipmentUrl { get; set; }
        public string CctvOutdoorUrl { get; set; }

        public ObsSettingData()
        {
            ObsId = -1;
            AreaId = -1;
            AreaName = string.Empty;
            ObsName = string.Empty;
            ToxinBoardFixed = false;
            ChemicalBoardFixed = false;
            QualityBoardFixed = false;
            ToxinWarning = 0f;
            CctvEquipmentUrl = string.Empty;
            CctvOutdoorUrl = string.Empty;
        }
    }
}