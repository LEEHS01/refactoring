using UnityEngine;
using UnityEngine.UI;

public class DynamicGridCellSize : MonoBehaviour
{
    private GridLayoutGroup gridLayout;
    private RectTransform rectTransform;

    void Start()
    {
        gridLayout = GetComponent<GridLayoutGroup>();
        rectTransform = GetComponent<RectTransform>();
        UpdateCellSize();
    }

    void Update()
    {
        // 해상도 변경 감지 (필요시)
        UpdateCellSize();
    }

    void UpdateCellSize()
    {
        int columnCount = 7; // GridToxin, GridWaterQuality는 7
        float aspectRatio = 2.25f; // 171/76

        float availableWidth = rectTransform.rect.width;
        float spacingTotal = gridLayout.spacing.x * (columnCount - 1);
        float paddingTotal = gridLayout.padding.left + gridLayout.padding.right;

        float cellWidth = (availableWidth - spacingTotal - paddingTotal) / columnCount;
        float cellHeight = cellWidth / aspectRatio;

        gridLayout.cellSize = new Vector2(cellWidth, cellHeight);
    }
}