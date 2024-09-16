using System;
using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class GuardGroup : MonoBehaviour, ICurriculumAgent
{
    [SerializeField] private List<Agent> guards;

    [field: SerializeField] public Arena Arena {get; set;}
    
    [field: SerializeField] private CurriculumManager curriculumManager;

    private SimpleMultiAgentGroup simpleGuardGroup = new();

    private float cumulativeReward = 0.0f;

    void Start()
    {
        guards.ForEach(guard => simpleGuardGroup.RegisterAgent(guard)); 
    }

    void FixedUpdate()
    {
        //Rewards
        simpleGuardGroup.AddGroupReward(-1.0f / Arena.MaxSteps);
        cumulativeReward += -1.0f / Arena.MaxSteps;
    }

    public void EndEpisodeCurriculum(float reward, bool interrupt = false)
    {
        simpleGuardGroup.AddGroupReward(reward);
        cumulativeReward += reward;
        curriculumManager.AddReward(cumulativeReward, Arena.Id, this);
        cumulativeReward = 0.0f;

        if (interrupt)
            simpleGuardGroup.GroupEpisodeInterrupted();
        else
            simpleGuardGroup.EndGroupEpisode();
    }
}
