
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

class MultiAgentCurriculumManager : CurriculumManager
{

    private new MultiAgentCurriculumMetrics metrics;

    private Dictionary<int, List<GameObject>> arenaGroups;

    private Dictionary<int, Tuple<Arena, float>> arenaIdToArena = new();
    [SerializeField] private int arenaRatioAdjustmentInterval = 200;
    [SerializeField] private int minimumArenaGroupSize = 3;

    private int trashLevel = 0;
    private float trashStart = 5.0f;

    private int trashLevels = 64;


    public override void Start()
    {
        arenaGroups = new();
        float offset = 0.0f;
        List<int> ids = new List<int>();
        foreach (var arena in arenas)
        {
            arenaIdToArena.Add(arena.Id, new Tuple<Arena, float>(arena, offset));
            offset += arena.ArenaSize + 25.0f;
            ids.Add(arena.Id);
        }
        metrics = new MultiAgentCurriculumMetrics("621f0a70-4f87-11ea-a6bf-784f4387d1f8", 300, ids);
        CreateArenaGroups();
        StartCoroutine(AdjustArenaRatios());
    }

    public override void AddReward(float reward, int arenaId, ICurriculumAgent requester)
    {
        metrics?.AddReward(reward, arenaId, requester);
    }

    private void ChangeArenaGroupSize(int arenaId, double arenaPercentage)
    {
        int groupSize = Math.Max(minimumArenaGroupSize, (int)Math.Ceiling((double)nArenas * arenaPercentage));

        var (arena, offset) = arenaIdToArena[arenaId];
        if (groupSize > arenaGroups[arenaId].Count)
        {
            arena.gameObject.SetActive(true);
            for (int i = arenaGroups[arenaId].Count; i < groupSize; i++)
                arenaGroups[arenaId].Add(Instantiate(arena.gameObject, new (transform.position.x + offset, 0.0f, i * (arena.ArenaSize + 1.0f)), new Quaternion()));
            arena.gameObject.SetActive(false);
        }
        else 
        {
            List<GameObject> group = arenaGroups[arenaId];
            for (int i = group.Count - 1; i >= groupSize; i--)
            {
                group[i].transform.position += new Vector3(0, trashStart * (1 + trashLevel), 0);
                group[i].GetComponent<Arena>().Die = true;
                group[i].gameObject.transform.GetChild(2).GetComponent<MeshRenderer>().material.color = Color.red;
            }
            group.RemoveRange(groupSize, group.Count - groupSize);   

            trashLevel = (trashLevel + 1) % trashLevels;
        }
    }

    public IEnumerator AdjustArenaRatios()
    {
        float skew = 5.0f;
        while (true)
        {
            Dictionary<int, float> arenasEmaDelta = new Dictionary<int, float>();
            double expSum = 0;
            foreach (var el in metrics.ArenaToStats)
            {
                float averageChange = (el.Value.GuardGroupAverageDelta + el.Value.ThiefAverageDelta) / 2.0f;
                arenasEmaDelta.Add(el.Key, averageChange * skew);
                expSum += Math.Exp(averageChange * skew);
            }
            
            foreach (var arena in arenasEmaDelta)
            {   
                double arenaPercentage = Math.Exp(arena.Value) / expSum;
                ChangeArenaGroupSize(arena.Key, arenaPercentage);
                metrics.SendMessage($"Adjusted ratio for arena {arena.Key}: {arenaPercentage * 100}% = {arenaGroups[arena.Key].Count}");
            }
            
            yield return new WaitForSecondsRealtime(arenaRatioAdjustmentInterval);
        }
    }

    private void CreateArenaGroups()
    {
        int groupSize = nArenas / arenas.Count;
        float verticalOffset = 0;
        foreach (var arena in arenas)
        {
            arenaGroups.Add(arena.Id, new List<GameObject>());
            arena.gameObject.SetActive(true);
            for (int i = 0; i < groupSize; i++)
            {
                arenaGroups[arena.Id].Add(Instantiate(arena.gameObject, new (transform.position.x + verticalOffset, 0.0f, i * (arena.ArenaSize + 1.0f)), new Quaternion()));
            }
            verticalOffset += arena.ArenaSize + 25.0f;
            arena.gameObject.SetActive(false);
        }
    }
}