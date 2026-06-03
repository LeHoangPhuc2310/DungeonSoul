using UnityEngine;

public class EnemyReward : MonoBehaviour
{
    [SerializeField] private int scoreReward = 10;
    [SerializeField] private int coinMin = 3;
    [SerializeField] private int coinMax = 6;
    [SerializeField] private bool isElite;
    [SerializeField] private bool isBoss;

    public int ScoreReward => scoreReward;
    public bool IsBoss => isBoss;

    public int RollCoins()
    {
        int min = coinMin;
        int max = coinMax + 1;
        if (isElite)
        {
            min += 4;
            max += 8;
        }
        if (isBoss)
        {
            min += 20;
            max += 40;
        }
        return Random.Range(min, max);
    }
}
