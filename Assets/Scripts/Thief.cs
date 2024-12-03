using System;
using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class Thief : Agent, ICurriculumAgent
{
    private Vector2 prizePosition;

    [SerializeField] private Transform prize;
    [SerializeField] private List<Agent> guards;

    [SerializeField] private GameObject plane;

    private float planeX, planeZ;
    private Rigidbody rb;

    [SerializeField] private float maxSpeed = 11.0f;
    [SerializeField] private float rotationSpeed = 45.0f;

    [field: SerializeField] private CurriculumManager curriculumManager;

    [field: SerializeField] public Arena Arena {get; set;}
    private Vector2 initialPosition;
    private float initialRotation;
    public override void Initialize()
    {
        rb = GetComponent<Rigidbody>();
        MaxStep = 0;
        planeX = 60;
        planeZ = 60;
    }

    public override void OnEpisodeBegin()
    {
        Arena.PlaceProceduralPrize(prize.gameObject);
        Arena.PlaceProceduralThief(transform.gameObject);
        prizePosition = new Vector2(prize.localPosition.x / planeX, prize.localPosition.z / planeZ);
        initialPosition = new(transform.localPosition.x / planeX, transform.localPosition.z / planeZ);
        initialRotation = (transform.localRotation.y % 360 + 360) % 360 / 360;
    }
    
    public override void OnActionReceived(ActionBuffers actions)
    {
        float forward = Mathf.Clamp(actions.ContinuousActions[0], -1.0f, 1.0f);
        float rotate = Mathf.Clamp(actions.ContinuousActions[1], -1.0f, 1.0f);

        rb.MoveRotation(rb.rotation * Quaternion.Euler(new Vector3(0, rotate * rotationSpeed, 0) * Time.fixedDeltaTime));
        rb.velocity = forward * maxSpeed * transform.forward;

        //Rewards
        AddReward(-1.0f / Arena.MaxSteps);

        if (StepCount >= Arena.MaxSteps)
        {
            //plane.GetComponent<MeshRenderer>().material.color = Color.white;
            Arena.EndEpisode(Arena.EpisodeResult.DRAW);       
        }
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        Vector2 currPosition = new(transform.localPosition.x / planeX, transform.localPosition.z / planeZ);
        Vector2 currVelocity = new(GetComponent<Rigidbody>().velocity.x / maxSpeed, GetComponent<Rigidbody>().velocity.z / maxSpeed);
        sensor.AddObservation(initialPosition - currPosition);
        sensor.AddObservation(currVelocity);
        sensor.AddObservation(initialRotation - ((transform.localRotation.y % 360 + 360) % 360 / 360));
        sensor.AddObservation(prizePosition - currPosition);
        sensor.AddObservation(Vector3.Magnitude(prizePosition - currPosition));
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continous = actionsOut.ContinuousActions;
        continous[0] = Input.GetKey(KeyCode.UpArrow) ? 1.0f : Input.GetKey(KeyCode.DownArrow) ? -1.0f : 0;
        continous[1] = Input.GetKey(KeyCode.D) ? 1.0f : Input.GetKey(KeyCode.A) ? -1.0f : 0;
    }
    
    private void OnTriggerEnter(Collider other)
    {
 
        if (other.CompareTag("Guard"))
        {
            //plane.GetComponent<MeshRenderer>().material.color = Color.blue;
            Arena.EndEpisode(Arena.EpisodeResult.THIEF_CAUGHT);
        }
        else if (other.CompareTag("Prize"))
        {
            //plane.GetComponent<MeshRenderer>().material.color = new Color(255, 100, 0);
            Arena.EndEpisode(Arena.EpisodeResult.PRIZE_STOLEN);
        }
    }

    public void EndEpisodeCurriculum(float reward, bool interrupt = false)
    {
        AddReward(reward + ProximityReward());
        curriculumManager.AddReward(GetCumulativeReward(), Arena.Id, this);
        if (interrupt)
            EpisodeInterrupted();
        else
            EndEpisode();
    }

    private float ProximityReward()
    {
        float proximity = Vector3.Magnitude(transform.position - prize.transform.position);
        if (proximity < 1.0f)
            return 1.0f;
        return 1.0f / proximity;
    }
}
