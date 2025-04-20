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

using GameAI;

using Tochas.FuzzyLogic;
using Tochas.FuzzyLogic.MembershipFunctions;
using Tochas.FuzzyLogic.Evaluators;
using Tochas.FuzzyLogic.Mergers;
using Tochas.FuzzyLogic.Defuzzers;
using Tochas.FuzzyLogic.Expressions;

namespace GameAICourse
{
    public class FuzzyVehicle : AIVehicle
    {
        enum FzOutputThrottle {Brake, Coast, Accelerate }
        enum FzOutputWheel { TurnLeft, Straight, TurnRight }

        enum FzInputSpeed { Slow, Medium, Fast }

        enum FzInputDir { Left, Right, Straight}

        FuzzySet<FzInputSpeed> fzSpeedSet;

        FuzzySet<FzInputDir> fzDirSet;

        FuzzySet<FzOutputThrottle> fzThrottleSet;
        FuzzyRuleSet<FzOutputThrottle> fzThrottleRuleSet;

        FuzzySet<FzOutputWheel> fzWheelSet;
        FuzzyRuleSet<FzOutputWheel> fzWheelRuleSet;

        FuzzyValueSet fzInputValueSet = new FuzzyValueSet();

        FuzzyValueSet mergedThrottle = new FuzzyValueSet();
        FuzzyValueSet mergedWheel = new FuzzyValueSet();

        private FuzzySet<FzInputSpeed> GetSpeedSet()
        {
            FuzzySet<FzInputSpeed> set = new FuzzySet<FzInputSpeed>();
            IMembershipFunction slowFx = new ShoulderMembershipFunction(-100f, new Coords(40f, 1f), new Coords(65f, 0f), 100f);
            IMembershipFunction mediumFx = new TriangularMembershipFunction(new Coords(40f, 0f), new Coords(65f, 1f), new Coords(80f, 0f));
            IMembershipFunction fastFx = new ShoulderMembershipFunction(-100f, new Coords(65f, 0f), new Coords(80f, 1f), 100f);

            set.Set(FzInputSpeed.Slow, slowFx);
            set.Set(FzInputSpeed.Medium, mediumFx);
            set.Set(FzInputSpeed.Fast, fastFx);

            return set;
        }

        private FuzzySet<FzInputDir> GetDirSet()
        {
            FuzzySet<FzInputDir> set = new FuzzySet<FzInputDir>();
            IMembershipFunction leftFx = new ShoulderMembershipFunction(-180f, new Coords(-45f, 1f), new Coords(0f, 0f), 180f);
            IMembershipFunction straightFx = new TriangularMembershipFunction(new Coords(-45f, 0f), new Coords(0f, 1f), new Coords(45f, 0f));
            IMembershipFunction rightFx = new ShoulderMembershipFunction(-180f, new Coords(0f, 0f), new Coords(45f, 1f), 180f);

            set.Set(FzInputDir.Left, leftFx);
            set.Set(FzInputDir.Straight, straightFx);
            set.Set(FzInputDir.Right, rightFx);

            return set;
        }

        private FuzzySet<FzOutputThrottle> GetThrottleSet()
        {
            FuzzySet<FzOutputThrottle> set = new FuzzySet<FzOutputThrottle>();
            IMembershipFunction coastFx = new TriangularMembershipFunction(new Coords(-0.5f, 0f), new Coords(0f, 1f), new Coords(0.5f, 0f));
            IMembershipFunction brakeFx = new ShoulderMembershipFunction(-1f, new Coords(-0.5f, 1f), new Coords(0f, 0f), 1f);
            IMembershipFunction accelerateFx = new ShoulderMembershipFunction(-1f, new Coords(0f, 0f), new Coords(0.5f, 1f), 1f);

            set.Set(FzOutputThrottle.Coast, coastFx);
            set.Set(FzOutputThrottle.Brake, brakeFx);
            set.Set(FzOutputThrottle.Accelerate, accelerateFx);

            return set;
        }

        private FuzzySet<FzOutputWheel> GetWheelSet()
        {
            FuzzySet<FzOutputWheel> set = new FuzzySet<FzOutputWheel>();
            IMembershipFunction straightFx = new TriangularMembershipFunction(new Coords(-0.2f, 0f), new Coords(0f, 1f), new Coords(0.2f, 0f));
            IMembershipFunction turnLeftFx = new ShoulderMembershipFunction(-1f, new Coords(-0.2f, 1f), new Coords(0f, 0f), 100f);
            IMembershipFunction turnRightFx = new ShoulderMembershipFunction(-1f, new Coords(0f, 0f), new Coords(0.2f, 1f), 1f);

            set.Set(FzOutputWheel.Straight, straightFx);
            set.Set(FzOutputWheel.TurnLeft, turnLeftFx);
            set.Set(FzOutputWheel.TurnRight, turnRightFx);

            return set;
        }

        private FuzzyRule<FzOutputThrottle>[] GetThrottleRules()
        {
            FuzzyRule<FzOutputThrottle>[] rules =
            {
                If(FzInputSpeed.Slow).Then(FzOutputThrottle.Accelerate),
                If(FzInputSpeed.Medium).Then(FzOutputThrottle.Coast),
                If(FzInputSpeed.Fast).Then(FzOutputThrottle.Brake),
            };

            return rules;
        }

        private FuzzyRule<FzOutputWheel>[] GetWheelRules()
        {
            FuzzyRule<FzOutputWheel>[] rules =
            {
                If(FzInputDir.Left).Then(FzOutputWheel.TurnLeft),
                If(FzInputDir.Straight).Then(FzOutputWheel.Straight),
                If(FzInputDir.Right).Then(FzOutputWheel.TurnRight),
            };

            return rules;
        }

        private FuzzyRuleSet<FzOutputThrottle> GetThrottleRuleSet(FuzzySet<FzOutputThrottle> throttle)
        {
            var rules = this.GetThrottleRules();
            return new FuzzyRuleSet<FzOutputThrottle>(throttle, rules);
        }

        private FuzzyRuleSet<FzOutputWheel> GetWheelRuleSet(FuzzySet<FzOutputWheel> wheel)
        {
            var rules = this.GetWheelRules();
            return new FuzzyRuleSet<FzOutputWheel>(wheel, rules);
        }

        protected override void Awake()
        {
            base.Awake();
            StudentName = "Prince Fodeke";
            IsPlayer = false;
        }

        protected override void Start()
        {
            base.Start();

            fzSpeedSet = this.GetSpeedSet();
            fzDirSet = this.GetDirSet();
            fzThrottleSet = this.GetThrottleSet();
            fzThrottleRuleSet = this.GetThrottleRuleSet(fzThrottleSet);
            fzWheelSet = this.GetWheelSet();
            fzWheelRuleSet = this.GetWheelRuleSet(fzWheelSet);
        }

        System.Text.StringBuilder strBldr = new System.Text.StringBuilder();

        override protected void Update()
        {
            fzSpeedSet.Evaluate(Speed, fzInputValueSet);
            float angle = Vector3.SignedAngle(Velocity, pathTracker.pathCreator.path.GetDirectionAtDistance(40), Vector3.up);
            fzDirSet.Evaluate(angle, fzInputValueSet);

            ApplyFuzzyRules<FzOutputThrottle, FzOutputWheel>(
                fzThrottleRuleSet,
                fzWheelRuleSet,
                fzInputValueSet,
                out var throttleRuleOutput,
                out var wheelRuleOutput,
                ref mergedThrottle,
                ref mergedWheel
                );
            
            base.Update();
        }
    }
}
