using System;

namespace HNS.Common.Models
{
    /// <summary>
    /// GET_SETTING 프로시저 결과 모델
    /// </summary>
    [Serializable]
    public class SettingModel
    {
        public int OBSIDX { get; set; }
        public int HNSIDX { get; set; }
        public int BOARDIDX { get; set; }
        public string HNSNM { get; set; }
        public float? ALAHIVAL { get; set; }
        public float? ALAHIHIVAL { get; set; }
        public string USEYN { get; set; }
        public float? ALAHIHISEC { get; set; }
        public string INSPECTIONFLAG { get; set; }
        public string UNIT { get; set; }
    }

    /// <summary>
    /// 지역 모델 (프로시저 결과)
    /// </summary>
    [Serializable]
    public class AreaModel
    {
        public int AREAIDX { get; set; }
        public string AREANM { get; set; }
        public int AREATYPE { get; set; }
    }

    /// <summary>
    /// 관측소 모델 (프로시저 결과)
    /// </summary>
    [Serializable]
    public class ObservatoryModel
    {
        public int OBSIDX { get; set; }
        public string OBSNM { get; set; }
        public int AREAIDX { get; set; }
    }

    /// <summary>
    /// 빈 모델 (UPDATE 프로시저용)
    /// </summary>
    [Serializable]
    public class EmptyModel
    {
    }

    /// <summary>
    /// 관측소 데이터 (간단 버전)
    /// </summary>
    [Serializable]
    public class ObservatoryData
    {
        public int ObsId { get; set; }
        public string ObsName { get; set; }
        public int AreaId { get; set; }
    }
}