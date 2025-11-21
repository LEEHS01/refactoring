using System;

namespace HNS.Common.Models
{
    /// <summary>
    /// 지역 데이터 (리팩토링 버전 - 원본 프로젝트와 독립)
    /// </summary>
    [Serializable]
    public class AreaData
    {
        public int areaId;
        public string areaName;
        public AreaType areaType;

        /// <summary>
        /// 지역 타입 (해양시설/발전소)
        /// </summary>
        public enum AreaType
        {
            Ocean = 0,      // 해양시설
            Nuclear = 1     // 발전소
        }

        /// <summary>
        /// 기본 생성자
        /// </summary>
        public AreaData()
        {
            areaId = 0;
            areaName = "";
            areaType = AreaType.Ocean;
        }

        /// <summary>
        /// 매개변수 생성자
        /// </summary>
        public AreaData(int id, string name, AreaType type)
        {
            areaId = id;
            areaName = name;
            areaType = type;
        }
    }
}