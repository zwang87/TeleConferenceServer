using UnityEngine;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using System;

public class NetworkServer : MonoBehaviour 
{
	#region Private Variables
	Vector3[] _Vertices; // Replace with appropriate vertices used with kinect;
	#endregion
	
	#region Network Related Variables
	private Socket newsock;
	private Socket client;
	
	private IPEndPoint serveripep;
	private IPEndPoint newclientipep;
	
	private NetworkStream nsSend;
	private NetworkStream nsReceive;
	
	private bool isClientConnected;
	
	byte[] dataToReceive;
	#endregion
	
	#region Public Variables
	public Transform clientCube;
	public float angleScale;
	#endregion
	
	#region Constructor
	void Start () 
	{
		DataManager.Initialize();
		
		dataToReceive = new byte[DataManager.NetByteCount];
		
		isClientConnected = false;
		
		StartServer ();
	}
	#endregion
	
	#region Loop
	void Update () 
	{
		if(isClientConnected)
		{
			try
			{
				// Send First
				SendPositionInformation(); // Use this function if you want to send the position of an object to Client
				
				//SendDepthCloud(); // Use this function with vertices of the depth cloud if you want to send the depth cloud to client
				
				// Receive Next
				ReceivePositionInformation(); // Use this function if you want to receive the position of the client - Use with SendPositionInformation()
				
				//ReceiveDepthCloud(); // Use this to fetch the depth cloud of client and access vertices using DataManager.receivedData array - Use with SendDepthCloud()
				
				// Code to Create Client Mesh If any
			}
			catch (System.Exception ex)
			{
				isClientConnected = false;
				
				Debug.Log("Client Disconnected. Please restart server and client in the exact order. Error: " + ex.Message);
			}
		}
	}
	#endregion
	
	#region Methods
	private void StartServer()
	{
		serveripep = new IPEndPoint(IPAddress.Any, 9050);
		
		newsock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
		
		newsock.Bind(serveripep);
		newsock.Listen(10);
		
		Debug.Log("Waiting for a client...");
		
		newsock.BeginAccept(AcceptConnection, null);
	}
	
	private void SendDepthCloud()
	{
		DataManager.MakeVertexDepthBytes(ref _Vertices);
		
		nsSend.Write(DataManager.dataToSend, 0, DataManager.dataToSend.Length);
		
		nsSend.Flush();
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
		
		float posX = 128.0f * headpos.x / widthX;
		float posY = 106.0f * headpos.y / heightY;
		
		Debug.Log (headpos.x + "  " + headpos.y + "  " + headpos.z + "  " + anglePara + "  " + angleVert);
		Camera.main.transform.eulerAngles = new Vector3(angleScale * angleVert+5f, angleScale * anglePara, 0);
		Camera.main.transform.position = new Vector3(-posX, posY+10f, headpos.z);
		
	}
	
	private void ReceivePositionInformation()
	{
		byte[] dataToReceive = new byte[DataManager.Vector3ByteLength];
		
		nsReceive.Read(dataToReceive, 0, dataToReceive.Length);
		
		Vector3 positionToSet = DataManager.BreakVector3Byte(dataToReceive);
		
		//clientCube.position = positionToSet;
		AdjustCameraPos(positionToSet);


	}
	#endregion
	
	#region Callbacks
	private void AcceptConnection(IAsyncResult ar)
	{
		client = newsock.EndAccept(ar);
		newclientipep = (IPEndPoint)client.RemoteEndPoint;
		
		nsSend = new NetworkStream(client);
		nsReceive = new NetworkStream (client);
		
		Debug.Log("Message received from " + newclientipep.Address + ":" + newclientipep.Port);
		
		isClientConnected = true;
	}
	#endregion
}
