using UnityEngine;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.IO;
using System.Text; 


[RequireComponent(typeof(Animator))]
public class AvatarController : MonoBehaviour
{	
    //Booleano que determina si las acciones de los personajes personajes son reflejadas (mirror).
    //por defecto el valor es falso
	public bool mirroredMovement = false;

    //Booleano que determina si el avatar puede moverse de manera vertical (?). 
    //por defecto el valor es falso.
	public bool verticalMovement = false;

    //velocidad en la que el avatar se mueve por la escena. Esta velocidad se multiplica por el movimiento 
    //(0.001f, por ejemplo, dividiendolo por 1000, el framerate de unity.
	protected int moveRate = 1;
	
    //Factor de suavidad (smooth) de slerp
	public float smoothFactor = 5f;

    // Determina si el offset debe estar reposicionado a las coordenadas del usuario, tal cual lo reporta el sensor.
    public bool offsetRelativeToSensor = false;
	

	// Nodo de raiz del cuerpo (bodyRoot) (Investigar)
	protected Transform bodyRoot;
	
    // Variable requerida si se quiere rotar el modelo en el espacio. 
	protected GameObject offsetNode;
	
    // Variable que sostiene todos los huesos. Inicializa del mismo tamaño a initialRotations.
	protected Transform[] bones;
	
    // Rotaciones de los huesos cuando empieza el tracking del kinect.
	protected Quaternion[] initialRotations;
	protected Quaternion[] initialLocalRotations;
	
    // Posición inicial y rotación de la transformación.
	protected Vector3 initialPosition;
	protected Quaternion initialRotation;
	
    // Calibración de variables del offset para la rotación de la posición del personaje.
	protected bool offsetCalibrated = false;
	protected float xOffset, yOffset, zOffset;

    // intancia privada del kinectManager
	protected KinectManager kinectManager;

    // variable que mejora el rendimiento por cache, ya que unity llama GetComponent<Transform()> cada vez que uno llama transform
	private Transform _transformCache;

    //abril 3
    public GameObject cubeman;

	public new Transform transform
	{
		get
		{
			if (!_transformCache) 
				_transformCache = base.transform;
			
			return _transformCache;
		}
	}
	
	public void Awake()
    {	
        // checkea un inicio doble.
		if(bones != null)
			return;
		
		// inicializa el arreglo de huesos
		bones = new Transform[22];
	
        // Rotaciones iniciales y direcciones de los huesos.
		initialRotations = new Quaternion[bones.Length];
		initialLocalRotations = new Quaternion[bones.Length];

        // Mapea los huesos a los puntos que el kinect rastrea.
		MapBones();

        // Variable que obtiene las rotaciones iniciales de los huesos.
		GetInitialRotations();
	}
	
	// Actualiza el avatar en cada frame.
    public void UpdateAvatar(uint UserID)
    {	
		if(!transform.gameObject.activeInHierarchy) 
			return;
		
		// Obtiene la instancia del kinectManager
		if(kinectManager == null)
		{
			kinectManager = KinectManager.Instance;
		}
		
		// mueve el avatar a la posición del kinect.
		MoveAvatar(UserID);

		for (var boneIndex = 0; boneIndex < bones.Length; boneIndex++)
		{
			if (!bones[boneIndex]) 
				continue;
			//Aqui se hace la seleccion del espejo
			if(boneIndex2JointMap.ContainsKey(boneIndex))
			{
				KinectWrapper.NuiSkeletonPositionIndex joint = !mirroredMovement ? boneIndex2JointMap[boneIndex] : boneIndex2MirrorJointMap[boneIndex];
				TransformBone(UserID, joint, boneIndex, !mirroredMovement);
			}
			else if(specIndex2JointMap.ContainsKey(boneIndex))
			{
				// Huesos especiales (claviculas)
				List<KinectWrapper.NuiSkeletonPositionIndex> alJoints = !mirroredMovement ? specIndex2JointMap[boneIndex] : specIndex2MirrorJointMap[boneIndex];
				
				if(alJoints.Count >= 2)
				{
					//Vector3 baseDir = alJoints[0].ToString().EndsWith("Left") ? Vector3.left : Vector3.right;
					//TransformSpecialBone(UserID, alJoints[0], alJoints[1], boneIndex, baseDir, !mirroredMovement);
				}
			}
		}
        //Vector3 jugadorPos = GameObject.FindWithTag("head").transform.position;
        //Camera.main.transform.position = new Vector3(jugadorPos.x, jugadorPos.y, jugadorPos.z);
        //Camera.main.transform.Rotate(GameObject.FindWithTag("head").transform.rotation.eulerAngles, Space.Self);
        //Camera.main.transform.localRotation = GameObject.FindWithTag("head").transform.rotation;

        //Camera.main.transform.rotation = GameObject.FindWithTag("head").transform.rotation;
    }

    // Ajusta los huesos a las posiciones y rotaciones iniciales
    public void ResetToInitialPosition()
	{	
		if(bones == null)
			return;
		
		if(offsetNode != null)
		{
			offsetNode.transform.rotation = Quaternion.identity;
		}
		else
		{
			transform.rotation = Quaternion.identity;
		}
		
        // Para cada hueso que ha sido definido, resetearlo a la posición inicial.
		for (int i = 0; i < bones.Length; i++)
		{
			if (bones[i] != null)
			{
				bones[i].rotation = initialRotations[i];
			}
		}
		
		if(bodyRoot != null)
		{
			bodyRoot.localPosition = Vector3.zero;
			bodyRoot.localRotation = Quaternion.identity;
		}
		
        // Restaurar la posición y rotación del offset
		if(offsetNode != null)
		{
			offsetNode.transform.position = initialPosition;
			offsetNode.transform.rotation = initialRotation;
		}
		else
		{
			transform.position = initialPosition;
			transform.rotation = initialRotation;
		}
	}
	
    // Función que se invoca en la calibración satisfactoria de un usuario.
	public void SuccessfulCalibration(uint userId)
	{
        // Resetea posiciones de modelos.
		if(offsetNode != null)
		{
			offsetNode.transform.rotation = initialRotation;
		}
		
        // re-calibra el offset de la posición
		offsetCalibrated = false;
	}
	
    // Aplicar la rotacion rastreada por el kinect a los joints.
	protected void TransformBone(uint userId, KinectWrapper.NuiSkeletonPositionIndex joint, int boneIndex, bool flip)
    {
		Transform boneTransform = bones[boneIndex];
		if(boneTransform == null || kinectManager == null)
			return;
		
		int iJoint = (int)joint;
		if(iJoint < 0)
			return;
		
        // Obtiene la orientacion de los Joints de kinect.
		Quaternion jointRotation = kinectManager.GetJointOrientation(userId, iJoint, flip);
		if(jointRotation == Quaternion.identity)
			return;
		
        // Transforma de manera suave (smooth) a la nueva rotación.
		Quaternion newRotation = Kinect2AvatarRot(jointRotation, boneIndex);
        //Camera.main.transform.rotation = jointRotation;
        if (smoothFactor != 0f)
        	boneTransform.rotation = Quaternion.Slerp(boneTransform.rotation, newRotation, smoothFactor * Time.deltaTime);
		else
			boneTransform.rotation = newRotation;
	}

    // Aplicar las rotaciones rastreadas por el kinect a una articulación especial
	protected void TransformSpecialBone(uint userId, KinectWrapper.NuiSkeletonPositionIndex joint, KinectWrapper.NuiSkeletonPositionIndex jointParent, int boneIndex, Vector3 baseDir, bool flip)
	{
		Transform boneTransform = bones[boneIndex];
		if(boneTransform == null || kinectManager == null)
			return;
		
		if(!kinectManager.IsJointTracked(userId, (int)joint) || 
		   !kinectManager.IsJointTracked(userId, (int)jointParent))
		{
			return;
		}
		
		Vector3 jointDir = kinectManager.GetDirectionBetweenJoints(userId, (int)jointParent, (int)joint, false, true);
		Quaternion jointRotation = jointDir != Vector3.zero ? Quaternion.FromToRotation(baseDir, jointDir) : Quaternion.identity;
		
//		if(!flip)
//		{
//			Vector3 mirroredAngles = jointRotation.eulerAngles;
//			mirroredAngles.y = -mirroredAngles.y;
//			mirroredAngles.z = -mirroredAngles.z;
//			
//			jointRotation = Quaternion.Euler(mirroredAngles);
//		}
		
		if(jointRotation != Quaternion.identity)
		{
            // Transicion suavizada (smooth) a la nueva rotación.
			Quaternion newRotation = Kinect2AvatarRot(jointRotation, boneIndex);
			
			if(smoothFactor != 0f)
				boneTransform.rotation = Quaternion.Slerp(boneTransform.rotation, newRotation, smoothFactor * Time.deltaTime);
			else
				boneTransform.rotation = newRotation;
		}
		
	}

    // Mueve el avatar en un espacio 3D - saca la posición rastreada de la espina (columna) y la aplica a la raiz.
    // Sólo saca posición, no rotación.
	protected void MoveAvatar(uint UserID)
	{
		if(bodyRoot == null || kinectManager == null)
			return;
		if(!kinectManager.IsJointTracked(UserID, (int)KinectWrapper.NuiSkeletonPositionIndex.HipCenter))
			return;
		
        // Obtener la posición del cuerpo y almacenarlo.
		Vector3 trans = kinectManager.GetUserPosition(UserID);
		
        // Si es la primera vez que se mueve el avatar, definir el offset, de resto ignorarlo.
		if (!offsetCalibrated)
		{
			offsetCalibrated = true;
			
            // PROBAR MIRROR MOVEMENT (imprimir en log).
			xOffset = !mirroredMovement ? trans.x * moveRate : -trans.x * moveRate;
			yOffset = trans.y * moveRate;
			zOffset = -trans.z * moveRate;
			
			if(offsetRelativeToSensor)
			{
				Vector3 cameraPos = Camera.main.transform.position;
				
				float yRelToAvatar = (offsetNode != null ? offsetNode.transform.position.y : transform.position.y) - cameraPos.y;
				Vector3 relativePos = new Vector3(trans.x * moveRate, yRelToAvatar, trans.z * moveRate);
				Vector3 offsetPos = cameraPos + relativePos;
				
				if(offsetNode != null)
				{
					offsetNode.transform.position = offsetPos;
				}
				else
				{
					transform.position = offsetPos;
				}
                
			}
		}
	
        // Transicionar de manera suave (Smooth) a nueva posición.
		Vector3 targetPos = Kinect2AvatarPos(trans, verticalMovement);

		if(smoothFactor != 0f)
			bodyRoot.localPosition = Vector3.Lerp(bodyRoot.localPosition, targetPos, smoothFactor * Time.deltaTime);
		else
			bodyRoot.localPosition = targetPos;

        //abril 3
        //cubeman.SendMessage("moveCubeMan", bodyRoot.position);
    }
	
    // Si los huesos a ser mapeados han sido declarados, mapear los huesos al modelo.
	protected virtual void MapBones()
	{
        // hacer el nodo offset (offsetNode) como padre de la transformación del modelo.
		offsetNode = new GameObject(name + "Ctrl") { layer = transform.gameObject.layer, tag = transform.gameObject.tag };
		offsetNode.transform.position = transform.position;
		offsetNode.transform.rotation = transform.rotation;
		offsetNode.transform.parent = transform.parent;
		
		transform.parent = offsetNode.transform;
		transform.localPosition = Vector3.zero;
		transform.localRotation = Quaternion.identity;
		

        // hacer la transformación del modelo como raiz del cuerpo (BodyRoot) INVESTIGAR.
		bodyRoot = transform;
		
		// get bone transforms from the animator component
		var animatorComponent = GetComponent<Animator>();
		
		for (int boneIndex = 0; boneIndex < bones.Length; boneIndex++)
		{
			if (!boneIndex2MecanimMap.ContainsKey(boneIndex)) 
				continue;
			
			bones[boneIndex] = animatorComponent.GetBoneTransform(boneIndex2MecanimMap[boneIndex]);
		}
	}
	
	// Capture the initial rotations of the bones
	protected void GetInitialRotations()
	{
		// save the initial rotation
		if(offsetNode != null)
		{
			initialPosition = offsetNode.transform.position;
			initialRotation = offsetNode.transform.rotation;
			
			offsetNode.transform.rotation = Quaternion.identity;
		}
		else
		{
			initialPosition = transform.position;
			initialRotation = transform.rotation;
			
			transform.rotation = Quaternion.identity;
		}
		
		for (int i = 0; i < bones.Length; i++)
		{
			if (bones[i] != null)
			{
				initialRotations[i] = bones[i].rotation; // * Quaternion.Inverse(initialRotation);
				initialLocalRotations[i] = bones[i].localRotation;
			}
		}
		
		// Restore the initial rotation
		if(offsetNode != null)
		{
			offsetNode.transform.rotation = initialRotation;
		}
		else
		{
			transform.rotation = initialRotation;
		}
	}
	
	// Converts kinect joint rotation to avatar joint rotation, depending on joint initial rotation and offset rotation
	protected Quaternion Kinect2AvatarRot(Quaternion jointRotation, int boneIndex)
	{
		// Apply the new rotation.
        Quaternion newRotation = jointRotation * initialRotations[boneIndex];
		
		//If an offset node is specified, combine the transform with its
		//orientation to essentially make the skeleton relative to the node
		if (offsetNode != null)
		{
			// Grab the total rotation by adding the Euler and offset's Euler.
			Vector3 totalRotation = newRotation.eulerAngles + offsetNode.transform.rotation.eulerAngles;
			// Grab our new rotation.
			newRotation = Quaternion.Euler(totalRotation);
		}
		
		return newRotation;
	}
	
	// Converts Kinect position to avatar skeleton position, depending on initial position, mirroring and move rate
	protected Vector3 Kinect2AvatarPos(Vector3 jointPosition, bool bMoveVertically)
	{
		float xPos;
		float yPos;
		float zPos;
		
		// If movement is mirrored, reverse it.
		if(!mirroredMovement)
			xPos = jointPosition.x * moveRate - xOffset;
		else
			xPos = -jointPosition.x * moveRate - xOffset;
		
		yPos = jointPosition.y * moveRate - yOffset;
		zPos = -jointPosition.z * moveRate - zOffset;
		
		// If we are tracking vertical movement, update the y. Otherwise leave it alone.
		Vector3 avatarJointPos = new Vector3(xPos, bMoveVertically ? yPos : 0f, zPos);
		
		return avatarJointPos;
	}
	
	// dictionaries to speed up bones' processing
	// the author of the terrific idea for kinect-joints to mecanim-bones mapping
	// along with its initial implementation, including following dictionary is
	// Mikhail Korchun (korchoon@gmail.com). Big thanks to this guy!
	private readonly Dictionary<int, HumanBodyBones> boneIndex2MecanimMap = new Dictionary<int, HumanBodyBones>
	{
		{0, HumanBodyBones.Hips},
		{1, HumanBodyBones.Spine},
		{2, HumanBodyBones.Neck},
		{3, HumanBodyBones.Head},
		
		{4, HumanBodyBones.LeftShoulder},
		{5, HumanBodyBones.LeftUpperArm},
		{6, HumanBodyBones.LeftLowerArm},
		{7, HumanBodyBones.LeftHand},
		{8, HumanBodyBones.LeftIndexProximal},

		{9, HumanBodyBones.RightShoulder},
		{10, HumanBodyBones.RightUpperArm},
		{11, HumanBodyBones.RightLowerArm},
		{12, HumanBodyBones.RightHand},
		{13, HumanBodyBones.RightIndexProximal},

		{14, HumanBodyBones.LeftUpperLeg},
		{15, HumanBodyBones.LeftLowerLeg},
		{16, HumanBodyBones.LeftFoot},
		{17, HumanBodyBones.LeftToes},
		
		{18, HumanBodyBones.RightUpperLeg},
		{19, HumanBodyBones.RightLowerLeg},
		{20, HumanBodyBones.RightFoot},
		{21, HumanBodyBones.RightToes},
	};
	// Esta es la caracterizacion de los esqueletos tanto normal como espejos
	protected readonly Dictionary<int, KinectWrapper.NuiSkeletonPositionIndex> boneIndex2JointMap = new Dictionary<int, KinectWrapper.NuiSkeletonPositionIndex>
	{
		{0, KinectWrapper.NuiSkeletonPositionIndex.HipCenter},
		{1, KinectWrapper.NuiSkeletonPositionIndex.Spine},
		{2, KinectWrapper.NuiSkeletonPositionIndex.ShoulderCenter},
		{3, KinectWrapper.NuiSkeletonPositionIndex.Head},
		
		{5, KinectWrapper.NuiSkeletonPositionIndex.ShoulderLeft},
		{6, KinectWrapper.NuiSkeletonPositionIndex.ElbowLeft},
		{7, KinectWrapper.NuiSkeletonPositionIndex.WristLeft},
		{8, KinectWrapper.NuiSkeletonPositionIndex.HandLeft},
		
		{10, KinectWrapper.NuiSkeletonPositionIndex.ShoulderRight},
		{11, KinectWrapper.NuiSkeletonPositionIndex.ElbowRight},
		{12, KinectWrapper.NuiSkeletonPositionIndex.WristRight},
		{13, KinectWrapper.NuiSkeletonPositionIndex.HandRight},
		
		{14, KinectWrapper.NuiSkeletonPositionIndex.HipLeft},
		{15, KinectWrapper.NuiSkeletonPositionIndex.KneeLeft},
		{16, KinectWrapper.NuiSkeletonPositionIndex.AnkleLeft},
		{17, KinectWrapper.NuiSkeletonPositionIndex.FootLeft},
		
		{18, KinectWrapper.NuiSkeletonPositionIndex.HipRight},
		{19, KinectWrapper.NuiSkeletonPositionIndex.KneeRight},
		{20, KinectWrapper.NuiSkeletonPositionIndex.AnkleRight},
		{21, KinectWrapper.NuiSkeletonPositionIndex.FootRight},
	};
	
	protected readonly Dictionary<int, List<KinectWrapper.NuiSkeletonPositionIndex>> specIndex2JointMap = new Dictionary<int, List<KinectWrapper.NuiSkeletonPositionIndex>>
	{
		{4, new List<KinectWrapper.NuiSkeletonPositionIndex> {KinectWrapper.NuiSkeletonPositionIndex.ShoulderLeft, KinectWrapper.NuiSkeletonPositionIndex.ShoulderCenter} },
		{9, new List<KinectWrapper.NuiSkeletonPositionIndex> {KinectWrapper.NuiSkeletonPositionIndex.ShoulderRight, KinectWrapper.NuiSkeletonPositionIndex.ShoulderCenter} },
	};
    // Esta es la caracterizacion de los esqueletos tanto normal como espejos
    protected readonly Dictionary<int, KinectWrapper.NuiSkeletonPositionIndex> boneIndex2MirrorJointMap = new Dictionary<int, KinectWrapper.NuiSkeletonPositionIndex>
	{
		{0, KinectWrapper.NuiSkeletonPositionIndex.HipCenter},
		{1, KinectWrapper.NuiSkeletonPositionIndex.Spine},
		{2, KinectWrapper.NuiSkeletonPositionIndex.ShoulderCenter},
		{3, KinectWrapper.NuiSkeletonPositionIndex.Head},
		
		{5, KinectWrapper.NuiSkeletonPositionIndex.ShoulderRight},
		{6, KinectWrapper.NuiSkeletonPositionIndex.ElbowRight},
		{7, KinectWrapper.NuiSkeletonPositionIndex.WristRight},
		{8, KinectWrapper.NuiSkeletonPositionIndex.HandRight},
		
		{10, KinectWrapper.NuiSkeletonPositionIndex.ShoulderLeft},
		{11, KinectWrapper.NuiSkeletonPositionIndex.ElbowLeft},
		{12, KinectWrapper.NuiSkeletonPositionIndex.WristLeft},
		{13, KinectWrapper.NuiSkeletonPositionIndex.HandLeft},
		
		{14, KinectWrapper.NuiSkeletonPositionIndex.HipRight},
		{15, KinectWrapper.NuiSkeletonPositionIndex.KneeRight},
		{16, KinectWrapper.NuiSkeletonPositionIndex.AnkleRight},
		{17, KinectWrapper.NuiSkeletonPositionIndex.FootRight},
		
		{18, KinectWrapper.NuiSkeletonPositionIndex.HipLeft},
		{19, KinectWrapper.NuiSkeletonPositionIndex.KneeLeft},
		{20, KinectWrapper.NuiSkeletonPositionIndex.AnkleLeft},
		{21, KinectWrapper.NuiSkeletonPositionIndex.FootLeft},
	};
	
	protected readonly Dictionary<int, List<KinectWrapper.NuiSkeletonPositionIndex>> specIndex2MirrorJointMap = new Dictionary<int, List<KinectWrapper.NuiSkeletonPositionIndex>>
	{
		{4, new List<KinectWrapper.NuiSkeletonPositionIndex> {KinectWrapper.NuiSkeletonPositionIndex.ShoulderRight, KinectWrapper.NuiSkeletonPositionIndex.ShoulderCenter} },
		{9, new List<KinectWrapper.NuiSkeletonPositionIndex> {KinectWrapper.NuiSkeletonPositionIndex.ShoulderLeft, KinectWrapper.NuiSkeletonPositionIndex.ShoulderCenter} },
	};
	
}

