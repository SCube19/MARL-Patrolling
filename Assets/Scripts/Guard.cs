using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using Unity.VisualScripting;
using UnityEngine;

public class Guard : Agent
{

    //maybe not aware of the prize?? Then naturally they wouldn't guard it
    //[SerializeField] private Transform prize;

    [SerializeField] private List<Agent> guards;
    //[SerializeField] private Thief thief;

    [SerializeField] private GameObject plane;

    private float planeX, planeZ;
    private Rigidbody rb;

    [SerializeField] private float maxSpeed = 11.0f;
    [SerializeField] private float rotationSpeed = 45.0f;

    [SerializeField] private Arena arena;

    [SerializeField] private bool soloScenario;

    [SerializeField] private GameObject soloScenarioThief;

    public override void Initialize()
    {
        rb = GetComponent<Rigidbody>();
        MaxStep = 0;
        planeX = plane.GetComponent<Renderer>().bounds.size.x;
        planeZ = plane.GetComponent<Renderer>().bounds.size.z;
    }

    public override void OnEpisodeBegin()
    {
        arena.PlaceProceduralGuard(transform.gameObject);
        if (soloScenario)
            arena.PlaceProceduralPrize(soloScenarioThief);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        float forward = Mathf.Clamp(actions.ContinuousActions[0], -1.0f, 1.0f);
        float rotate = Mathf.Clamp(actions.ContinuousActions[1], -1.0f, 1.0f);

        rb.MoveRotation(rb.rotation * Quaternion.Euler(new Vector3(0, rotate * rotationSpeed, 0) * Time.fixedDeltaTime));
        rb.velocity = forward * maxSpeed * transform.forward;

        if (soloScenario && StepCount >= arena.MaxSteps)
        {
            plane.GetComponent<MeshRenderer>().material.color = Color.white;
            arena.EndEpisode(Arena.EpisodeResult.DRAW);       
        }
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        Vector2 currPosition = new(transform.localPosition.x / planeX, transform.localPosition.z / planeZ);
        sensor.AddObservation(currPosition);
        sensor.AddObservation(new Vector2(GetComponent<Rigidbody>().velocity.x / maxSpeed, GetComponent<Rigidbody>().velocity.z / maxSpeed));
        sensor.AddObservation(guards.ConvertAll<float>(guard => guard.transform.localPosition.x / planeX ));
        sensor.AddObservation(guards.ConvertAll<float>(guard => guard.transform.localPosition.z / planeZ ));
        sensor.AddObservation(guards.ConvertAll<float>(guard => guard.GetComponent<Rigidbody>().velocity.x / maxSpeed));
        sensor.AddObservation(guards.ConvertAll<float>(guard => guard.GetComponent<Rigidbody>().velocity.z / maxSpeed));
        //position of a thief if known
        //actually prize knowledge is a fun experiment to do as a variant
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continous = actionsOut.ContinuousActions;
        continous[0] = Input.GetKey(KeyCode.UpArrow) ? 1.0f : Input.GetKey(KeyCode.DownArrow) ? -1.0f : 0;
        continous[1] = Input.GetKey(KeyCode.D) ? 1.0f : Input.GetKey(KeyCode.A) ? -1.0f : 0;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (soloScenario && other.CompareTag("Thief"))
        {
            plane.GetComponent<MeshRenderer>().material.color = Color.blue;
            arena.EndEpisode(Arena.EpisodeResult.THIEF_CAUGHT);
        }
    }
}
