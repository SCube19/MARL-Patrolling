using System;
using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using UnityEngine;

public class Arena : MonoBehaviour
{

    [SerializeField] public int Id;
    [SerializeField] public float TargetAverageReward;
    [SerializeField] public int MinimumEpisodes;
    [SerializeField] public float ArenaSize;
    [SerializeField] public int MaxSteps;

    [SerializeField] private GameObject thief;
    [SerializeField] private GuardGroup guardGroup;

    [SerializeField] private List<Collider> prizeAreas;
    [SerializeField] private List<Collider> thiefAreas;
    [SerializeField] private List<Collider> guardAreas;

    private static readonly System.Random rng = new();

    public bool Die = false;

    public enum EpisodeResult {
        THIEF_CAUGHT,
        PRIZE_STOLEN,
        DRAW
    };

    private static readonly Dictionary<EpisodeResult, Tuple<float, float>> resultToReward = new()
    {
        {EpisodeResult.THIEF_CAUGHT, new Tuple<float, float>(-3.0f, 3.0f)},
        {EpisodeResult.PRIZE_STOLEN, new Tuple<float, float>(2.0f, -3.0f)},
        {EpisodeResult.DRAW, new Tuple<float, float>(0.0f, 0.0f)}
    };

    public void EndEpisode(EpisodeResult result)
    {
        if (thief != null)
            thief?.GetComponent<ICurriculumAgent>().EndEpisodeCurriculum(resultToReward.GetValueOrDefault(result).Item1, result == EpisodeResult.DRAW);
        if (guardGroup != null)
            guardGroup?.EndEpisodeCurriculum(resultToReward.GetValueOrDefault(result).Item2, result == EpisodeResult.DRAW);
            
        if (Die)
            Destroy(gameObject);
    }   

    public void PlaceProceduralPrize(GameObject prize)
    {
        PlaceProceduralGameObject(prize, prizeAreas);
    }

    public void PlaceProceduralThief(GameObject thief)
    {
        PlaceProceduralGameObject(thief, thiefAreas);
    }

    public void PlaceProceduralGuard(GameObject guard)
    {
        PlaceProceduralGameObject(guard, guardAreas);
    }

    private void PlaceProceduralGameObject(GameObject obj, List<Collider> areas)
    {
        Collider area = areas[rng.Next(areas.Count)];
        float minX = area.bounds.min.x;
        float maxX = area.bounds.max.x;
        float minZ = area.bounds.min.z;
        float maxZ = area.bounds.max.z;

        float posX = UnityEngine.Random.Range(minX, maxX);
        float posZ = UnityEngine.Random.Range(minZ, maxZ);
        float rotation = UnityEngine.Random.Range(-180.0f, 180.0f);

        obj.transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
        obj.transform.position = new Vector3(posX, 0.25f, posZ);
        obj.transform.localPosition = new Vector3(obj.transform.localPosition.x, 0.25f, obj.transform.localPosition.z);
    }
}
