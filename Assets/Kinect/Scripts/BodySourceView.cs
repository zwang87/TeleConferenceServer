using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Kinect = Windows.Kinect;




public class BodySourceView : MonoBehaviour 
{
	/// <summary>
	private Vector3 initHeadpos = new Vector3(0, 0, 0);
	private Vector3 headpos = new Vector3(0, 0, 0);
	private bool initialized = false;
	/// </summary>
	
	public Material BoneMaterial;
	public GameObject BodySourceManager;
	public float scale = 1000.0f;
	public Vector3 transformVec = new Vector3(0, 0, 0.50f);
	public float angleScale = 1.0f;
	
	private Dictionary<ulong, GameObject> _Bodies = new Dictionary<ulong, GameObject>();
	private BodySourceManager _BodyManager;
	
	private Dictionary<Kinect.JointType, Kinect.JointType> _BoneMap = new Dictionary<Kinect.JointType, Kinect.JointType>()
	{
		{ Kinect.JointType.FootLeft, Kinect.JointType.AnkleLeft },
		{ Kinect.JointType.AnkleLeft, Kinect.JointType.KneeLeft },
		{ Kinect.JointType.KneeLeft, Kinect.JointType.HipLeft },
		{ Kinect.JointType.HipLeft, Kinect.JointType.SpineBase },
		
		{ Kinect.JointType.FootRight, Kinect.JointType.AnkleRight },
		{ Kinect.JointType.AnkleRight, Kinect.JointType.KneeRight },
		{ Kinect.JointType.KneeRight, Kinect.JointType.HipRight },
		{ Kinect.JointType.HipRight, Kinect.JointType.SpineBase },
		
		{ Kinect.JointType.HandTipLeft, Kinect.JointType.HandLeft },
		{ Kinect.JointType.ThumbLeft, Kinect.JointType.HandLeft },
		{ Kinect.JointType.HandLeft, Kinect.JointType.WristLeft },
		{ Kinect.JointType.WristLeft, Kinect.JointType.ElbowLeft },
		{ Kinect.JointType.ElbowLeft, Kinect.JointType.ShoulderLeft },
		{ Kinect.JointType.ShoulderLeft, Kinect.JointType.SpineShoulder },
		
		{ Kinect.JointType.HandTipRight, Kinect.JointType.HandRight },
		{ Kinect.JointType.ThumbRight, Kinect.JointType.HandRight },
		{ Kinect.JointType.HandRight, Kinect.JointType.WristRight },
		{ Kinect.JointType.WristRight, Kinect.JointType.ElbowRight },
		{ Kinect.JointType.ElbowRight, Kinect.JointType.ShoulderRight },
		{ Kinect.JointType.ShoulderRight, Kinect.JointType.SpineShoulder },
		
		{ Kinect.JointType.SpineBase, Kinect.JointType.SpineMid },
		{ Kinect.JointType.SpineMid, Kinect.JointType.SpineShoulder },
		{ Kinect.JointType.SpineShoulder, Kinect.JointType.Neck },
		{ Kinect.JointType.Neck, Kinect.JointType.Head },
	};
	
	void Update () 
	{
		if (BodySourceManager == null)
		{
			return;
		}
		
		_BodyManager = BodySourceManager.GetComponent<BodySourceManager>();
		if (_BodyManager == null)
		{
			return;
		}
		
		Kinect.Body[] data = _BodyManager.GetData();
		if (data == null)
		{
			return;
		}
		
		List<ulong> trackedIds = new List<ulong>();
		foreach(var body in data)
		{
			if (body == null)
			{
				continue;
			}
			
			if(body.IsTracked)
			{
				trackedIds.Add (body.TrackingId);
			}
		}
		
		List<ulong> knownIds = new List<ulong>(_Bodies.Keys);
		
		// First delete untracked bodies
		foreach(ulong trackingId in knownIds)
		{
			if(!trackedIds.Contains(trackingId))
			{
				Destroy(_Bodies[trackingId]);
				_Bodies.Remove(trackingId);
			}
		}


		foreach(var body in data)
		{
			if (body == null)
			{
				continue;
			}
			
			if(body.IsTracked)
			{
				if(!_Bodies.ContainsKey(body.TrackingId))
				{
					_Bodies[body.TrackingId] = CreateBodyObject(body.TrackingId);
				}
				
				RefreshBodyObject(body, _Bodies[body.TrackingId]);
				break;
			}
		}

		/*
		Kinect.Body bodyData = data[0];
		if (bodyData != null) {
				if (bodyData.IsTracked) {
				if (!_Bodies.ContainsKey (bodyData.TrackingId)) {
										_Bodies [bodyData.TrackingId] = CreateBodyObject (bodyData.TrackingId);
								}
				RefreshBodyObject (bodyData, _Bodies [bodyData.TrackingId]);
						}
				}*/
	}
	
	private GameObject CreateBodyObject(ulong id)
	{
		GameObject body = new GameObject("Body:" + id);
		for (Kinect.JointType jt = Kinect.JointType.SpineBase; jt <= Kinect.JointType.HipLeft; jt++)//Kinect.JointType.ThumbRight; jt++)
		{

			GameObject jointObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
			
			LineRenderer lr = jointObj.AddComponent<LineRenderer>();
			lr.SetVertexCount(2);
			lr.material = BoneMaterial;
			lr.SetWidth(0.05f, 0.05f);
			
			jointObj.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
			jointObj.name = jt.ToString();
			jointObj.transform.parent = body.transform;
		}
		return body;
	}
	
	
	int flag = 0;
	private void RefreshBodyObject(Kinect.Body body, GameObject bodyObject)
	{
		for (Kinect.JointType jt = Kinect.JointType.SpineBase; jt <= Kinect.JointType.HipLeft; jt++)//Kinect.JointType.ThumbRight; jt++)
		{
			Kinect.Joint sourceJoint = body.Joints[jt];
			Kinect.Joint? targetJoint = null;
			
			if(_BoneMap.ContainsKey(jt))
			{
				targetJoint = body.Joints[_BoneMap[jt]];
			}
			
			Transform jointObj = bodyObject.transform.FindChild(jt.ToString());
			jointObj.localPosition = GetVector3FromJoint(sourceJoint);
			LineRenderer lr = jointObj.GetComponent<LineRenderer>();
			if(targetJoint.HasValue)
			{
				lr.SetPosition(0, jointObj.localPosition);
				lr.SetPosition(1, GetVector3FromJoint(targetJoint.Value));
				lr.SetColors(GetColorForState (sourceJoint.TrackingState), GetColorForState(targetJoint.Value.TrackingState));
			}
			else
			{
				lr.enabled = false;
			}
			///////////////////////////////////////////////////////////////////////////////////////
			/*
			if( jt == Kinect.JointType.Head ){
				if(flag < 10)flag ++;
				else{
					if(!initialized){
						initHeadpos.x = sourceJoint.Position.X;
						initHeadpos.y = sourceJoint.Position.Y;
						initHeadpos.z = sourceJoint.Position.Z;
						initialized = true;
					}else{
						headpos.x = sourceJoint.Position.X-initHeadpos.x;
						headpos.y = sourceJoint.Position.Y-initHeadpos.y;
						headpos.z = sourceJoint.Position.Z-initHeadpos.z;
						
						
						headpos = headpos + transformVec;
						headpos *= scale;
						GameObject.Find("HeadPosSent").transform.position = new Vector3(headpos.x, headpos.y, headpos.z);

						AdjustCameraPos(headpos);
					}
				}
			}
			*/

			if(jt == Kinect.JointType.Head){
				headpos.x = sourceJoint.Position.X;
				headpos.y = sourceJoint.Position.Y;
				headpos.z = sourceJoint.Position.Z;
				headpos += transformVec;
				//headpos = headpos * scale;
				GameObject.Find("HeadPosSent").transform.position = new Vector3(headpos.x, headpos.y, headpos.z);
				GameObject.Find("Plane").transform.position = new Vector3(-33f, 50f, 100 * headpos.z - 35f);
				//AdjustCameraPos(headpos);
			}

		}
	}

	private void AdjustCameraPos(Vector3 headpos)
	{
		float radianPara = Mathf.Atan2(headpos.x, headpos.z);
		float anglePara = radianPara * Mathf.Rad2Deg;
		
		
		float radianVert = Mathf.Atan2(headpos.y, headpos.z);
		float angleVert = radianVert * Mathf.Rad2Deg;

		float halfXFOV = 35.3f * Mathf.Deg2Rad;
		float halfYFOV = 30f * Mathf.Deg2Rad;
		float widthX = headpos.z * Mathf.Tan (halfXFOV);
		float heightY = headpos.z * Mathf.Tan (halfYFOV);

		float posX = 128.0f * headpos.x / widthX;
		float posY = 106.0f * headpos.y / heightY;

		Debug.Log (headpos.x + "  " + headpos.y + "  " + headpos.z + "  " + anglePara + "  " + angleVert);
		Camera.main.transform.eulerAngles = new Vector3(angleScale * angleVert+5f, angleScale * anglePara, 0);
		Camera.main.transform.position = new Vector3(-posX, posY+10f, headpos.z);
		
	}
	
	private static Color GetColorForState(Kinect.TrackingState state)
	{
		switch (state)
		{
		case Kinect.TrackingState.Tracked:
			return Color.green;
			
		case Kinect.TrackingState.Inferred:
			return Color.red;
			
		default:
			return Color.black;
		}
	}
	
	private static Vector3 GetVector3FromJoint(Kinect.Joint joint)
	{
		return new Vector3(joint.Position.X * 10, joint.Position.Y * 10, joint.Position.Z * 10);
	}
}
