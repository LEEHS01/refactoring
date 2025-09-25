using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using System.Linq;
using Onthesys;
using HNS.Services;

/// <summary>
/// ���� �˶� TOP5 ViewModel - MonoBehaviour ����
/// DataService���� UnityEvent�� �����͸� �޾� View���� ����
/// </summary>
public class MonthlyAlarmTop5ViewModel : MonoBehaviour
{
    [Header("Runtime Data")]
    [SerializeField] private List<(int areaId, int count)> _top5Data = new List<(int, int)>();
    [SerializeField] private int _totalCount = 0;
    [SerializeField] private string _currentMonth = "";

    [Header("Unity Events - View���� ����")]
    [Space(10)]

    /// <summary>
    /// TOP5 ������ ���� �� View���� �˸�
    /// </summary>
    public UnityEvent<List<(int, int)>, int> OnTop5DataChanged = new UnityEvent<List<(int, int)>, int>();

    #region Properties

    /// <summary>
    /// ���� TOP5 ������
    /// </summary>
    public List<(int areaId, int count)> Top5Data => _top5Data;

    /// <summary>
    /// �� �˶� ����
    /// </summary>
    public int TotalCount => _totalCount;

    /// <summary>
    /// ���� ��
    /// </summary>
    public string CurrentMonth => _currentMonth;

    #endregion

    private void Start()  // OnEnable() �� Start()�� ����
    {
        _currentMonth = System.DateTime.Now.ToString("yyyyMM");
        _top5Data.Clear();
        _totalCount = 0;

        Debug.Log("[MonthlyAlarmTop5ViewModel] �ʱ�ȭ �Ϸ�");
    }

    /// <summary>
    /// DataService���� ȣ��Ǵ� �޼��� (Inspector���� UnityEvent�� ����)
    /// �Ű����� ���� DataService���� ���� �����͸� ������
    /// </summary>
    public void UpdateMonthlyData()
    {
        Debug.Log("[MonthlyAlarmTop5ViewModel] ���� ������ ������Ʈ ��û");

        // DataService���� ĳ�õ� ������ ��������
        var dataService = FindObjectOfType<DataService>();
        if (dataService == null)
        {
            Debug.LogError("[MonthlyAlarmTop5ViewModel] DataService�� ã�� �� �����ϴ�.");
            return;
        }

        var monthlyData = dataService.CurrentMonthlyData;
        if (monthlyData == null)
        {
            Debug.LogWarning("[MonthlyAlarmTop5ViewModel] ���� �����Ͱ� null�Դϴ�.");
            return;
        }

        Debug.Log($"[MonthlyAlarmTop5ViewModel] ���� ������ ����: {monthlyData.Count}�� ����");

        // ModelProvider���� ���� ���� �ʿ�
        var modelProvider = UiManager.Instance?.modelProvider;
        if (modelProvider == null)
        {
            Debug.LogError("[MonthlyAlarmTop5ViewModel] ModelProvider�� ������ �� �����ϴ�.");
            return;
        }

        // AlarmMontlyModel�� (int, int) Ʃ�÷� ��ȯ
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
                Debug.LogWarning($"[MonthlyAlarmTop5ViewModel] ������ ã�� �� �����ϴ�: {model.areanm}");
            }
        }

        // TOP5 ������ ���� (�˶� ���� ���� �������� ����)
        _top5Data = convertedData
            .OrderByDescending(x => x.count)
            .Take(5)
            .ToList();

        // �� ���� ���
        _totalCount = _top5Data.Sum(x => x.count);

        // ���� �� ������Ʈ
        _currentMonth = System.DateTime.Now.ToString("yyyyMM");

        Debug.Log($"[MonthlyAlarmTop5ViewModel] TOP5 ������Ʈ �Ϸ� - �� {_totalCount}�� �˶�");

        // View���� ������ ���� �˸�
        OnTop5DataChanged?.Invoke(_top5Data, _totalCount);
    }

    /// <summary>
    /// ����� ���� ���
    /// </summary>
    [ContextMenu("����� ���� ���")]
    public void PrintDebugInfo()
    {
        Debug.Log($"[MonthlyAlarmTop5ViewModel] ����� ����:" +
                 $"\n- ���� ��: {_currentMonth}" +
                 $"\n- �� �˶�: {_totalCount}��" +
                 $"\n- TOP5 ������: {_top5Data.Count}��");

        for (int i = 0; i < _top5Data.Count; i++)
        {
            var data = _top5Data[i];
            Debug.Log($"  [{i + 1}��] ����ID: {data.areaId}, �˶�: {data.count}ȸ");
        }
    }
}