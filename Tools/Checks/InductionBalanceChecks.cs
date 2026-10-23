using System;
using System.Collections.Generic;
using UnityEngine;
using _10_Hyunwoo.Scripts;

// Run in the connected Editor:
// unity command script run --file Tools/Checks/InductionBalanceChecks.cs --entry InductionBalanceChecks.Run
public static class InductionBalanceChecks
{
    public static object Run()
    {
        var passed = new List<string>();
        void Check(bool condition, string name) { if (!condition) throw new Exception(name); passed.Add(name); }
        float Energy(float a, float b, float dt) => MagnetDragGenerator.CalculateGeneratedEnergy(a,b,0,1.5f,0.4f,dt,2f);
        Check(Energy(0,0,1)==0, "Stationary magnet generates zero");
        Check(Energy(-0.5f,0.5f,1)==0, "Uniform flux inside coil generates zero");
        Check(Energy(2,3,1)==0, "Motion outside field generates zero");
        Check(Energy(0.8f,1.2f,0)==0, "Zero delta time is safe");
        var slow=Energy(0.8f,1.2f,0.4f);
        var fast=Energy(0.8f,1.2f,0.2f);
        Check(Mathf.Abs(fast/slow-2f)<0.001f, "Same pass twice as fast produces twice the energy");
        Check(Mathf.Abs((fast/0.2f)/(slow/0.4f)-4f)<0.001f, "Twice the speed produces four times the power");
        Check(Mathf.Abs(Energy(-2,2,2)-Energy(2,-2,2))<0.0001f, "Both travel directions generate equal energy");
        float segments=0;
        for(var i=0;i<120;i++) segments+=Energy(-2+4f*i/120,-2+4f*(i+1)/120,2f/120);
        Check(Mathf.Abs(segments-Energy(-2,2,2))<0.0001f, "Full pass result independent of frame subdivision");
        Check(Energy(-2,2,2)>0, "Fast pass through both edges is counted");
        var crossingTimes = new[]{4f,2f,1f,0.5f,0.1f,1f/60f,1f/144f};
        var previousReward = 0f;
        foreach(var seconds in crossingTimes)
        {
            var crossingReward = Energy(-2,2,seconds);
            Check(crossingReward > previousReward, "Faster full traversal gives more energy at " + seconds + "s");
            Check(Mathf.Abs(crossingReward * seconds - 0.96f)<0.001f,
                "Full traversal reward scales with speed at " + seconds + "s");
            var stroke = new InductionStrokeTracker();
            Check(stroke.Advance(-2,2,seconds,-1.5f,1.5f,crossingReward,out var paid,out _)
                && Mathf.Approximately(paid,crossingReward), "Fast traversal settles entire reward at " + seconds + "s");
            previousReward = crossingReward;
        }
        var expected=new[]{1f,3f,6f,10f,15f,21f,28f};
        for(var i=0;i<expected.Length;i++) Check(StatSO.BasePowerAtLevel(i+1)==expected[i], "Sum from one to level "+(i+1)+" sets base power");
        Check(StatSO.BasePowerAtLevel(100)==28f, "Base power stops at level seven");
        var tracker = new InductionStrokeTracker();
        Check(!tracker.Advance(0,2,1,-1,1,0.3f,out _,out _), "Starting inside does not settle partial traversal");
        Check(!tracker.Advance(2,0,1,-1,1,0.25f,out _,out _), "Partial traversal stays pending");
        Check(tracker.Advance(0,-2,1,-1,1,0.25f,out var reward,out var duration), "Opposite endpoint settles once");
        Check(Mathf.Abs(reward-0.5f)<0.0001f && Mathf.Abs(duration-1f)<0.0001f, "Reward and duration include only area traversal");
        Check(!tracker.Advance(-2,-3,1,-1,1,0,out _,out _), "Continuing outside does not duplicate reward");
        Check(tracker.Advance(-3,3,3,-1,1,0.7f,out reward,out duration) && Mathf.Approximately(reward,0.7f), "Single-frame reverse full traversal settles");
        tracker.Reset();
        tracker.Advance(-2,0,1,-1,1,1,out _,out _);
        Check(!tracker.Advance(0,-0.2f,1,-1,1,1,out _,out _), "Reversing inside cancels attempt");
        Check(!tracker.Advance(-0.2f,2,1,-1,1,1,out _,out _), "Cancelled wiggle reward cannot be cashed out");
        tracker.Advance(2,0,1,-1,1,0.25f,out _,out _);
        tracker.Advance(0,0,2,-1,1,0,out _,out _);
        Check(tracker.Advance(0,-2,1,-1,1,0.25f,out reward,out duration) && Mathf.Abs(duration-3f)<0.0001f, "Pausing inside lowers traversal average WPS");
        return new { passed=passed.Count, checks=passed, baselinePassEnergy=Energy(-2,2,2) };
    }
}
