
using System.Collections;
using System.Runtime.InteropServices;
using UnityEngine;

class MultiAgentCurriculumManager : CurriculumManager
{

    private CurriculumMetrics thiefMetrics = new("621f0a70-4f87-11ea-a6bf-784f4387d1f7", "THIEF", 100);
    private CurriculumMetrics guardGroupMetrics = new("621f0a70-4f87-11ea-a6bf-784f4387d1f8", "GUARD", 100);

    private float thiefAverageDelta = 0.0f;
    private float guardGroupAverageDelta = 0.0f;

    private float lastThiefEma = 0.0f;
    private float lastGuardGroupEma = 0.0f;
    private static readonly float smoothing = 2.0f;

    private long dataPoints = 0;

    [SerializeField] private float curriculumAverageRewardThreshold = 0.1f;

    public override void Start()
    {
        currentArenas = new GameObject[nArenas];
        NextArena();
        StartCoroutine(TryChangeArena());
    }

    public override void AddReward(float reward, int arenaId, ICurriculumAgent requester)
    {
        if (requester is Thief)
            thiefMetrics.AddReward(reward, arenaId);
        else
            guardGroupMetrics.AddReward(reward, arenaId);

        if (thiefMetrics.DataPoints >= episodeLimit)
            PrevArena();
    }

    protected override IEnumerator TryChangeArena()
    {
        while (true)
        {
            float thiefDelta = Mathf.Abs(lastThiefEma - thiefMetrics.Average);
            float guardDelta = Mathf.Abs(lastGuardGroupEma - guardGroupMetrics.Average);
            float smoothingTerm = smoothing / (float)(++dataPoints + 1);
            thiefAverageDelta = (thiefDelta * smoothingTerm) + (thiefAverageDelta * (1 - smoothingTerm));
            guardGroupAverageDelta = (guardDelta * smoothingTerm) + (guardGroupAverageDelta * (1 - smoothingTerm));

            if (thiefMetrics.DataPoints >= arenas[currentArenaIndex].MinimumEpisodes &&
                thiefAverageDelta <= curriculumAverageRewardThreshold &&
                guardGroupAverageDelta <= curriculumAverageRewardThreshold && 
                (thiefMetrics.Average > 0 || guardGroupMetrics.Average > 0))
                NextArena();

            yield return new WaitForSecondsRealtime(10);
        }
    }
}