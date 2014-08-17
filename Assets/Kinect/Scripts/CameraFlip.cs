using UnityEngine;
using System.Collections;

public class CameraFlip : MonoBehaviour {

	// Use this for initialization
	void Start () {
		Matrix4x4 mat = camera.projectionMatrix;
		mat *= Matrix4x4.Scale(new Vector3(-1, 1, 1));
		camera.projectionMatrix = mat;
		GL.SetRevertBackfacing (true);
	}
}
