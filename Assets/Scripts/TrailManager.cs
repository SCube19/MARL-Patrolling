using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

public class TrailManager : MonoBehaviour
{
    //1. Detect end of game and detect the number of wins for each group
    //2. When all have ended prepare the arenas
    //      - create a point at the current position of a thief, color accordingly
    //      - remove all agents but leave trails
    //3. Take a screenshot of every arena
    [SerializeField] private Arena arena;
    [SerializeField] private int nArenas;

    [SerializeField] private int targetDataPoints;
    private int currentDataPoints;

    private int thiefWins = 0;
    private int guardWins = 0;

    [SerializeField] private GameObject guardWinSphere;
    [SerializeField] private GameObject thiefWinSphere;

    [SerializeField] private GameObject guardTrailDump;
    [SerializeField] private GameObject guardSphereDump;
    [SerializeField] private GameObject thiefTrailDump;
    [SerializeField] private GameObject thiefSphereDump;

    private GameObject[] arenas;

    private int width = 1024;
    private int height = 1024;
    void Awake()
    {
        Camera cam = Camera.allCameras[0];
        if (cam.targetTexture == null)
            cam.targetTexture = new RenderTexture(width, height, 24);
        else
        {
            width = cam.targetTexture.width;
            height = cam.targetTexture.height;
        }
    }

    private void ResetArenas()
    {
        foreach (Transform child in guardTrailDump.transform)
        {
            Destroy(child.gameObject);
        }
        foreach (Transform child in guardSphereDump.transform)
        {
            Destroy(child.gameObject);
        }
        foreach (Transform child in thiefSphereDump.transform)
        {
            Destroy(child.gameObject);
        }
        foreach (Transform child in thiefTrailDump.transform)
        {
            Destroy(child.gameObject);
        }

        arena.gameObject.SetActive(true);
        for (int i = 0; i < nArenas; i++)
        {
            GameObject next = Instantiate(arena.gameObject, new (transform.position.x, 0.0f, i * (arena.ArenaSize + 1.0f)), new Quaternion());
            Destroy(arenas[i]);
            arenas[i] = next;
        }
        arena.gameObject.SetActive(false);
    }

    private void Start()
    {   
        arenas = new GameObject[nArenas];
        ResetArenas();
    }

    public void EndEpisode(Arena.EpisodeResult result, GameObject arena)
    {
        if (result == Arena.EpisodeResult.THIEF_CAUGHT)
            GuardWon(arena);
        else
            ThiefWon(arena);

        CleanArena(arena);
        if (thiefWins + guardWins != 0 && (thiefWins + guardWins) % nArenas == 0)
        {
            Time.timeScale = 0;
            StartCoroutine(TakeScreenshots());
        }
    }

    private void ThiefWon(GameObject arena)
    {
        thiefWins++;
        thiefWinSphere.SetActive(true);
        GameObject sphere = Instantiate(thiefWinSphere, arena.GetComponentInChildren<Thief>().transform.position, new Quaternion());
        sphere.transform.parent = thiefSphereDump.transform;        
        thiefWinSphere.SetActive(false);
    }

    private void GuardWon(GameObject arena)
    {
        guardWins++;
        guardWinSphere.SetActive(true);
        GameObject sphere = Instantiate(guardWinSphere, arena.GetComponentInChildren<Thief>().transform.position, new Quaternion());
        sphere.transform.parent = guardSphereDump.transform;  
        guardWinSphere.SetActive(false);
    }

    private IEnumerator TakeScreenshots()
    {   
        yield return new WaitForEndOfFrame();

        Debug.Log("Taking screenshots...");
       
        GameObject[] prizes = GameObject.FindGameObjectsWithTag("Prize");
        foreach (var prize in prizes)
            prize.SetActive(false);

        string[] paths = {
            Application.dataPath + "/Screenshots/spheres/thief/",
            Application.dataPath + "/Screenshots/trails/thief/",
            Application.dataPath + "/Screenshots/trails/guard/",
            Application.dataPath + "/Screenshots/spheres/guard/"
        };
        foreach (var path in paths)
            if(!System.IO.Directory.Exists(path))
                System.IO.Directory.CreateDirectory(path);

        foreach (GameObject arena in arenas)
        {
            PlaceCameraAt(arena);
            guardSphereDump.SetActive(false);
            guardTrailDump.SetActive(false);
            thiefTrailDump.SetActive(false);
            thiefSphereDump.SetActive(true);
            Capture(Application.dataPath + "/Screenshots/spheres/thief/" + currentDataPoints + ".png");

            guardSphereDump.SetActive(false);
            guardTrailDump.SetActive(false);
            thiefTrailDump.SetActive(true);
            thiefSphereDump.SetActive(false);
            Capture(Application.dataPath + "/Screenshots/trails/thief/" + currentDataPoints + ".png");

            guardSphereDump.SetActive(false);
            guardTrailDump.SetActive(true);
            thiefTrailDump.SetActive(false);
            thiefSphereDump.SetActive(false);
            Capture(Application.dataPath + "/Screenshots/trails/guard/" + currentDataPoints + ".png");

            guardSphereDump.SetActive(true);
            guardTrailDump.SetActive(false);
            thiefTrailDump.SetActive(false);
            thiefSphereDump.SetActive(false);
            Capture(Application.dataPath + "/Screenshots/spheres/guard/" + currentDataPoints + ".png");

            currentDataPoints++;
        }

       

        if (currentDataPoints >= targetDataPoints)
        {
            Debug.Log("Guard wins: " + guardWins);
            Debug.Log("Thief wins: " + thiefWins);

            PlaceCameraAt(arenas[0]);
            guardSphereDump.SetActive(false);
            guardTrailDump.SetActive(false);
            thiefTrailDump.SetActive(false);
            thiefSphereDump.SetActive(false);
            Capture(Application.dataPath + "/Screenshots/background.png");

            UnityEditor.EditorApplication.isPlaying = false;
        }
        else
        {
            Debug.Log(currentDataPoints + " shots taken");
        }
        foreach (var prize in prizes)
            prize.SetActive(true);
            
        Time.timeScale = 1;
        ResetArenas();
    }

    private void PlaceCameraAt(GameObject arena)
    {
        Camera.allCameras[0].transform.position = arena.transform.Find("Plane").position;
        Camera.allCameras[0].transform.position += new Vector3(0, arena.GetComponent<Arena>().EncompassingCameraHeight, 0);
    }

    private void Capture(string path)
    {
        Camera cam = Camera.allCameras[0];

        Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);
        cam.Render();
        RenderTexture.active = cam.targetTexture;
        tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        byte[] bytes = tex.EncodeToPNG();
        System.IO.File.WriteAllBytes(path, bytes);
    }

    private void CleanArena(GameObject arena)
    {
        arena.GetComponentInChildren<Thief>().GetComponentInChildren<TrailRenderer>().transform.parent = thiefTrailDump.transform;

        arena.GetComponentInChildren<Thief>().gameObject.SetActive(false);
        foreach (var guard in arena.GetComponentsInChildren<Guard>())
        {
            guard.GetComponentInChildren<TrailRenderer>().transform.parent = guardTrailDump.transform;
            guard.gameObject.SetActive(false);
        }
    }
}
