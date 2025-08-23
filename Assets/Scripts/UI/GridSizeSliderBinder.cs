// GridSizeSliderBinder.cs
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GridSizeSliderBinder : MonoBehaviour
{
    [SerializeField] HexGrid grid;         // reference to your HexGrid
    [SerializeField] Slider widthSlider;   // slider for width
    [SerializeField] Slider heightSlider;  // slider for height
    [SerializeField] TMP_Text widthLabel;  // optional: shows current width
    [SerializeField] TMP_Text heightLabel; // optional: shows current height

    void Start()
    {
        if (grid == null) return;

        if (widthSlider)
        {
            widthSlider.SetValueWithoutNotify(grid.width);
            widthSlider.onValueChanged.AddListener(OnWidthChanged);
        }

        if (heightSlider)
        {
            heightSlider.SetValueWithoutNotify(grid.height);
            heightSlider.onValueChanged.AddListener(OnHeightChanged);
        }

        UpdateLabels();
    }

    void OnDestroy()
    {
        if (widthSlider) widthSlider.onValueChanged.RemoveListener(OnWidthChanged);
        if (heightSlider) heightSlider.onValueChanged.RemoveListener(OnHeightChanged);
    }

    void OnWidthChanged(float value)
    {
        if (!grid) return;
        grid.GetType().GetProperty("width")?.SetValue(grid, Mathf.RoundToInt(value));
        UpdateLabels();
    }

    void OnHeightChanged(float value)
    {
        if (!grid) return;
        grid.GetType().GetProperty("height")?.SetValue(grid, Mathf.RoundToInt(value));
        UpdateLabels();
    }

    void UpdateLabels()
    {
        if (widthLabel) widthLabel.text = $"Width: {grid.width}";
        if (heightLabel) heightLabel.text = $"Height: {grid.height}";
    }
}
