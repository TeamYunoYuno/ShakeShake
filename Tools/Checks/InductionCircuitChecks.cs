using System;
using System.Collections.Generic;
using UnityEngine;
public static class InductionCircuitChecks
{
 public static object Run(){
  var passed=new List<string>();
  void Check(bool c,string name){if(!c)throw new Exception(name);passed.Add(name);}
  float V(float a,float b,float dt,float power=1f)=>MagnetDragGenerator.CalculateInducedVoltage(a,b,0,1.5f,0.4f,dt,2,power,10);
  var entering=V(-1.5f,-0.6f,0.45f);var leaving=V(0.6f,1.5f,0.45f);
  Check(entering<0 && leaving>0,"Entering and leaving have opposite polarities");
  Check(Mathf.Abs(V(-0.6f,-1.5f,0.45f)+entering)<0.0001f,"Reversing movement reverses voltage");
  Check(V(0,0,0.1f)==0 && V(-0.2f,0.2f,0.1f)==0,"Stationary and uniform flux produce zero voltage");
  Check(V(2,3,0.1f)==0,"Outside field produces zero voltage");
  Check(Mathf.Abs(V(-1.5f,-0.6f,0.225f)/entering-2)<0.001f,"Twice the speed doubles voltage and current");
  var energy=MagnetDragGenerator.CalculateGeneratedEnergy(-1.5f,-0.6f,0,1.5f,0.4f,0.45f,2);
  Check(Mathf.Abs(entering*entering/10-energy/0.45f)<0.0001f,"V times I matches generated circuit power");
  Check(Mathf.Abs(V(-1.5f,-0.6f,0.45f,3)/entering-Mathf.Sqrt(3))<0.0001f,"Base power upgrade updates voltage consistently");
  Check(V(-1.5f,-0.6f,0)==0,"Zero duration is safe");
  return new{checks=passed,referenceVoltage=Mathf.Abs(entering),referenceCurrent=Mathf.Abs(entering)/10};
 }
}
