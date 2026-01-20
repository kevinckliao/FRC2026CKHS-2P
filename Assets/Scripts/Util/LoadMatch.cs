using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using Util;
using UnityEngine.InputSystem;

[ExecuteAlways]
public class LoadMatch : MonoBehaviour
{
    [SerializeField] private GameObject[] fieldPrefab;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private Transform spawnPoint2; //kevin: added second spawn point

    [Header("Robot 1")]
    [SerializeField] private InspectorDropdown robotSelected;
    [SerializeField] private Cameras view;

    [Header("Robot 2")]
    [SerializeField] private InspectorDropdown robotSelected2;
    [SerializeField] private Cameras view2;

    private int selectedRobotIndex, selectedRobotIndex2;
    private string selectedName, selectedName2;
    private List<GameObject> availableRobots = new List<GameObject>();


    private GameObject _fieldHolder;
    private GameObject _activeRobot, _activeRobot2;
    private GameObject _1StCam, _2ndCam; //kevin: added second camera

    private FMS fms;

    private void OnEnable()
    {
        CheckRobots();
        robotSelected.canBeSelected = availableRobots.Select(x => x.name).ToList();
        robotSelected2.canBeSelected = availableRobots.Select(x => x.name).ToList(); //kevin: added second Robot Selecting
    }

    private void LateUpdate()
    {
        CheckRobots();
        robotSelected.canBeSelected = availableRobots.Select(x => x.name).ToList();
        robotSelected2.canBeSelected = availableRobots.Select(x => x.name).ToList(); //kevin: added

        robotSelected.selectedIndex = selectedRobotIndex;
        robotSelected2.selectedIndex = selectedRobotIndex; //kevin: added

        robotSelected.selectedName = selectedName;
        robotSelected2.selectedName = selectedName2; //kevin: added
    }

    private void Start()
    {
        selectedName = robotSelected.selectedName;
        selectedName2 = robotSelected2.selectedName; //kevin: added

        selectedRobotIndex = robotSelected.selectedIndex;
        selectedRobotIndex2 = robotSelected2.selectedIndex; //kevin: added

        CheckRobots();
        ResetField();
    }

    private void Update()
    {
        selectedName = robotSelected.selectedName;
        selectedName2 = robotSelected2.selectedName; //kevin: added

        selectedRobotIndex = robotSelected.selectedIndex;
        selectedRobotIndex2 = robotSelected2.selectedIndex; //kevin: added

        // Editor vs runtime-safe check to avoid referencing editor-only APIs in player builds
#if UNITY_EDITOR
        if (!UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode && RobotLoaded())
#else
        if (!Application.isPlaying && RobotLoaded())
#endif
        {
            DeleteRobot();
        }
        if (Application.isPlaying) return;

        if (!CheckField())
        {
            DestroyField();
            LoadField();
        }

        CheckRobots();
    }

    private void LoadField()
    {
        _fieldHolder = new GameObject
        {
            name = "FieldHolder",
            transform = { position = Vector3.zero, rotation = Quaternion.identity, parent = transform },

        };
        Instantiate(fieldPrefab[0], Vector3.zero, Quaternion.identity, _fieldHolder.transform);
    }

    private bool CheckField()
    {
        if (transform.childCount == 0)
        {
            return false;
        }
        else
        {
            return _fieldHolder.transform.Find(fieldPrefab[0].name + "(Clone)");
        }
    }

    private void DestroyField()
    {
        if (transform.Find("FieldHolder"))
        {
            _fieldHolder = transform.Find("FieldHolder").GameObject();
            DestroyImmediate(_fieldHolder);
        }
    }

    public void ResetField()
    {
        DestroyField();
        LoadField();
        SpawnRobot();
        addCamera();
        Utils.resetParentCache();
        if (fms)
        {
            fms.Restart();
        }
    }

    public void setFMS(FMS fms)
    {
        this.fms = fms;
    }

    public GameObject getFieldHolder()
    {
        return _fieldHolder;
    }

    private void SpawnRobot()
    {
        if (availableRobots.Count > 0 && selectedRobotIndex >= 0 && selectedRobotIndex < availableRobots.Count)
        {
            GameObject robotToSpawn = availableRobots[selectedRobotIndex];
            GameObject robotToSpawn2 = availableRobots[selectedRobotIndex2];

            _activeRobot  = Instantiate(robotToSpawn, spawnPoint.position, spawnPoint.rotation, _fieldHolder.transform);
            _activeRobot2 = Instantiate(robotToSpawn2, spawnPoint2.position, spawnPoint2.rotation, _fieldHolder.transform);
            
            var frame = _activeRobot.GetComponent<BuildFrame>();
            var frame2 = _activeRobot2.GetComponent<BuildFrame>();
            
            var controller  = frame.GetSwerveController(0);
            var controller2 = frame2.GetSwerveController(1);
            
            if (controller)
            {
                switch (view)
                {
                    case (Cameras.FirstPerson):
                        controller.reversed = false;
                        controller.fieldCentric = true; //kevin: changed to true for field centric
                        controller2.reversed = false;
                        controller2.fieldCentric = true; //kevin: changed to true for field centric
                        break;
                    case (Cameras.FirstPersonReversed):
                        controller.reversed = true;
                        controller.fieldCentric = false;
                        controller2.reversed = true;
                        controller2.fieldCentric = false;
                        break;
                    case (Cameras.ThirdPerson):
                        controller.reversed = false;
                        controller.fieldCentric = true;
                        controller2.reversed = false;
                        controller2.fieldCentric = true;
                        break;
                    case (Cameras.ReversedThirdPerson):
                        controller.reversed = true;
                        controller.fieldCentric = true;
                        controller2.reversed = true;
                        controller2.fieldCentric = true;
                        break;
                }
            }
        }
    }

    private bool RobotLoaded()
    {
        return _activeRobot != null;
    }

    public GameObject GetRobotLoaded()
    {
        return _activeRobot;
    }
    private void DeleteRobot()
    {
        DestroyImmediate(_activeRobot);
        DestroyImmediate(_activeRobot2);
    }

    private void addCamera()
    {
        //string objectToLoad = "Cameras/" + view.ToString();
        string objectToLoad = "Cameras/FirstPerson";
        _1StCam = Resources.Load(objectToLoad) as GameObject;
        var cam = Instantiate(_1StCam, Vector3.zero, spawnPoint.rotation, _activeRobot.transform); ;
        cam.transform.localPosition = Vector3.zero;

        string objectToLoad2 = "Cameras/FirstPerson2";
        _2ndCam = Resources.Load(objectToLoad2) as GameObject;
        var cam2 = Instantiate(_2ndCam, Vector3.zero, spawnPoint.rotation, _activeRobot2.transform); ;
        cam2.transform.localPosition = Vector3.zero;
    }


    public void CheckRobots()
    {
        GameObject[] loadedRobots = Resources.LoadAll<GameObject>("Robots");

        availableRobots.Clear();
        foreach (var robot in loadedRobots)
        {
            availableRobots.Add(robot);
        }

        if (selectedRobotIndex >= availableRobots.Count)
        {
            selectedRobotIndex = availableRobots.Count > 0 ? availableRobots.Count - 1 : 0;
        }
    }
}

