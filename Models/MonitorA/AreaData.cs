using Onthesys;
using System;

namespace HNS.MonitorA.Models
{
    /// <summary>
    /// 지역 데이터 (레거시 호환)
    /// </summary>
    [Serializable]
    public class AreaData
    {
        // ✅ 레거시와 동일한 필드명 사용
        public int areaId { get; set; }
        public string areaName { get; set; }
        public AreaType areaType { get; set; }

        public AreaData()
        {
            areaId = -1;
            areaName = string.Empty;
            areaType = AreaType.Ocean;
        }

        public AreaData(int areaId, string areaName)
        {
            this.areaId = areaId;
            this.areaName = areaName;
            this.areaType = AreaType.Ocean;
        }

        public AreaData(int areaId, string areaName, AreaType areaType)
        {
            this.areaId = areaId;
            this.areaName = areaName;
            this.areaType = areaType;
        }

        /// <summary>
        /// 지역 타입 (해역/발전소)
        /// </summary>
        public enum AreaType
        {
            Ocean,      // 해역
            Nuclear     // 발전소
        }

        // ✅ 레거시 호환을 위한 정적 메서드 (선택사항)
        public static AreaData FromAreaDataModel(AreaDataModel areaModel) => new()
        {
            areaId = areaModel.areaIdx,
            areaName = areaModel.areaNm,
            areaType = (AreaType)areaModel.areaType,
        };
    }
}