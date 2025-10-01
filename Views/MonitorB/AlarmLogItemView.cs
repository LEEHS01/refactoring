using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using Models;

public class AlarmLogItemView : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private TMP_Text _txtTime;        // 날짜
    [SerializeField] private TMP_Text _txtContent;     // 내용 (센서명)
    [SerializeField] private TMP_Text _txtArea;        // 지역
    [SerializeField] private TMP_Text _txtObs;         // 관측소
    [SerializeField] private TMP_Text _txtStatus;      // 상태 (경보/경계/설비이상)
    [SerializeField] private Button _btnItem;          // 클릭 버튼 (선택사항)

    private AlarmLogData _alarmData;
    public event Action<AlarmLogData> OnItemClicked;

    private void Start()
    {
        if (_btnItem != null)
        {
            _btnItem.onClick.AddListener(OnClick);
        }
    }

    public void SetData(AlarmLogData alarmData)
    {
        _alarmData = alarmData;

        // 날짜
        if (_txtTime != null)
            _txtTime.text = alarmData.time.ToString("yyyy-MM-dd HH:mm");

        // 내용 (센서명)
        if (_txtContent != null)
            _txtContent.text = alarmData.sensorName;

        // 지역
        if (_txtArea != null)
            _txtArea.text = alarmData.areaName;

        // 관측소
        if (_txtObs != null)
            _txtObs.text = alarmData.obsName;

        // 상태 텍스트만 표시 (색상 없음)
        if (_txtStatus != null)
        {
            _txtStatus.text = GetStatusText(alarmData.status);
        }
    }

    private string GetStatusText(int statusCode)
    {
        return statusCode switch
        {
            2 => "경보",
            1 => "경계",
            0 => "설비이상",
            _ => "알수없음"
        };
    }

    private void OnClick()
    {
        OnItemClicked?.Invoke(_alarmData);
        Debug.Log($"알람 아이템 클릭: {_alarmData.obsName} - {_alarmData.sensorName}");
    }
}