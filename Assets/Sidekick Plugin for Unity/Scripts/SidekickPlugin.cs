//------------------------------------------------------------------------------
//MIT License
//
//Copyright (c) 2023 AnimationPrepStudio
//
//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:
//
//The above copyright notice and this permission notice shall be included in all
//copies or substantial portions of the Software.
//
//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//SOFTWARE.
//-----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

/// <summary>
/// This script simply listes for packets streamed from the APS Sidekick face capture app and applise the data to a avatar face rig and blendshapes.
/// 
/// Add this script to a gameobject in the scene (eg. Facecap Head) then assign face renderers and face rig bones if any.
/// Download the APS Sidekick face capture app from Apple App store: https://apps.apple.com/us/app/aps-sidekick/id1536328156
/// Start the Sidekick app and ensure iPhone and PC are on the same network and are able to ping one another.
///
/// There are three example scenes in the Sidekick Plugin for Unity example project:
/// SidekickPluginExample - Is a simple scene consisting of a MocapHead mesh object that tracks to the user and includes face capture blendshapes. 
/// SidekickPluginExample_Dragon_Simple - Shows how to link a advanced character using the animated MocapHead rig, and controls a dragon puppet avatar using rotation constraints and custom script to copy the target blendshape.
/// SidekickPluginExample_Dragon_Drones_Game - Is the same as above but with added game functionality to show how to use the Sidekick plugin in a real-world application.
///
/// For answers to questions or tech support please visit the Discord: https://discord.com/invite/ErZcKaQ
/// </summary>
public class SidekickPlugin : MonoBehaviour {
	
	private static SidekickPlugin _instance;
	public static SidekickPlugin Instance { get { return _instance; } }

	[Header("Facial Expressions (blendshapes):")]
	[Tooltip("This is where you can assign the mesh that contains the facial Blendshaps. Used for Facecap, Blinking, Emotes, Visems.\n\nImportant Note: Whenever changes are made you will need to press \"Re-Build Avatar Asset\" to update the asset prior to copying to APS's VR_MocapAssets folder.")]
	[SerializeField]
	public SkinnedMeshRenderer[] faceRenderers = new SkinnedMeshRenderer[1]; //The renderer that has the facecao blend shapes and any additional renderers such as eyebrows, jaw, teeth, hair

	
	[Header("Face Rig:")]
	public Transform eyeL;
	public Transform eyeR;
	

	[Header("Other Options:")]
	[Tooltip("Update the head location. Applies when AR head tracking is enable from the Sidekick app.")]
	public bool headTracking = true;

	[Space] 
	[Range(0.0F,0.99F)]
	public float headRotationSmoothing = 0.0f;
	[Range(0.0F,0.99F)]
	public float headLocationSmoothing = 0.0f;

	
	//Store the initial forward of the eye bone, because makehuman and reallusion rigs are different and these values are the coefficients used to correct the gaze forward for SR vive eye
	private Quaternion m_eyeR_InitialRotation;
	private Quaternion m_eyeR_InitialLocalRotation;
	private Quaternion eyeR_InitialRotation { get { return m_eyeR_InitialRotation; } }
	private Quaternion m_eyeL_InitialRotation;
	private Quaternion m_eyeL_InitialLocalRotation;
	private Quaternion eyeL_InitialRotation { get { return m_eyeL_InitialRotation; } }

	private Transform eyeLGlobalRotationTarget; //a transform that sits at the same position as the eye bone, but has it's rotation zeroed to the global rotation
	private Transform eyeRGlobalRotationTarget; //a transform that sits at the same position as the eye bone, but has it's rotation zeroed to the global rotation 
	
	public static IPEndPoint endPoint
	{
		get {
			return new IPEndPoint(IPAddress.Any, 9000);
		}
	}
	private UdpClient udpClient;
	
	private float[] floats = new float[124 / 2];
	Queue<Byte[]> data = new Queue<Byte[]>();


	private void Awake()
	{
		if (_instance != null && _instance != this)
		{
			Destroy(gameObject);
			return;
		}
		else
		{
			_instance = this;
		}
		
		DontDestroyOnLoad(this);
	}

	void Start() 
	{

		
		//store the initial gaze forwards now while the character is still in tpose
		if (eyeR != null)
		{
			m_eyeR_InitialRotation = eyeR.rotation;
			m_eyeR_InitialLocalRotation = eyeR.localRotation;
			
			eyeRGlobalRotationTarget = new GameObject("eyeRGlobalRotationTarget").transform;
			eyeRGlobalRotationTarget.parent = eyeR.parent;
			eyeRGlobalRotationTarget.position = eyeR.position;
			eyeRGlobalRotationTarget.rotation = Quaternion.identity;;
		}
		
		if (eyeL != null)
		{
			m_eyeL_InitialRotation = eyeL.rotation;
			m_eyeL_InitialLocalRotation = eyeL.localRotation;

			eyeLGlobalRotationTarget = new GameObject("eyeLGlobalRotationTarget").transform;
			eyeLGlobalRotationTarget.parent = eyeL.parent;
			eyeLGlobalRotationTarget.position = eyeL.position;
			eyeLGlobalRotationTarget.rotation = Quaternion.identity;
		}

		try
		{
			udpClient = new UdpClient(endPoint);
		}
		catch (Exception e)
		{
			udpClient = null;
			return;
		}
		
		StartListening();
	}

	private void StartListening()
	{
		udpClient.BeginReceive(Receive, new object());
	}
	
	private void Receive(IAsyncResult ar)
	{
		var remoteIPRef = endPoint;
		
		data.Clear();
		data.Enqueue( udpClient.EndReceive(ar, ref remoteIPRef));
		
		StartListening();
	}
	
	void LateUpdate()
	{
		if (data.Count == 0)
			return;
		
		byte[] bytes = null;

		bytes = data.Dequeue();
		
		for (int n = 0; n < bytes.Length; n += 2)
		{
			var uInt8Value1 = (byte) bytes[n];
			var uInt8Value0 = (byte) bytes[n + 1];

			floats[n / 2] = IEEE754( uInt8Value1, uInt8Value0);
		}
		
		switch (bytes.Length) {
			case 104: //blendshapes
				ExtractBlendshapeData(floats);
				break;
			case 112: //blendshapes, gaze
				ExtractBlendshapeData(floats);
				ExtractEyeRotationData(floats);
				break;
			case 124: //blendshapes, gaze, head tracking
				ExtractBlendshapeData(floats);
				ExtractEyeRotationData(floats);
				ExtractHeadTransformData(floats);
				break;
		}
	}

	public void OnDisable()
	{
		try
		{
			if (udpClient != null)
			{
				udpClient.Close();
				udpClient.Dispose();
			}
			
			udpClient = null;
			Debug.Log("UDP connection closed.");
		}
		catch (Exception e) { 
			Debug.LogError ("UDB connection error: " + e.Message);
		}
	}
	
	void ExtractBlendshapeData(float[] values)
	{
		for (int i = 0; i < 52; i++)
			foreach (var faceRenderer in faceRenderers)
				faceRenderer.SetBlendShapeWeight(i,  values[i]);
	}

	void ExtractEyeRotationData(float[] values)
	{
		LeftEyeEulerAnglesReceived( new Vector2(values[52], values[53]));
		RightEyeEulerAnglesReceived( new Vector2(values[54], values[55]));
	}
	
	void ExtractHeadTransformData(float[] values)
	{
		const bool MirrorHeadLocation = false;
		if (MirrorHeadLocation)
		{
			transform.localRotation = Quaternion.Lerp(Quaternion.Euler(-values[56], -values[58], values[57]), transform.localRotation, headRotationSmoothing);
			if (headTracking) transform.localPosition = Vector3.Lerp(new Vector3(-values[61], values[60], values[59]), transform.localPosition, headLocationSmoothing);
		}
		else
		{
			transform.localRotation = Quaternion.Lerp(Quaternion.Euler(-values[56], values[58], -values[57]), transform.localRotation, headRotationSmoothing);
			if (headTracking) transform.localPosition = Vector3.Lerp(new Vector3(values[61], values[60], values[59]), transform.localPosition, headLocationSmoothing);
		}
	}
	
	public static float IEEE754(byte HO, byte LO)
	{
		//Half-precision floating point conversation according to IEEE 754-2008 standard.
		var intVal = BitConverter.ToInt32(new byte[] { HO, LO, 0, 0 }, 0);

		int mant = intVal & 0x03ff;
		int exp = intVal & 0x7c00;
		if (exp == 0x7c00) exp = 0x3fc00;
		else if (exp != 0)
		{
			exp += 0x1c000;
			if (mant == 0 && exp > 0x1c400)
				return BitConverter.ToSingle(BitConverter.GetBytes((intVal & 0x8000) << 16 | exp << 13 | 0x3ff), 0);
		}
		else if (mant != 0)
		{
			exp = 0x1c400;
			do
			{
				mant <<= 1;
				exp -= 0x400;
			} while ((mant & 0x400) == 0);
			mant &= 0x3ff;
		}
		return BitConverter.ToSingle(BitConverter.GetBytes((intVal & 0x8000) << 16 | (exp | mant) << 13), 0);
	}
	
	protected Quaternion ConvertRotationToUnitySpace(Vector3 eulerAngles)
	{
		Quaternion q = Quaternion.Euler(eulerAngles);
		return new Quaternion(-q.x, q.y, q.z, -q.w);
	}

	protected void LeftEyeEulerAnglesReceived(Vector2 value)
	{
		if (eyeL)
		{
			Vector3 target = eyeLGlobalRotationTarget.TransformPoint(Matrix4x4.Rotate(ConvertRotationToUnitySpace(value)).rotation * Vector3.forward);

			eyeL.LookAt(target, transform.up);
			eyeL.localRotation *= eyeL_InitialRotation;
			eyeL.localRotation = Quaternion.Slerp(m_eyeL_InitialLocalRotation,eyeL.localRotation,1.0f);
		}
	}

	protected void RightEyeEulerAnglesReceived(Vector2 value)
	{
		if (eyeR)
		{
			Vector3 target = eyeRGlobalRotationTarget.TransformPoint(Matrix4x4.Rotate(ConvertRotationToUnitySpace(value)).rotation * Vector3.forward);
		                    
			eyeR.LookAt(target, transform.up);
			eyeR.localRotation *= eyeR_InitialRotation;
			eyeR.localRotation = Quaternion.Slerp(m_eyeR_InitialLocalRotation,eyeR.localRotation,1.0f);
		}
	}
	
}

