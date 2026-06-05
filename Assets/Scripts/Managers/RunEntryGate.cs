// DungeonSoul — RunEntryGate.cs — Bắt buộc qua màn chọn nhân vật trước khi chơi.

using UnityEngine;

public static class RunEntryGate
{
    public static bool ConfirmedThisPlaySession { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnPlayMode()
    {
        ConfirmedThisPlaySession = false;
    }

    public static void ConfirmCharacterSelect()
    {
        ConfirmedThisPlaySession = true;
    }
}
