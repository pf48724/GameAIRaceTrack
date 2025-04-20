/*
 * This code is part of Arcade Car Physics for Unity by Saarg (2018)
 * 
 * This is distributed under the MIT Licence (see LICENSE.md for details)
 * 
 * AIVehicle is based on WheelVehicle
 */
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Unity.MLAgents;
using Unity.MLAgents.Sensors;

using GameAI;
using Unity.MLAgents.Actuators;

namespace GameAICourse
{
    public class NNVehicle : AIVehicleNN
    {
        [Header("NN Stuff")]
        public float EpisodeLenSec = 360f;
        bool episodeEndPending = false;
        float episodeStart = 0f;

        float normSpdFactor = 1f / 100f;
        float lastDistTraveled = 0f;
        float lastDistSampleTime = 0f;

        public float DB_RewardTotal = 0f;

        protected override void Awake()
        {
            base.Awake();
            StudentName = "Prince Fodeke";
            IsPlayer = false;
        }

        protected override void Start()
        {
            base.Start();
            lastDistTraveled = pathTracker.totalDistanceTravelled;
            lastDistSampleTime = Time.timeSinceLevelLoad;
        }

        protected void Rewards()
        {
            var episodeTime = Time.timeSinceLevelLoad - episodeStart;

            if (episodeTime > EpisodeLenSec)
            {
                var r = 1f;
                AddReward(r);
                DB_RewardTotal += r;
                EndEpisode();
            }
            else
            {
                var r = pathTracker.totalDistanceTravelled - lastDistTraveled;
                var dt = Time.timeSinceLevelLoad - lastDistSampleTime;

                if (!Mathf.Approximately(dt, 0f))
                {
                    var normAverageSpeed = 3.6f * r / dt * normSpdFactor;
                    r = normAverageSpeed * 0.001f;
                    AddReward(r);
                    DB_RewardTotal += r;
                }

                lastDistTraveled = pathTracker.totalDistanceTravelled;
                lastDistSampleTime = Time.timeSinceLevelLoad;
            }
        }

        public override void OnEpisodeBegin()
        {
            episodeStart = Time.timeSinceLevelLoad;
            lastDistTraveled = pathTracker.totalDistanceTravelled;
            lastDistSampleTime = episodeStart;
            DB_RewardTotal = 0f;
            episodeEndPending = false;
        }

        protected void NoPunishResetCar()
        {
            base.ResetCar();
            Throttle = 0f;
            Steering = 0f;
            EndEpisode();
        }

        protected override void ResetCar()
        {
            base.ResetCar();
            Debug.Log("RESET CAR");
            Throttle = 0f;
            Steering = 0f;

            var v = -1f;
            AddReward(v);
            DB_RewardTotal += v;
            EndEpisode();
        }

        public override void CollectObservations(VectorSensor sensor)
        {
            Rewards();
        }

        public override void OnActionReceived(ActionBuffers actions)
        {
            var vectorAction = actions.ContinuousActions;
            Steering = vectorAction[0];
            Throttle = vectorAction[1];
        }

        public override void Heuristic(in ActionBuffers actionsOut)
        {
            var continuousActionsOut = actionsOut.ContinuousActions;
            continuousActionsOut[0] = Input.GetAxis("Horizontal");
            continuousActionsOut[1] = Input.GetAxis("Throttle") - Input.GetAxis("Brake");
        }

        override protected void Update()
        {
            if(episodeEndPending)
            {
                NoPunishResetCar();
                episodeEndPending = false;
            }
            base.Update();
        }
    }
}
