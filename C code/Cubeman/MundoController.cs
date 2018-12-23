using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System;

public class MundoController : MonoBehaviour {

    private SimpleGestureListener gestureListener;
    public GameObject cubeman;
    public GameObject mundoObjetivo;
    [Range(0, 90f)]
    public float anguloRotacion;
    [Range(0, 5f)]
    public float distMovimiento;

    //public Vector3 GradosRotacion;
    [Range(0,10f)]
    public float smooth = 0.5f;

    public Transform textoAngle;
    public Transform textoDistance;
    private bool rotating;


    //private Quaternion mundoRotation;
    //private Vector3 mundoPosition;


    public void Scroll_change_angle(float angleVal)
    {
        anguloRotacion = angleVal;
        textoAngle.GetComponent<Text>().text = ((float)angleVal).ToString();

    }

    public void Scroll_change_distancia(float distVal)
    {
        distMovimiento = distVal;
        textoDistance.GetComponent<Text>().text = ((float)distVal).ToString();

    }

    void Start () 
    {
        //GradosRotacion = new Vector3(0.0f,0.0f,0.0f);
        //mundoPosition = this.transform.position;
        //mundoRotation = this.transform.rotation;
        anguloRotacion = 45f;
        distMovimiento= 5f;
        mundoObjetivo.transform.position = this.transform.position;
        mundoObjetivo.transform.rotation = this.transform.rotation;
        gestureListener = Camera.main.GetComponent<SimpleGestureListener>();
        rotating = false;
	}


    private void Update()
    {
        if (gestureListener)
        {
            if (gestureListener.IsSwipeLeft())
            {
                Debug.Log("Left");
                //sLeft();
                //this.transform.RotateAround(cubeman.transform.position, new Vector3(0.0f, -90.0f, 0.0f), Time.deltaTime * smooth);
                //this.transform.RotateAround(cubeman.transform.position, Vector3.up, -180 * Time.deltaTime);

            }
            else if (gestureListener.IsSwipeRight())
            {
                Debug.Log("Right");
               // sRight();
                //this.transform.RotateAround(cubeman.transform.position, new Vector3(0.0f,90.0f,0.0f), Time.deltaTime * smooth);
                //this.transform.RotateAround(cubeman.transform.position, Vector3.up, -180 * Time.deltaTime);
            }
            else if (gestureListener.IsPush())
            {
                Debug.Log("Push");
                //sPush();
            }
            else if (gestureListener.IsPull())
            {
                Debug.Log("Pull");
                //sPull();
            }
        }
        this.transform.rotation = Quaternion.Slerp(this.transform.rotation, mundoObjetivo.transform.rotation, Time.deltaTime * smooth);
        this.transform.position = Vector3.Lerp(this.transform.position, mundoObjetivo.transform.position, Time.deltaTime * smooth);
    }

    private void sLeft()
    {
        //Vector3 gradosRot = new Vector3(0f,90f,0f);
        //GradosRotacion = new Vector3(0f, 90f, 0f);
        //mundoRotation *= Quaternion.Euler(gradosRot);
        //this.transform.Rotate(gradosRot);
        //this.transform.rotation = Quaternion.Slerp(this.transform.rotation, Quaternion.Euler(gradosRot), smooth * Time.deltaTime); // Esta linea no funciona bien por que no esta en el update
        //this.transform.rotation *= Quaternion.Euler(gradosRot);
        //this.transform.RotateAround(cubeman.transform.position, gradosRot,Time.deltaTime*smooth);
        //Rotate(this.transform, cubeman.transform, Vector3.up, 30.0f, 3.0f);

        mundoObjetivo.transform.RotateAround(cubeman.transform.position, Vector3.up, -anguloRotacion);
    }
    private void sRight()
    {
        // Vector3 gradosRot = new Vector3(0f, -90f, 0f);
        //GradosRotacion = new Vector3(0f, 90f, 0f);
        //mundoRotation *= Quaternion.Euler(gradosRot);
        //this.transform.Rotate(gradosRot);
        //this.transform.rotation = Quaternion.Slerp(this.transform.rotation, Quaternion.Euler(gradosRot), smooth * Time.deltaTime); // Esta linea no funciona bien por que no esta en el update
        //this.transform.rotation *= Quaternion.Euler(gradosRot);
        //Quaternion.Slerp(this.transform.rotation, destino.rotation, Time.deltaTime * smooth);
        //Rotate(this.transform, cubeman.transform, Vector3.up, -30.0f, 3.0f);

        mundoObjetivo.transform.RotateAround(cubeman.transform.position, Vector3.up, anguloRotacion);
    }
    private void sPush()
    {
        Vector3 distMov = new Vector3(0f, 0f, -distMovimiento);
        mundoObjetivo.transform.position += distMov;
    }
    private void sPull()
    {
        Vector3 distMov = new Vector3(0f,0f,distMovimiento);
        mundoObjetivo.transform.position += distMov;
    }

    private void Rotate(Transform thisTransform, Transform otherTransform, Vector3 rotateAxis , float degrees ,float  totalTime)
    {
        //if (rotating) return;
        rotating = true;


        //Quaternion startRotation = thisTransform.rotation;
        //Vector3 startPosition = thisTransform.position;
        thisTransform.RotateAround(otherTransform.position, rotateAxis, degrees);
        /*Quaternion endRotation = thisTransform.rotation;
        Vector3 endPosition = thisTransform.position;
        thisTransform.rotation = startRotation;
        thisTransform.position = startPosition;

        float rate = degrees / totalTime;
        for (float i = 0.0f; i < degrees; i += Time.deltaTime * rate)
        {
            thisTransform.RotateAround(otherTransform.position, rotateAxis, Time.deltaTime * rate);
        }

        thisTransform.rotation = endRotation;
        thisTransform.position = endPosition;*/
        rotating = false;
    }
}
