
using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.SideChannels;
using UnityEngine;

public class CurriculumManager : MonoBehaviour
{
    //zrobi� z tego interface na prosty manager i competitive manager
    [SerializeField] protected List<Arena> arenas;
    [SerializeField] protected long episodeLimit;
    [SerializeField] protected int nArenas;
    
    enum Type 
    {
        THIEF,
        GUARD
    }

    [SerializeField] private Type managerType;
    protected int currentArenaIndex = -1;

    protected GameObject[] currentArenas;

    private static readonly System.Random rng = new();

    private CurriculumMetrics metrics;

    virtual public void Start()
    {
        switch(managerType)
        {
            case Type.THIEF:
                metrics = new("621f0a70-4f87-11ea-a6bf-784f4387d1f7", "THIEF", 100);
                break;
            case Type.GUARD:
                metrics = new("621f0a70-4f87-11ea-a6bf-784f4387d1f9", "GUARD", 100);
                break;
        }
        currentArenas = new GameObject[nArenas];
        NextArena();
        StartCoroutine(TryChangeArena());
    }
       
    virtual public void AddReward(float reward, int arenaId, ICurriculumAgent requester)
    {
        metrics.AddReward(reward, arenaId);
        if (metrics.DataPoints >= episodeLimit)
            PrevArena();
    }

    //Interval coroutine for trying to advance to the next arena
    virtual protected IEnumerator TryChangeArena()
    {
        while (true)
        {
            Debug.Log("Checking for new arena");
            if (metrics.DataPoints >= arenas[currentArenaIndex].MinimumEpisodes &&
               metrics.Average >= arenas[currentArenaIndex].TargetAverageReward)
                NextArena();
            yield return new WaitForSecondsRealtime(10);
        }
    }

    protected void PrevArena()
    {
        if (currentArenaIndex > 0)
            ChangeArena(--currentArenaIndex);
        else
            ChangeArena(0);
    }
    protected void NextArena()
    {
        if (currentArenaIndex >= arenas.Count - 1)
        {
            ChangeArena(rng.Next(arenas.Count));
            metrics.SendMessage("Training Completed, continuing with random arena");
        }
        else
            ChangeArena(++currentArenaIndex);
    }

    protected void ChangeArena(int arenaIndex)
    {
        Debug.Log("Changing the arena");
        metrics.OnArenaChange(arenas[arenaIndex].Id);

        Arena arena = arenas[arenaIndex];
        arena.gameObject.SetActive(true);
        for (int i = 0; i < nArenas; i++)
        {
            GameObject next = Instantiate(arena.gameObject, new (transform.position.x, 0.0f, i * (arena.ArenaSize + 1.0f)), new Quaternion());
            Destroy(currentArenas[i]);
            currentArenas[i] = next;
        }
        arena.gameObject.SetActive(false);
    }
}
