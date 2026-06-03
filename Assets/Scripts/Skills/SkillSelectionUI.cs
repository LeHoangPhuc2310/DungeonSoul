using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SkillSelectionUI : MonoBehaviour
{
    public static SkillSelectionUI Instance;

    public Canvas skillCanvas;
    public Button[] skillButtons = new Button[3];
    public List<SkillData> allSkills = new List<SkillData>();

    private readonly List<SkillData> currentShownSkills = new List<SkillData>(3);

    private void Awake()
    {
        Instance = this;

        if (skillCanvas == null)
            skillCanvas = GetComponentInChildren<Canvas>(true);

        if (skillCanvas != null)
            skillCanvas.gameObject.SetActive(false);

        EnsureButtonArray();
    }

    public void Show()
    {
        if (skillCanvas == null)
            return;

        if (allSkills == null || allSkills.Count == 0)
        {
            SkillData[] loaded = Resources.LoadAll<SkillData>("SkillData");
            allSkills = new List<SkillData>(loaded);
            Debug.Log("Loaded: " + allSkills.Count + " skills from Resources/Skills");
        }

        Time.timeScale = 0f;
        skillCanvas.gameObject.SetActive(true);

        EnsureButtonArray();
        currentShownSkills.Clear();
        currentShownSkills.AddRange(GetRandomSkills(3));

        for (int i = 0; i < skillButtons.Length; i++)
        {
            Button button = skillButtons[i];
            if (button == null)
                continue;

            button.onClick.RemoveAllListeners();

            SkillData skill = i < currentShownSkills.Count ? currentShownSkills[i] : null;
            button.interactable = skill != null;

            if (button.targetGraphic != null)
            {
                button.targetGraphic.color = Color.white;
            }

            TMP_Text label = button.GetComponentInChildren<TMP_Text>();
            if (label != null)
            {
                label.color = Color.black;
                if (skill == null)
                {
                    label.text = "Empty";
                }
                else
                {
                    label.text = $"{skill.skillName}\n{skill.description}\n{skill.rarity}";
                }
            }

            if (skill != null)
            {
                SkillData capturedSkill = skill;
                button.onClick.AddListener(() => OnSkillButtonClicked(capturedSkill));
            }
        }
    }

    private void OnSkillButtonClicked(SkillData skill)
    {
        if (skill != null && PlayerSkillHandler.Instance != null)
            PlayerSkillHandler.Instance.ApplySkill(skill);

        if (skillCanvas != null)
            skillCanvas.gameObject.SetActive(false);

        Time.timeScale = 1f;
    }

    private void EnsureButtonArray()
    {
        if (skillButtons == null || skillButtons.Length != 3)
            skillButtons = new Button[3];
    }

    private List<SkillData> GetRandomSkills(int count)
    {
        List<SkillData> pool = new List<SkillData>();
        if (allSkills != null)
        {
            for (int i = 0; i < allSkills.Count; i++)
            {
                if (allSkills[i] != null)
                    pool.Add(allSkills[i]);
            }
        }

        List<SkillData> selected = new List<SkillData>(count);
        int pickCount = Mathf.Min(count, pool.Count);
        for (int i = 0; i < pickCount; i++)
        {
            int randomIndex = Random.Range(0, pool.Count);
            selected.Add(pool[randomIndex]);
            pool.RemoveAt(randomIndex);
        }

        return selected;
    }
}
