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

    [SerializeField] private List<Guard> guards;
    //[SerializeField] private Thief thief;

    [SerializeField] private GameObject plane;

    private float planeX, planeZ;
    private Rigidbody rb;

    [SerializeField] private float maxSpeed = 11.0f;
    [SerializeField] private float rotationSpeed = 45.0f;

    [SerializeField] private Arena arena;

    [SerializeField] private bool soloScenario;

    [SerializeField] private GameObject soloScenarioThief;

    public bool ThiefVisible { get; set; }

    public GameObject Thief { get; set; }

    BufferSensorComponent friendSensor;
    BufferSensorComponent thiefSensor;

    [SerializeField] private Transform prize;
    private Vector2 prizePosition;

    private Vector2 initialPosition;
    private float initialRotation;

    public override void Initialize()
    {
        rb = GetComponent<Rigidbody>();
        var bufferSensor = GetComponents<BufferSensorComponent>();
        friendSensor = bufferSensor[0];
        thiefSensor = bufferSensor[1];
        MaxStep = 0;
        planeX = 60;
        planeZ = 60;
    }

    public override void OnEpisodeBegin()
    {
        arena.PlaceProceduralGuard(transform.gameObject);
        if (soloScenario)
            arena.PlaceProceduralPrize(soloScenarioThief);
        prizePosition = prize ? new Vector2(prize.localPosition.x / planeX, prize.localPosition.z / planeZ) : new Vector2(0, 0);
        initialPosition = new(transform.localPosition.x / planeX, transform.localPosition.z / planeZ);
        initialRotation = (transform.localRotation.y % 360 + 360) % 360 / 360;
        StartCoroutine(ThiefVisibleUpdate());
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        float forward = Mathf.Clamp(actions.ContinuousActions[0], -1.0f, 1.0f);
        float rotate = Mathf.Clamp(actions.ContinuousActions[1], -1.0f, 1.0f);

        rb.MoveRotation(rb.rotation * Quaternion.Euler(new Vector3(0, rotate * rotationSpeed, 0) * Time.fixedDeltaTime));
        rb.velocity = forward * maxSpeed * transform.forward;

        if (soloScenario && StepCount >= arena.MaxSteps)
        {
            //plane.GetComponent<MeshRenderer>().material.color = Color.white;
            arena.EndEpisode(Arena.EpisodeResult.DRAW);       
        }
        if (!soloScenario)
        {
            
            Vector2 currPosition = new(transform.localPosition.x / planeX, transform.localPosition.z / planeZ);
            float proximity = Vector3.Magnitude(currPosition - prizePosition);
            if (proximity <= 1.0f)
                AddReward(-1.0f / (arena.MaxSteps * 1.0f));
            else
                AddReward(-1.0f / (arena.MaxSteps * proximity));
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

        if (guards.Count == 0)
            return;
        bool thiefAlreadyAdded = false;
        foreach (Guard guard in guards)
        {
            float[] guardsPositions = new float[2]{guard.transform.localPosition.x / planeX - currPosition.x, guard.transform.localPosition.z / planeZ - currPosition.y};
            float[] guardsVelocities = new float[2]{guard.GetComponent<Rigidbody>().velocity.x / maxSpeed - currVelocity.x, guard.GetComponent<Rigidbody>().velocity.z / maxSpeed - currVelocity.y};
            friendSensor.AppendObservation(guardsPositions);
            friendSensor.AppendObservation(guardsVelocities);
            if (!thiefAlreadyAdded && guard.ThiefVisible)
            {
                thiefAlreadyAdded = true;
                float[] thiefPosition = new float[2]{guard.Thief.transform.localPosition.x / planeX - currPosition.x, guard.Thief.transform.localPosition.z / planeZ - currPosition.y};
                thiefSensor.AppendObservation(thiefPosition);
            }
        }

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
            //plane.GetComponent<MeshRenderer>().material.color = Color.blue;
            arena.EndEpisode(Arena.EpisodeResult.THIEF_CAUGHT);
            StopCoroutine(ThiefVisibleUpdate());
        }
    }

    public IEnumerator ThiefVisibleUpdate()
    {
        RayPerceptionSensorComponent3D m_rayPerceptionSensorComponent3D = transform.GetChild(6).GetComponent<RayPerceptionSensorComponent3D>();

        while (true)
        {
            var rayOutputs = RayPerceptionSensor.Perceive(m_rayPerceptionSensorComponent3D.GetRayPerceptionInput()).RayOutputs;
            int lengthOfRayOutputs = rayOutputs.Length;

            ThiefVisible = false;
            for (int i = 0; i < lengthOfRayOutputs; i++)
            {
                if (rayOutputs[i].HitTagIndex == 2)
                {
                    ThiefVisible = true;
                    Thief = rayOutputs[i].HitGameObject;
                    break;
                }
            }
            yield return new WaitForSeconds(1);
        }
    }
}
