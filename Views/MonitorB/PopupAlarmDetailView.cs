﻿using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ViewModels.MonitorB;
using Models.MonitorB;
using System.Collections.Generic;
using System.Linq;

namespace Views.MonitorB
{
    public class PopupAlarmDetailView : MonoBehaviour
    {
        [Header("UI Components")]
        [SerializeField] private TMP_Text txtTitle;
        [SerializeField] private TMP_Text txtDate;
        [SerializeField] private TMP_Text txtTime;
        [SerializeField] private Button btnClose;

        [Header("Sensor Containers")]
        [SerializeField] private Transform lstToxin;
        [SerializeField] private Transform lstQuality;
        [SerializeField] private Transform lstChemical;

        private void Start()
        {
            if (btnClose != null)
            {
                btnClose.onClick.AddListener(ClosePopup);
            }

            ConnectToViewModel();
            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            DisconnectFromViewModel();

            if (btnClose != null)
            {
                btnClose.onClick.RemoveListener(ClosePopup);
            }
        }

        private void ConnectToViewModel()
        {
            if (AlarmDetailViewModel.Instance != null)
            {
                AlarmDetailViewModel.Instance.OnDataLoaded += OnDataLoaded;
                AlarmDetailViewModel.Instance.OnError += OnError;
            }
        }

        private void DisconnectFromViewModel()
        {
            if (AlarmDetailViewModel.Instance != null)
            {
                AlarmDetailViewModel.Instance.OnDataLoaded -= OnDataLoaded;
                AlarmDetailViewModel.Instance.OnError -= OnError;
            }
        }

        public void OpenPopup(
            int obsId,
            int alarmBoardId,
            int alarmHnsId,
            System.DateTime alarmTime,
            float? alarmCurrVal,
            string obsName,
            string areaName)
        {
            gameObject.SetActive(true);

            if (txtTitle != null)
                txtTitle.text = $"{areaName} - {obsName}";

            if (txtDate != null)
                txtDate.text = alarmTime.ToString("yyyy.MM.dd");

            if (txtTime != null)
                txtTime.text = alarmTime.ToString("HH:mm:ss");

            AlarmDetailViewModel.Instance?.LoadAlarmDetail(
                obsId, alarmBoardId, alarmHnsId, alarmTime, alarmCurrVal, obsName, areaName);
        }

        private void OnDataLoaded(AlarmDetailData data)
        {
            Debug.Log($"🔥 OnDataLoaded 호출됨!");

            var toxinSensors = data.ToxinSensors.Where(s => s.IsActive).ToList();
            var qualitySensors = data.QualitySensors.Where(s => s.IsActive).ToList();
            var chemicalSensors = data.ChemicalSensors.Where(s => s.IsActive).ToList();

            Debug.Log($"🔥 생태독성: {toxinSensors.Count}개");
            Debug.Log($"🔥 수질: {qualitySensors.Count}개");
            Debug.Log($"🔥 법정HNS: {chemicalSensors.Count}개");

            FillExistingItems(toxinSensors, lstToxin);
            FillExistingItems(qualitySensors, lstQuality);
            FillExistingItems(chemicalSensors, lstChemical);

            Canvas.ForceUpdateCanvases();

            if (lstToxin != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(lstToxin as RectTransform);
            if (lstQuality != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(lstQuality as RectTransform);
            if (lstChemical != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(lstChemical as RectTransform);

            var content = lstToxin?.parent;
            if (content != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(content as RectTransform);
        }

        private void FillExistingItems(List<AlarmSensorData> sensors, Transform container)
        {
            if (container == null)
            {
                Debug.LogError($"❌ Container가 null!");
                return;
            }

            var items = new List<PopupAlarmDetailItemView>();

            for (int i = 0; i < container.childCount; i++)
            {
                var child = container.GetChild(i);
                var itemView = child.GetComponent<PopupAlarmDetailItemView>();

                if (itemView != null)
                {
                    items.Add(itemView);
                }
            }

            Debug.Log($"🔥 {container.name}: 기존 아이템 {items.Count}개, 센서 데이터 {sensors.Count}개");

            for (int i = 0; i < items.Count; i++)
            {
                if (i < sensors.Count)
                {
                    items[i].gameObject.SetActive(true);
                    items[i].SetData(sensors[i]);
                    Debug.Log($"  ✅ [{i}] {sensors[i].SensorName} 데이터 설정");
                }
                else
                {
                    items[i].gameObject.SetActive(false);
                    Debug.Log($"  ⏭️ [{i}] 아이템 비활성화");
                }
            }

            if (sensors.Count > items.Count)
            {
                Debug.LogWarning($"⚠️ {container.name}: 센서 {sensors.Count}개인데 아이템은 {items.Count}개! 부족!");
            }
        }

        private void OnError(string error)
        {
            Debug.LogError($"알람 상세 정보 로드 실패: {error}");
        }

        public void ClosePopup()
        {
            gameObject.SetActive(false);
        }
    }
}