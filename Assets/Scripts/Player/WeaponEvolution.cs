using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class WeaponEvolution : MonoBehaviour
{
    public static WeaponEvolution Instance { get; private set; }

    [SerializeField] private AudioClip evolutionSfx;

    private readonly Dictionary<WeaponType, WeaponType> evolutionMap = new Dictionary<WeaponType, WeaponType>
    {
        { WeaponType.IronBow, WeaponType.StormBow },
        { WeaponType.FireStaff, WeaponType.DragonStaff },
        { WeaponType.FrostWand, WeaponType.BlizzardWand },
        { WeaponType.PoisonDagger, WeaponType.DeathDagger },
        { WeaponType.HolyCross, WeaponType.HolyNova },
        { WeaponType.ThunderRod, WeaponType.ZeusRod }
    };

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public bool TryGetEvolution(WeaponType baseWeapon, int copies, out WeaponType evolvedWeapon)
    {
        evolvedWeapon = baseWeapon;
        if (copies < 3)
            return false;

        return evolutionMap.TryGetValue(baseWeapon, out evolvedWeapon);
    }

    public void PlayEvolutionFx(Transform target, WeaponType evolvedWeapon)
    {
        if (target == null)
            return;

        if (evolutionSfx != null)
            AudioSource.PlayClipAtPoint(evolutionSfx, target.position, 0.8f);

        StartCoroutine(ShowEvolutionText(target, evolvedWeapon));
    }

    private IEnumerator ShowEvolutionText(Transform target, WeaponType evolvedWeapon)
    {
        GameObject textObject = RuntimeSpawnGuard.Mark(new GameObject("WeaponEvolutionText"));
        textObject.transform.position = target.position + Vector3.up * 2.2f;
        TextMeshPro text = textObject.AddComponent<TextMeshPro>();
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = 4f;
        text.fontStyle = FontStyles.Bold;
        text.color = new Color(1f, 0.9f, 0.35f, 1f);
        text.text = "EVOLVED!\n" + evolvedWeapon;

        float duration = 1.1f;
        float elapsed = 0f;
        Vector3 start = textObject.transform.position;
        Vector3 end = start + Vector3.up * 1.4f;

        while (elapsed < duration && text != null)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            textObject.transform.position = Vector3.Lerp(start, end, t);
            Color c = text.color;
            c.a = Mathf.Lerp(1f, 0f, t);
            text.color = c;

            Camera cam = Camera.main;
            if (cam != null)
                textObject.transform.rotation = cam.transform.rotation;

            yield return null;
        }

        if (textObject != null)
            Destroy(textObject);
    }
}
