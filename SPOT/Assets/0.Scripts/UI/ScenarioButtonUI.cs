using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class ScenarioButtonUI : MonoBehaviour
{
    [SerializeField] private WeldDataGenerator generator;
    [SerializeField] private Transform scenarioButtonContainer;
    [SerializeField] private TextMeshProUGUI scenarioLabel;
    [SerializeField] private Button autoCycleButton;

    [SerializeField] private Color colorNormalScenario    = new Color(0.2f, 0.8f, 0.2f);
    [SerializeField] private Color colorCautionScenario   = new Color(1f,   0.8f, 0f);
    [SerializeField] private Color colorReinspectScenario = new Color(0.9f, 0.2f, 0.2f);
    [SerializeField] private Color colorIdle              = new Color(0.3f, 0.3f, 0.3f);

    private readonly List<Button> buttons = new List<Button>();

    private void Start()
    {
        if (generator == null)
        {
            Debug.LogError($"{nameof(ScenarioButtonUI)}: generator reference missing.", this);
            enabled = false;
            return;
        }

        CollectButtons();

        int wired = Mathf.Min(buttons.Count, generator.ScenarioCount);
        for (int i = 0; i < wired; i++)
        {
            int index = i;
            buttons[i].onClick.AddListener(() => SelectScenario(index));

            var label = buttons[i].GetComponentInChildren<TextMeshProUGUI>(includeInactive: true);
            if (label != null) label.text = generator.GetScenarioLabel(i);
        }

        if (autoCycleButton != null)
            autoCycleButton.onClick.AddListener(SelectAutoCycle);

        if (generator.AutoCycleMode)
            SelectAutoCycle();
        else
            SelectScenario(generator.SelectedScenario);
    }

    private void CollectButtons()
    {
        buttons.Clear();
        Transform root = scenarioButtonContainer != null ? scenarioButtonContainer : transform;
        var found = root.GetComponentsInChildren<Button>(includeInactive: false);
        for (int i = 0; i < found.Length; i++)
            if (found[i] != autoCycleButton) buttons.Add(found[i]);
    }

    private void SelectScenario(int index)
    {
        generator.SelectScenario(index);

        if (scenarioLabel != null)
            scenarioLabel.text = $"시나리오: {generator.GetScenarioLabel(index)}";

        ApplyButtonHighlight(index, ScenarioColor(index));
    }

    private void SelectAutoCycle()
    {
        generator.EnableAutoCycle();

        if (scenarioLabel != null)
            scenarioLabel.text = "시나리오: 자동 순환";

        ApplyButtonHighlight(-1, colorIdle);
    }

    private void ApplyButtonHighlight(int selectedIndex, Color selectedColor)
    {
        for (int i = 0; i < buttons.Count; i++)
        {
            var colors = buttons[i].colors;
            colors.normalColor = (i == selectedIndex) ? selectedColor : colorIdle;
            buttons[i].colors = colors;
        }
    }

    private Color ScenarioColor(int index)
    {
        return index switch
        {
            0      => colorNormalScenario,
            1 or 2 => colorCautionScenario,
            _      => colorReinspectScenario,
        };
    }
}
