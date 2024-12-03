using Unity.VisualScripting;
using Unity.MLAgents.SideChannels;
using UnityEngine;
using System.Collections.Generic;
using System;
using Unity.MLAgents;
using System.Linq;

class MultiAgentCurriculumMetrics : SideChannel, ICurriculumMetrics
{

    public class ArenaStats
    {
        public long ThiefDataPoints = 0;
        public long ThiefDeltaDataPoints = 0;
        public long GuardGroupDataPoints = 0;
        public long GuardGroupDeltaDataPoints = 0;
        public float ThiefAverageDelta = 0.0f;
        public float GuardGroupAverageDelta = 0.0f;
        public float LastThiefAverage = 0.0f;
        public float LastGuardGroupAverage = 0.0f;
        public float ThiefAverage = 0.0f;
        public float GuardGroupAverage = 0.0f;
    };

    public Dictionary<int, ArenaStats> ArenaToStats = new ();
    private long frequency;
    private static readonly float smoothing = 2.0f;
    public MultiAgentCurriculumMetrics(string guid, long frequency, List<int> ids) 
    {
        ChannelId = new System.Guid(guid);
        foreach (var id in ids)
            ArenaToStats[id] = new ArenaStats();
        this.frequency = frequency;
        SideChannelManager.RegisterSideChannel(this);
    }
    
    private long DeltaUpdates = 0;

    //100 is around 25 episodes
    private long DeltaUpdateFrequency = 100;

    public void AddReward(float reward, int arenaId, ICurriculumAgent requester)
    {
        DeltaUpdates++;

        ArenaStats arenaStats = ArenaToStats[arenaId];
        if (requester is Thief)
        {
            float smoothingTerm = smoothing / (float)(++arenaStats.ThiefDataPoints + 1);
            arenaStats.ThiefAverage = (reward * smoothingTerm) + (arenaStats.ThiefAverage * (1 - smoothingTerm));
        }
        else
        {
            float smoothingTerm = smoothing / (float)(++arenaStats.GuardGroupDataPoints + 1);
            arenaStats.GuardGroupAverage = (reward * smoothingTerm) + (arenaStats.GuardGroupAverage * (1 - smoothingTerm));
        }
        
        if ((arenaStats.ThiefDataPoints + arenaStats.GuardGroupDataPoints) % DeltaUpdateFrequency == 0)
        {
            float thiefDelta = Mathf.Abs(arenaStats.LastThiefAverage - arenaStats.ThiefAverage);
            float smoothingTerm = smoothing / (float)(++arenaStats.ThiefDeltaDataPoints + 1);
            arenaStats.ThiefAverageDelta = (thiefDelta * smoothingTerm) + (arenaStats.ThiefAverageDelta * (1 - smoothingTerm));
            arenaStats.LastThiefAverage = arenaStats.ThiefAverage;

            float guardDelta = Mathf.Abs(arenaStats.LastGuardGroupAverage - arenaStats.GuardGroupAverage);
            float smoothingTerm2 = smoothing / (float)(++arenaStats.GuardGroupDeltaDataPoints + 1);
            arenaStats.GuardGroupAverageDelta = (guardDelta * smoothingTerm2) + (arenaStats.GuardGroupAverageDelta * (1 - smoothingTerm2));
            arenaStats.LastGuardGroupAverage = arenaStats.GuardGroupAverage;
            
        }

        if (DeltaUpdates % frequency == 0)
        {
            SendEmaMessage(0);
            SendTensorBoardData();
        }
    }

    public void OnArenaChange(int newArenaId)
    {
        throw new System.NotImplementedException();
    }

    protected override void OnMessageReceived(IncomingMessage msg)
    {
        throw new System.NotImplementedException();
    }

    public virtual void SendEmaMessage<T>(T ema)
    {
        foreach (var item in ArenaToStats)
            SendMessage($"Exponential Moving Average For Arena {item.Key}:[(Thief: {item.Value.ThiefAverage}, {item.Value.ThiefDataPoints} eps.), (Guards: {item.Value.GuardGroupAverage}, {item.Value.GuardGroupDataPoints} eps.)]");
        
        foreach (var item in ArenaToStats)
            SendMessage($"Delta-EMA Of Reward Stats For Arena {item.Key}:[(Thief: {item.Value.ThiefAverageDelta}, {item.Value.ThiefDeltaDataPoints} eps.), (Guards: {item.Value.GuardGroupAverageDelta}, {item.Value.GuardGroupDeltaDataPoints} eps.)]");
    }

    public virtual void SendMessage(string message)
    {
        var msg = new OutgoingMessage();
        msg.WriteString(message);
        QueueMessageToSend(msg);
    }

    public virtual void SendTensorBoardData()
    {
        float averageThief = ArenaToStats.Values.Average(x => x.ThiefAverage);
        float averageGuards = ArenaToStats.Values.Average(x => x.GuardGroupAverage);
        float averageThiefDelta = ArenaToStats.Values.Average(x => x.ThiefAverageDelta);
        float averageGuardsDelta = ArenaToStats.Values.Average(x => x.GuardGroupAverageDelta);
        Academy.Instance.StatsRecorder.Add("Exponential Moving Average Thief Reward", averageThief);
        Academy.Instance.StatsRecorder.Add("Exponential Moving Average Guards Reward", averageGuards);
        Academy.Instance.StatsRecorder.Add("Exponential Moving Average Thief Reward Delta", averageThiefDelta);
        Academy.Instance.StatsRecorder.Add("Exponential Moving Average Guards Reward Delta", averageGuardsDelta);
    }

}