﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public enum CurveMode { Line, Arc, Bezier }

public class BuildRoad : MonoBehaviour
{
    [SerializeField]
    InputHandler inputHandler;

    Type spawnType = typeof(Line);

    Lane currentLane;

    float highlightRadius = 2f;

    Stack<Command> commandSequence = new Stack<Command>();

    private void Start()
    {
        // Init behavior
        inputHandler.OnClick += delegate (object sender, Vector3 position) {
            Debug.Log("Onclick");
            
            if (currentLane == null)
            {
                Curve currentCurve = null;
                if (spawnType == typeof(Line))
                {
                    currentCurve = Line.GetDefault();
                }
                if (spawnType == typeof(Arc))
                {
                    currentCurve = Arc.GetDefault();
                }
                if (spawnType == typeof(Bezier))
                {
                    currentCurve = Bezier.GetDefault();
                }

                Function currentFunc = new LinearFunction(); // TODO: Create more
                currentLane = new Lane(currentCurve, currentFunc);
            }

            new PlaceEndingCommand(position).Execute(currentLane);

            if (currentLane.IsValid)
            {
                // Quit Init
                GetComponent<FollowMouseBehavior>().enabled = false;
                var placeCmd = new PlaceLaneCommand();
                commandSequence.Push(placeCmd);
                placeCmd.Execute(currentLane);
                currentLane.SetGameobjVisible(false);
                currentLane = null;
                GetComponent<HighLightCtrlPointBehavior>().radius = highlightRadius;
            }
            else
            {
                // Pending
                GetComponent<FollowMouseBehavior>().enabled = true;
                GetComponent<FollowMouseBehavior>().SetTarget(currentLane);
                GetComponent<HighLightCtrlPointBehavior>().radius = 0f;
            }

        };

        // Adjust behavior
        inputHandler.OnDragStart += delegate (object sender, Vector3 position)
        {
            Lane targetLane = RoadPositionRecords.QueryClosestCPs3DCurve(position);
            if (targetLane != null)
            {
                GetComponent<FollowMouseBehavior>().enabled = true;
                currentLane = new Lane(targetLane);
                GetComponent<FollowMouseBehavior>().SetTarget(currentLane);
                
                //replace targetLane with a temporary object (currentLane)
                var removeCmd = new RemoveLaneCommand();
                commandSequence.Push(removeCmd);
                removeCmd.Execute(targetLane);
                
                GetComponent<HighLightCtrlPointBehavior>().radius = 0f;
            }
            
        };

        inputHandler.OnDragEnd += delegate (object sender, Vector3 position)
        {
            if (currentLane != null)
            {
                GetComponent<FollowMouseBehavior>().enabled = false;
                GetComponent<HighLightCtrlPointBehavior>().radius = highlightRadius;

                // add actual lane to network
                var placeCmd = new PlaceLaneCommand();
                commandSequence.Push(placeCmd);
                placeCmd.Execute(currentLane);

                currentLane.SetGameobjVisible(false);
                currentLane = null;
            }
        };

        inputHandler.OnUndoPressed += delegate {
            var latestCmd = commandSequence.Pop();
            latestCmd.Undo();
        };

    }

    private void Update()
    {
        if (Input.GetKeyUp(KeyCode.Alpha1)){
            Debug.Log("Line mode");
            spawnType = typeof(Line);
        }

        if (Input.GetKeyUp(KeyCode.Alpha2))
        {
            Debug.Log("Arc mode");
            spawnType = typeof(Arc);
        }

        if (Input.GetKeyUp(KeyCode.Alpha3))
        {
            Debug.Log("Bezier mode");
            spawnType = typeof(Bezier);
        }

        if (Input.GetKeyUp(KeyCode.Q))
        {
            Debug.Log("Quit");
            currentLane = null;
        }


    }
}