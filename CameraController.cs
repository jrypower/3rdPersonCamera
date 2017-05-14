using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Rewired;

[RequireComponent(typeof (Camera))]
public class CameraController : MonoBehaviour
{
    // Debug 
    [SerializeField]
    private Texture mCrossHairTex;

    // Constants
    private static readonly float MAX_INPUT_ANGLE = 1.0f;
    private static readonly float MIN_INPUT_ANGLE = -1.0f;

    [SerializeField]
    private float mVerticalAimingSpeed = 600.0f; // Degrees Per Second?
    [SerializeField]
    private float mHorizontalAimingSpeed = 600.0f; // Degrees Per Second?

    [SerializeField]
    private float mMaxVerticalAngle = 50.0f;
    [SerializeField]
    private float mMinVerticalAngle = -50.0f;

    [SerializeField]
    private float mMinXAxisInput = 0.1f;

    [SerializeField]
    private float mMinYAxisInput = 0.1f;

    [SerializeField]
    private Vector3 mCamOffset = new Vector3(2.0f, 2.0f, -2.0f);

    [SerializeField]
    private Vector3 mMinCamOffset = new Vector3(0.0f, 0.0f, 0.0f);

    [SerializeField]
    private float mMinDistFromObstruction = 0.2f;

    [SerializeField]
    private bool mInvertX = false;
    [SerializeField]
    private bool mInvertY = false;

    [SerializeField]
    private Transform mTarget;

    private float mCurHorizAngle = 0.0f;
    private float mCurVertAngle = 0.0f;

    private Camera mCamera;
    private LayerMask mMask;

    void Awake()
    {
        mMask = 1 << mTarget.gameObject.layer;
        // Add Igbore Raycast layer to mask
        mMask |= 1 << LayerMask.NameToLayer("Ignore Raycast");
        // Invert mask
        mMask = ~mMask;

        mCamera = GetComponent<Camera>();
    }

    void LateUpdate()
    {
        float horiz = ReInput.players.GetPlayer(0).GetAxis(RewiredConsts.Action.LookHorizontal);
        float vert = ReInput.players.GetPlayer(0).GetAxis(RewiredConsts.Action.LookVertical);

        // Dead Zones
        if(Mathf.Abs(horiz) <= mMinXAxisInput)
        {
            horiz = 0.0f;
        }

        if (Mathf.Abs(vert) <= mMinYAxisInput)
        {
            vert = 0.0f;
        }

        mCurHorizAngle += Mathf.Clamp(horiz, MIN_INPUT_ANGLE, MAX_INPUT_ANGLE) * mHorizontalAimingSpeed * Time.deltaTime;
        mCurVertAngle += Mathf.Clamp(vert, MIN_INPUT_ANGLE, MAX_INPUT_ANGLE) * mVerticalAimingSpeed * Time.deltaTime;

        mCurVertAngle = Mathf.Clamp(mCurVertAngle, mMinVerticalAngle, mMaxVerticalAngle);
        Quaternion camYRotation = Quaternion.Euler(0, mCurVertAngle, 0);
        Quaternion inputRotation = Quaternion.Euler((mInvertY ? 1.0f : -1.0f) * mCurVertAngle, (mInvertX ? 1.0f : -1.0f) * mCurHorizAngle, 0);

        mCamera.transform.rotation = inputRotation;

        Vector3 desiredCamPos = mTarget.position + inputRotation * mCamOffset; // This is the desired position of the camera offset by the player
        Vector3 minCamPos = mTarget.position + inputRotation * mMinCamOffset; // This is the minimum offset rotated by the input rotation away from the player.

        float camPosDiff = Vector3.Distance(desiredCamPos, minCamPos);

        Vector3 camDiffDistVect = (desiredCamPos - minCamPos) / camPosDiff; // Normalized
        Debug.DrawLine(minCamPos, minCamPos + camDiffDistVect, Color.blue);

        Vector3 lookTarget = Vector3.Project(mTarget.position - mCamera.transform.position, mCamera.transform.forward);
        Debug.DrawLine(mCamera.transform.position, mCamera.transform.position + lookTarget, Color.red, 0.2f);
        Vector3 playerToCamNormal = (mTarget.position - mCamera.transform.position).normalized;
        float playerToCamDist = Vector3.Distance(mTarget.position, mCamera.transform.position);
        Vector3 desiredPlayerToCamNormal = (mTarget.position - desiredCamPos).normalized;
        float desiredPlayerToCamDist = Vector3.Distance(mTarget.position, desiredCamPos);

        RaycastHit hit;
        if (Physics.Raycast(minCamPos, camDiffDistVect, out hit, camPosDiff, mMask))
        {
            mCamera.transform.position = minCamPos + (hit.distance - mMinDistFromObstruction) * camDiffDistVect;
        }
        else if (Physics.Raycast(mCamera.transform.position, playerToCamNormal, out hit, playerToCamDist, mMask))
        {
            mCamera.transform.position = new Vector3(mTarget.position.x, mCamera.transform.position.y, mTarget.position.z);
        }
        else if (!Physics.Raycast(desiredCamPos, desiredPlayerToCamNormal, out hit, desiredPlayerToCamDist, mMask))
        {
            mCamera.transform.position = desiredCamPos;
        }
        else
        {
            mCamera.transform.position = minCamPos;
        }
    }

    void OnGUI()
    {
        if (Time.time != 0 && Time.timeScale != 0)
            GUI.DrawTexture(new Rect(Screen.width / 2 - (mCrossHairTex.width * 0.5f), Screen.height / 2 - (mCrossHairTex.height * 0.5f), mCrossHairTex.width, mCrossHairTex.height), mCrossHairTex);
    }
}