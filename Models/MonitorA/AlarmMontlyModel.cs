using System;

/// <summary>
/// 월간 알람 모델 (기존 프로젝트와 동일)
/// DB에서 가져오는 월간 알람 데이터 구조
/// </summary>
[Serializable]
public class AlarmMontlyModel
{
    /// <summary>
    /// 지역명
    /// </summary>
    public string areanm;

    /// <summary>
    /// 알람 개수
    /// </summary>
    public int cnt;

    /// <summary>
    /// 기본 생성자
    /// </summary>
    public AlarmMontlyModel()
    {
        areanm = "";
        cnt = 0;
    }

    /// <summary>
    /// 매개변수 생성자
    /// </summary>
    public AlarmMontlyModel(string areaName, int count)
    {
        areanm = areaName;
        cnt = count;
    }

    /// <summary>
    /// 디버깅용 문자열 표현
    /// </summary>
    public override string ToString()
    {
        return $"[AlarmMontlyModel] {areanm}: {cnt}회";
    }
}