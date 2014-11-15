﻿using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class Debugger : MonoBehaviour {

	public bool isDebug;
	public Text debuggerText;
	public ContinousMovement movement;

	public  float updateInterval = 0.5F;
 
	private float accum   = 0; 
	private int   frames  = 0; 
	private float timeleft; 
	private string format;
	private float fps;

	void Start () {

		Screen.sleepTimeout = SleepTimeout.NeverSleep;

		timeleft = updateInterval;  

		if(isDebug) {
			debuggerText.color = Color.white;
			StartCoroutine("RenderText");
		}
	}
	
	private IEnumerator RenderText() {
		while(true) {
			debuggerText.text = "x: " + movement.dir + "\n"
							+ "y: " + Mathf.Abs(Mathf.Atan2(Input.acceleration.y, Input.acceleration.z)*-1).ToString("f3") + "\n" 
							+ "fps: " + format + "\n"
							+ "accel: "+movement.accel;

			yield return new WaitForSeconds(0.1f);
		}
	}

	void Update()
	{
	    timeleft -= Time.deltaTime;
	    accum += Time.timeScale/Time.deltaTime;
	    ++frames;
	    if( timeleft <= 0.0 )
	    {
			fps = accum/frames;
			format = System.String.Format("{0:F2}",fps);
	        timeleft = updateInterval;
	        accum = 0.0F;
	        frames = 0;
	    }
	}
}