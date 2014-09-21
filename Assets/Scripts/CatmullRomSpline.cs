﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CatmullRomSpline : MonoBehaviour {

	public bool isDebug;
	public UILabel debugLabel;
	public bool invert;
	public float deriveDelta;

	private float startTimestep;
	private float nodeTimeLimit;
	private bool isReady;

	public bool IsReady {
		get {
			return isReady;
		}
	}

	public float TimeLimit {
		get {
			return nodeTimeLimit;
		}
		set { Debug.Log("You are not allowed to change nodeTimeLimit!"); } //We don't want it to be changed!
	}

	internal class Node {
		internal GameObject go;
		internal Vector3 pos;
		internal Quaternion rot;
		internal float time;
		internal CatmullRomNode nodeScript;

		internal Node(GameObject g, Vector3 p) { go = g; pos = p; }
		internal Node(GameObject g, Vector3 p, float t) { go = g; pos = p; time = t; }
		internal Node(GameObject g, Vector3 p, Quaternion q, CatmullRomNode n) { go = g; pos = p; nodeScript = n; rot = q; }

		internal string ToString() {
			return "Pos :"+pos+",\nRot: "+rot+",\nTime: "+time;
		}

		internal string GetTime() { return time.ToString("f2"); }

		internal float Time {
			get {
				return time;
			}
			set {
				time = value;
				nodeScript.timeGiven = value;
			}
		}
	}

	public delegate void SplineUpdate(float limit);
    public static event SplineUpdate OnSplineUpdated; 

	List<Node> nodes = new List<Node>();

	void log(string msg) {
		if(isDebug) Debug.Log("Spline: "+msg);
	}

	void Awake() {
		isReady = false;
	}

	void PrintNodeTimes() {
		string report = "";
		for(int i = 0; i < (nodes.Count); i++) {
			report += "Node #"+i+": "+nodes[i].GetTime()+"| ";
			nodeTimeLimit = i * startTimestep;
		}
		nodeTimeLimit -= (startTimestep*2);
		isReady = true;
		report += "Limit: "+nodeTimeLimit;
		if(OnSplineUpdated != null) OnSplineUpdated(nodeTimeLimit);
		log(report);
	}

	public void AddNode(GameObject gameObj) {
		isReady = false;
		if(CheckExistingNode(gameObj) == true) return;
		nodes.Add(new Node(gameObj, gameObj.transform.position, gameObj.transform.rotation, gameObj.GetComponent<CatmullRomNode>()));

		RecalculateNodeTimes();
	}

	public void PushNode(GameObject gameObj) {
		isReady = false;
		nodes.Add(new Node(gameObj, gameObj.transform.position, gameObj.transform.rotation, gameObj.GetComponent<CatmullRomNode>()));
		nodes[nodes.Count-1].Time = nodes[nodes.Count-2].Time + startTimestep;
		PrintNodeTimes();
	}

	bool CheckExistingNode(GameObject gameObj) {
		bool isFound = false;
		for(int i = 0; i < nodes.Count; i++) {
			if(nodes[i].go == gameObj) { //Selected node is already in list
				UpdateNode(gameObj, i);
				log("Node at index "+i+" updated!");
				isFound = true;
			}
		}
		isReady = true;
		return isFound;
	}

	public void UpdateNode(GameObject gameObj, int index) {
		nodes[index].pos = gameObj.transform.position;
	}

	void RecalculateNodeTimes() {
		int nodesCount = nodes.Count;

		if(nodesCount < 4) {
			log("Spline too short! Unable to recalculate times and create spline. ");
		} 
		else {
			float timeStep = 0f;
			timeStep = 1.0f / (nodesCount - 3);
			startTimestep = timeStep;

			for(int i = 0; i < (nodesCount-2); i++) {
				nodes[i+1].Time = i * timeStep;
				Debug.Log(i*timeStep);
			}

			nodes[0].Time = -timeStep;
			nodes[nodes.Count - 1].Time = 1 + timeStep;

			log("Times recalculated.");
			PrintNodeTimes();
		}
	}

	int NearestNodeToTime(float _t) {
		for(int i = 0; i < (nodes.Count-2); i++) {
			if(nodes[i+1].time > _t) return i;
		}
		if(_t == nodeTimeLimit) return nodes.Count - 3; //Case when reaching almost least node in spline

		return -1;
	}

	public Quaternion GetRotAtTime(float t) {

		Quaternion rot = Quaternion.identity;
		int nearestNodeIndex = NearestNodeToTime(t);
		if(nearestNodeIndex < 0) {
			log("Nearest node not found for t = "+t+", aborting!");
			return Quaternion.identity;
		}
		try {

			t = (t - nodes[nearestNodeIndex].time) / (nodes[nearestNodeIndex+1].time - nodes[nearestNodeIndex].time); //T Conversion. Putting raw 0..1 input causes weird things

			Quaternion q0 = nodes[nearestNodeIndex-1].rot;
			Quaternion q1 = nodes[nearestNodeIndex].rot;
			Quaternion q2 = nodes[nearestNodeIndex+1].rot;
			Quaternion q3 = nodes[nearestNodeIndex+2].rot;

			Quaternion T1 = MathUtils.GetSquadIntermediate(q0, q1, q2);
			Quaternion T2 = MathUtils.GetSquadIntermediate(q1, q2, q3);

			rot = MathUtils.GetQuatSquad(t, q1, q2, T1, T2);
		}
		catch(System.Exception e) {
			Debug.Log("Unable to calculate quaternion. "+e);
		}

		return rot;
	}

	public void GetRotAtTime(float t, GameObject go) {
		Vector3 futurePos;
		if(t + deriveDelta < nodeTimeLimit) futurePos = GetPositionAtTime(t + deriveDelta);
		else futurePos = GetPositionAtTime(nodeTimeLimit);

		go.transform.LookAt(futurePos);
	}

	public Vector3 GetPositionAtTime(float t) {

		//if(!invert) t = 1 - t; //Inversion. Works only when spline is in 0..1 range

		Vector3 pos = Vector3.zero;
		int nearestNodeIndex = NearestNodeToTime(t);
		if(nearestNodeIndex < 0) {
			log("Nearest node not found for t = "+t+", aborting!");
			return Vector3.zero;
		}
			//log("Nearest node found: "+nearestNodeIndex);
			t = (t - nodes[nearestNodeIndex].time) / (nodes[nearestNodeIndex+1].time - nodes[nearestNodeIndex].time); //T Conversion. Putting raw 0..1 input causes weird things

			Vector3 p0 = nodes[nearestNodeIndex-1].pos;
			Vector3 p1 = nodes[nearestNodeIndex].pos;
			Vector3 p2 = nodes[nearestNodeIndex+1].pos;
			Vector3 p3 = nodes[nearestNodeIndex+2].pos;

			Vector3 tension1 = 2 * p1;
			Vector3 tension2 = (-p0 + p2) * t;
			Vector3 tension3 = ((2 * p0) - (5 * p1) + (4 * p2) - p3) * Mathf.Pow(t,2);
			Vector3 tension4 = (-p0 + (3 * p1) - (3 * p2) + p3) * Mathf.Pow(t,3);
		
			pos = 0.5f * (tension1 + tension2 + tension3 + tension4);

			if(isDebug) {
				debugLabel.text = "Nodes count: "+nodes.Count + "\n"+
				"Closest node index: "+nearestNodeIndex + "\n"+
				"Node details: "+nodes[nearestNodeIndex].ToString() + "\n" +
				"Pos_0: "+p0 + "\n"+
				"Pos_1: "+p1 + "\n"+
				"Pos_2: "+p2 + "\n"+
				"Pos_3: "+p3 + "\n"+
				"Tension_1: "+tension1 + "\n"+
				"Tension_2: "+tension2 + "\n"+
				"Tension_3: "+tension3 + "\n"+
				"Tension_4: "+tension4 + "\n"+
				"Result: "+ pos;
			
			}
		return pos;
	}
}