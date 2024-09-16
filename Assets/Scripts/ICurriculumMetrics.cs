
public interface ICurriculumMetrics
{
    public void SendMessage(string message);
    public void SendEmaMessage<T>(T ema);

    public void AddReward(float reward, int arenaId, ICurriculumAgent requester);

    public void OnArenaChange(int newArenaId);

    public void SendTensorBoardData();
}