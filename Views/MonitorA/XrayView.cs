using System;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using HNS.MonitorA.ViewModels;

namespace HNS.MonitorA.Views
{
    /// <summary>
    /// X-Ray 기능 3D 오브젝트 제어 View
    /// </summary>
    public class XrayView : MonoBehaviour
    {
        [Header("3D Object References")]
        private GameObject outerWall;       // 외벽 구조물
        private GameObject equipments;      // 내부 장비
        private Transform sensorsParent;    // 센서 부모 Transform
        private GameObject equipmentCanvas; // ⭐⭐⭐ 장비 Canvas 추가

        private bool _isSubscribed = false;

        #region Unity Lifecycle

        private void Start()
        {
            FindXrayTargets();
            SubscribeToViewModel();
            UpdateAllVisibility();
        }

        private void OnDestroy()
        {
            UnsubscribeFromViewModel();
        }

        #endregion

        #region ViewModel 구독

        private void SubscribeToViewModel()
        {
            if (_isSubscribed) return;

            if (XrayViewModel.Instance == null)
            {
                Debug.LogError("[XrayView] XrayViewModel.Instance가 null입니다!");
                return;
            }

            XrayViewModel.Instance.OnStructureXrayChanged.AddListener(OnStructureXrayChanged);
            XrayViewModel.Instance.OnEquipmentXrayChanged.AddListener(OnEquipmentXrayChanged);
            XrayViewModel.Instance.OnSensorsVisibilityChanged.AddListener(OnSensorsVisibilityChanged);

            _isSubscribed = true;
            Debug.Log("[XrayView] ✅ ViewModel 이벤트 구독 완료");
        }

        private void UnsubscribeFromViewModel()
        {
            if (!_isSubscribed) return;

            if (XrayViewModel.Instance != null)
            {
                XrayViewModel.Instance.OnStructureXrayChanged.RemoveListener(OnStructureXrayChanged);
                XrayViewModel.Instance.OnEquipmentXrayChanged.RemoveListener(OnEquipmentXrayChanged);
                XrayViewModel.Instance.OnSensorsVisibilityChanged.RemoveListener(OnSensorsVisibilityChanged);
            }

            _isSubscribed = false;
        }

        #endregion

        #region 3D 오브젝트 찾기

        private void FindXrayTargets()
        {
            try
            {
                Scene currentScene = SceneManager.GetActiveScene();
                GameObject[] rootObjects = currentScene.GetRootGameObjects();
                GameObject area3D = rootObjects.FirstOrDefault(go => go.name == "Area_3D");

                if (area3D == null)
                {
                    Debug.LogError("[XrayView] Area_3D를 찾을 수 없습니다!");
                    return;
                }

                Transform observatory = area3D.transform.Find("Observatory");
                if (observatory == null)
                {
                    Debug.LogError("[XrayView] Observatory를 찾을 수 없습니다!");
                    return;
                }

                // OuterWall, Equipments, Sensors 찾기
                outerWall = observatory.Find("OuterWall")?.gameObject;
                equipments = observatory.Find("Equipments")?.gameObject;
                sensorsParent = observatory.Find("Sensors");

                // ⭐⭐⭐ Equipments 하위의 Canvas 찾기
                if (equipments != null)
                {
                    Transform canvasTransform = equipments.transform.Find("Canvas");
                    if (canvasTransform != null)
                    {
                        equipmentCanvas = canvasTransform.gameObject;
                        Debug.Log("[XrayView] ✅ Equipment Canvas 찾음");

                        // ⭐⭐⭐ 초기 상태: Canvas 숨김
                        equipmentCanvas.SetActive(false);
                    }
                    else
                    {
                        Debug.LogWarning("[XrayView] Equipments/Canvas를 찾을 수 없습니다!");
                    }
                }

                if (outerWall == null) Debug.LogWarning("[XrayView] OuterWall을 찾을 수 없습니다!");
                if (equipments == null) Debug.LogWarning("[XrayView] Equipments를 찾을 수 없습니다!");
                if (sensorsParent == null) Debug.LogWarning("[XrayView] Sensors를 찾을 수 없습니다!");

                Debug.Log("[XrayView] ✅ X-Ray 대상 오브젝트 찾기 완료");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[XrayView] X-Ray 대상 오브젝트 찾기 실패: {ex.Message}");
                Debug.LogException(ex);
            }
        }

        #endregion

        #region ViewModel 이벤트 핸들러

        /// <summary>
        /// 건물 X-Ray 상태 변경 (외벽 표시/숨김)
        /// </summary>
        private void OnStructureXrayChanged(bool isXrayActive)
        {
            if (outerWall != null)
            {
                // X-Ray 활성화 = 외벽 숨김 (내부가 보임)
                outerWall.SetActive(!isXrayActive);
                Debug.Log($"[XrayView] 외벽 표시: {!isXrayActive}");
            }

            // ⭐⭐⭐ 건물 X-Ray 활성화 시 Canvas도 표시
            if (equipmentCanvas != null)
            {
                // 건물 X-Ray가 켜지면 Canvas 표시
                equipmentCanvas.SetActive(isXrayActive);
                Debug.Log($"[XrayView] Equipment Canvas 표시: {isXrayActive}");
            }
        }

        /// <summary>
        /// 장비 X-Ray 상태 변경 (장비 표시/숨김)
        /// </summary>
        private void OnEquipmentXrayChanged(bool isXrayActive)
        {
            if (equipments != null)
            {
                // X-Ray 활성화 = 장비 숨김 (센서가 보임)
                // ⭐⭐⭐ 주의: Canvas는 제외하고 장비만 숨김
                foreach (Transform child in equipments.transform)
                {
                    // Canvas는 건물 X-Ray로 제어되므로 건드리지 않음
                    if (child.name != "Canvas")
                    {
                        child.gameObject.SetActive(!isXrayActive);
                    }
                }
                Debug.Log($"[XrayView] 장비 표시: {!isXrayActive}");
            }
        }

        /// <summary>
        /// 센서 표시 상태 변경 (두 X-Ray 모두 활성화시만 표시)
        /// </summary>
        private void OnSensorsVisibilityChanged(bool shouldShow)
        {
            if (sensorsParent == null) return;

            try
            {
                foreach (Transform sensor in sensorsParent)
                {
                    if (sensor.name.StartsWith("Sensor_"))
                    {
                        sensor.gameObject.SetActive(shouldShow);
                    }
                }
                Debug.Log($"[XrayView] 센서 표시: {shouldShow}");   
            }
            catch (Exception ex)
            {
                Debug.LogError($"[XrayView] 센서 표시 제어 실패: {ex.Message}");
                Debug.LogException(ex);
            }
        }

        #endregion

        #region Private Methods

        private void UpdateAllVisibility()
        {
            if (XrayViewModel.Instance == null) return;

            OnStructureXrayChanged(XrayViewModel.Instance.IsStructureXrayActive);
            OnEquipmentXrayChanged(XrayViewModel.Instance.IsEquipmentXrayActive);
            OnSensorsVisibilityChanged(XrayViewModel.Instance.ShouldShowSensors);

            Debug.Log("[XrayView] 초기 표시 상태 업데이트 완료");
        }

        #endregion
    }
}