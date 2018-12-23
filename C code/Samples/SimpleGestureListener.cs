using UnityEngine;
using System.Collections;
using System;

public class SimpleGestureListener : MonoBehaviour, KinectGestures.GestureListenerInterface
{
	// GUI Text to display the gesture messages.
	public GUIText GestureInfo;
	
	// private bool to track if progress message has been displayed
	private bool progressDisplayed;

    private bool swipeLeft;
    private bool swipeRight;
    private bool push;
    private bool pull;


    public bool IsSwipeLeft()
    {
        if (swipeLeft)
        {
            swipeLeft = false;
            return true;
        }

        return false;
    }

    public bool IsSwipeRight()
    {
        if (swipeRight)
        {
            swipeRight = false;
            return true;
        }

        return false;
    }

    public bool IsPush()
    {
        if (push)
        {
            push = false;
            return true;
        }

        return false;
    }

    public bool IsPull()
    {
        if (pull)
        {
            pull = false;
            return true;
        }

        return false;
    }

    public void UserDetected(uint userId, int userIndex)
	{
		// as an example - detect these user specific gestures
		KinectManager manager = KinectManager.Instance;

		//manager.DetectGesture(userId, KinectGestures.Gestures.Jump);
		//manager.DetectGesture(userId, KinectGestures.Gestures.Squat);

        manager.DetectGesture(userId, KinectGestures.Gestures.SwipeLeft);
        manager.DetectGesture(userId, KinectGestures.Gestures.SwipeRight);

        //manager.DeleteGesture(userId, KinectGestures.Gestures.ZoomIn);
        //manager.DeleteGesture(userId, KinectGestures.Gestures.ZoomOut);

        manager.DetectGesture(userId, KinectGestures.Gestures.Push);
        manager.DetectGesture(userId, KinectGestures.Gestures.Pull);

       // manager.DetectGesture(userId, KinectGestures.Gestures.SwipeUp);
        //manager.DetectGesture(userId, KinectGestures.Gestures.SwipeDown);

    }
	
	public void UserLost(uint userId, int userIndex)
	{
		if(GestureInfo != null)
		{
			GestureInfo.GetComponent<GUIText>().text = string.Empty;
		}
	}

	public void GestureInProgress(uint userId, int userIndex, KinectGestures.Gestures gesture, 
	                              float progress, KinectWrapper.NuiSkeletonPositionIndex joint, Vector3 screenPos)
	{
		//nada
	}

	public bool GestureCompleted (uint userId, int userIndex, KinectGestures.Gestures gesture, 
	                              KinectWrapper.NuiSkeletonPositionIndex joint, Vector3 screenPos)
	{
		string sGestureText = gesture + " detected simple";
		if(gesture == KinectGestures.Gestures.Click)
			sGestureText += string.Format(" at ({0:F1}, {1:F1}) simple", screenPos.x, screenPos.y);

        else if (gesture == KinectGestures.Gestures.SwipeLeft)
            swipeLeft = true;
        else if (gesture == KinectGestures.Gestures.SwipeRight)
            swipeRight = true;
        else if (gesture == KinectGestures.Gestures.Push)
            push = true;
        else if (gesture == KinectGestures.Gestures.Pull)
            pull = true;

        if (GestureInfo != null)
			GestureInfo.GetComponent<GUIText>().text = sGestureText;
		progressDisplayed = false;


        return true;
	}

	public bool GestureCancelled (uint userId, int userIndex, KinectGestures.Gestures gesture, 
	                              KinectWrapper.NuiSkeletonPositionIndex joint)
	{
		if(progressDisplayed)
		{
			// clear the progress info
			if(GestureInfo != null)
				GestureInfo.GetComponent<GUIText>().text = String.Empty;
			
			progressDisplayed = false;
		}
		
		return true;
	}
	
}
