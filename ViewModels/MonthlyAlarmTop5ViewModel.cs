using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using System.Linq;
using Onthesys;
using HNS.Services;

/// <summary>
/// 월간 알람 TOP5 ViewModel - MonoBehaviour 버전
/// DataService에서 UnityEvent로 데이터를 받아 View에게 전달
/// </summary>
public class MonthlyAlarmTop5ViewModel : MonoBehaviour
{
    [Header("Runtime Data")]
    [SerializeField] private List<(int areaId, int count)> _top5Data = new List<(int, int)>();
    [SerializeField] private int _totalCount = 0;
    [SerializeField] private string _currentMonth = "";

    [Header("Unity Events - View에서 구독")]
    [Space(10)]

    /// <summary>
    /// TOP5 데이터 변경 시 View에게 알림
    /// </summary>
    public UnityEvent<List<(int, int)>, int> OnTop5DataChanged = new UnityEvent<List<(int, int)>, int>();

    #region Properties

    /// <summary>
    /// 현재 TOP5 데이터
    /// </summary>
    public List<(int areaId, int count)> Top5Data => _top5Data;

    /// <summary>
    /// 총 알람 개수
    /// </summary>
    public int TotalCount => _totalCount;

    /// <summary>
    /// 현재 월
    /// </summary>
    public string CurrentMonth => _currentMonth;

    #endregion

    private void Start()  // OnEnable() → Start()로 변경
    {
        _currentMonth = System.DateTime.Now.ToString("yyyyMM");
        _top5Data.Clear();
        _totalCount = 0;

        Debug.Log("[MonthlyAlarmTop5ViewModel] 초기화 완료");
    }

    /// <summary>
    /// DataService에서 호출되는 메서드 (Inspector에서 UnityEvent로 연결)
    /// 매개변수 없이 DataService에서 직접 데이터를 가져옴
    /// </summary>
    public void UpdateMonthlyData()
    {
        Debug.Log("[MonthlyAlarmTop5ViewModel] 월간 데이터 업데이트 요청");

        // DataService에서 캐시된 데이터 가져오기
        var dataService = FindObjectOfType<DataService>();
        if (dataService == null)
        {
            Debug.LogError("[MonthlyAlarmTop5ViewModel] DataService를 찾을 수 없습니다.");
            return;
        }

        var monthlyData = dataService.CurrentMonthlyData;
        if (monthlyData == null)
        {
            Debug.LogWarning("[MonthlyAlarmTop5ViewModel] 월간 데이터가 null입니다.");
            return;
        }

        Debug.Log($"[MonthlyAlarmTop5ViewModel] 월간 데이터 수신: {monthlyData.Count}개 지역");

        // ModelProvider에서 지역 정보 필요
        var modelProvider = UiManager.Instance?.modelProvider;
        if (modelProvider == null)
        {
            Debug.LogError("[MonthlyAlarmTop5ViewModel] ModelProvider에 접근할 수 없습니다.");
            return;
        }

        // AlarmMontlyModel을 (int, int) 튜플로 변환
        var convertedData = new List<(int areaId, int count)>();

        foreach (var model in monthlyData)
        {
            var area = modelProvider.GetAreaByName(model.areanm);
            if (area != null)
            {
                convertedData.Add((area.areaId, model.cnt));
            }
            else
            {
                Debug.LogWarning($"[MonthlyAlarmTop5ViewModel] 지역을 찾을 수 없습니다: {model.areanm}");
            }
        }

        // TOP5 데이터 생성 (알람 개수 기준 내림차순 정렬)
        _top5Data = convertedData
            .OrderByDescending(x => x.count)
            .Take(5)
            .ToList();

        // 총 개수 계산
        _totalCount = _top5Data.Sum(x => x.count);

        // 현재 월 업데이트
        _currentMonth = System.DateTime.Now.ToString("yyyyMM");

        Debug.Log($"[MonthlyAlarmTop5ViewModel] TOP5 업데이트 완료 - 총 {_totalCount}개 알람");

        // View에게 데이터 변경 알림
        OnTop5DataChanged?.Invoke(_top5Data, _totalCount);
    }

    /// <summary>
    /// 디버그 정보 출력
    /// </summary>
    [ContextMenu("디버그 정보 출력")]
    public void PrintDebugInfo()
    {
        Debug.Log($"[MonthlyAlarmTop5ViewModel] 디버그 정보:" +
                 $"\n- 현재 월: {_currentMonth}" +
                 $"\n- 총 알람: {_totalCount}개" +
                 $"\n- TOP5 데이터: {_top5Data.Count}개");

        for (int i = 0; i < _top5Data.Count; i++)
        {
            var data = _top5Data[i];
            Debug.Log($"  [{i + 1}위] 지역ID: {data.areaId}, 알람: {data.count}회");
        }
    }
}