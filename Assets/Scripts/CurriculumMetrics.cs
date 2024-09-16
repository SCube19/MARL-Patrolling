using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents.SideChannels;
using UnityEngine;
using System.Numerics;
using System;
using Mono.Cecil;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using Unity.MLAgents;

public class SingleAgentCurriculumMetrics : SideChannel, ICurriculumMetrics
{
    private int arenaId = 0;

    private string agentName;

    public long DataPoints {get; private set;}
    public float Average {get; private set;}
    private long frequency;
    private static readonly float smoothing = 2.0f;

    public SingleAgentCurriculumMetrics(string guid, string agentName, long frequency)
    {
        // Thief 621f0a70-4f87-11ea-a6bf-784f4387d1f7
        // GuardGroup 621f0a70-4f87-11ea-a6bf-784f4387d1f8
        // Guard 621f0a70-4f87-11ea-a6bf-784f4387d1f9
        ChannelId = new System.Guid(guid);
        DataPoints = 0;
        Average = 0;
        this.agentName = agentName;
        this.frequency = frequency;
        SideChannelManager.RegisterSideChannel(this);
    }

    protected override void OnMessageReceived(IncomingMessage msg)
    {
        throw new System.NotImplementedException();
    }

    public virtual void SendEmaMessage<T>(T ema)
    {
        SendMessage($"Exponential Moving Average For Arena {arenaId}: {ema}, in {DataPoints} episodes ({agentName})");
    }

    public virtual void SendMessage(string message)
    {
        var msg = new OutgoingMessage();
        msg.WriteString(message);
        QueueMessageToSend(msg);
    }

    public virtual void AddReward(float reward, int arenaId, ICurriculumAgent requester)
    {
        Debug.Log("Trying to add a reward with arenaId " + arenaId + " and currentArenaId " + this.arenaId);
        if (arenaId != this.arenaId)
        {
            SendMessage($"arenaId does not match: trying to add {arenaId}, currentArenaId " + this.arenaId);
            return;
        }

        float smoothingTerm = smoothing / (float)(++DataPoints + 1);
        Average = (reward * smoothingTerm) + (Average * (1 - smoothingTerm));
        Debug.Log("adding reward. EMA is " + Average);
        Debug.Log("data Points: " + DataPoints);

        if (DataPoints % frequency == 0)
        {
            SendEmaMessage(Average);
            SendTensorBoardData();
        }
    }

    public virtual void OnArenaChange(int newArenaId)
    {
        var msg = new OutgoingMessage();
        msg.WriteString($"Arena Change ({this.arenaId} -> {newArenaId}), in {DataPoints} episodes (avg. {Average}, agt. {agentName})");
        QueueMessageToSend(msg);
        arenaId = newArenaId;
        Average = 0.0f;
        DataPoints = 0;
    }

    public virtual void SendTensorBoardData()
    {
        Academy.Instance.StatsRecorder.Add("Exponential Moving Average Reward", Average);
    }
}