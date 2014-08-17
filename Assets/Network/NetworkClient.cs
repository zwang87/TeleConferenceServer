using UnityEngine;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using System;
using System.IO;


public class NetworkClient : MonoBehaviour 
{
	#region Private Variables
	Vector3[] _Vertices; // Replace with appropriate vertices used with kinect;
	#endregion
	
	#region Network Variables
	private IPEndPoint serveripep;
	
	private Socket server;
	
	private NetworkStream nsSend;
	private NetworkStream nsReceive;
	
	private bool isServerConnected;
	
	byte[] dataToReceive;
	#endregion
	
	#region Public Variables
	public string serverIP;
	public float angleScale = 2.5f;
	#endregion
	
	#region Constructor and Destructor
	void Start () 
	{
		DataManager.Initialize();
		
		dataToReceive = new byte[DataManager.NetByteCount];
		
		isServerConnected = false;
		
		ConnectToServer ();
	}
	
	void Destroy()
	{
		nsSend.Close ();
		nsReceive.Close ();
		server.Disconnect (false);
	}
	#endregion
	
	#region Loop
	void Update () 
	{
		if(isServerConnected)
		{
			try
			{
				// Send First
				SendPositionInformation(); // Use this function if you want to send the position of an object to Server
				
				//SendDepthCloud(); // Use this function with vertices of the depth cloud if you want to send the depth cloud to server
				
				// Receive Next
				ReceivePositionInformation(); // Use this function if you want to receive the position of the server - Use with SendPositionInformation()
				
				//ReceiveDepthCloud(); // Use this to fetch the depth cloud of server and access vertices using DataManager.receivedData array - Use with SendDepthCloud()
				
				// Code to Create Server Mesh If any
			}
			catch (System.Exception ex)
			{
				isServerConnected = false;
				
				Debug.Log("Server Disconnected. Please restart server and client in the exact order. Error: " + ex.Message);
			}
		}
	}
	#endregion
	
	#region Methods
	private void ConnectToServer()
	{
		server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
		
		serveripep = new IPEndPoint(IPAddress.Parse(serverIP), 9050);
		
		try
		{
			server.Connect(serveripep);
			
			nsSend = new NetworkStream(server);
			nsReceive = new NetworkStream(server);
			
			isServerConnected = true;
		}
		catch(System.Exception ex)
		{
			Debug.Log(ex.Message);
		}
	}
	
	private void SendDepthCloud()
	{
		//_Vertices = depthSource.GetVertices ();
		//DataManager.MakeVertexDepthBytes(ref _Vertices);



		//nsSend.Write(DataManager.dataToSend, 0, DataManager.dataToSend.Length);
		
		//nsSend.Flush();
	}
	
	private void SendPositionInformation()
	{
		byte[] dataToSend = DataManager.MakeVector3Byte(GameObject.Find("HeadPosSent").transform.position);
		
		nsSend.Write(dataToSend, 0, dataToSend.Length);
		
		nsSend.Flush();
	}
	
	private void ReceiveDepthCloud()
	{
		nsReceive.Read(dataToReceive, 0, dataToReceive.Length);
		
		DataManager.BreakVertexDepthBytes(dataToReceive);
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
		
		float posX = 64.0f * headpos.x / widthX;
		float posY = 53.0f * headpos.y / heightY;
		
		
		Camera.main.transform.eulerAngles = new Vector3(angleScale * angleVert, -angleScale * anglePara, 0);
		Camera.main.transform.position = new Vector3(-posX, posY, headpos.z);
		
	}
	
	private void ReceivePositionInformation()
	{
		byte[] dataToReceive = new byte[DataManager.Vector3ByteLength];
		
		nsReceive.Read(dataToReceive, 0, dataToReceive.Length);
		
		Vector3 positionToSet = DataManager.BreakVector3Byte(dataToReceive);

		AdjustCameraPos (positionToSet);
	}
	#endregion
}
