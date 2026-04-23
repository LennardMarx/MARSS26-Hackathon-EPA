using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;


// Data container for each animation frame
[System.Serializable]
public class FrameData
{
    public GameObject heart;
    public GameObject pericardium;
}

public class MeshAnimator : MonoBehaviour
{
    public float animationDuration = 0.8f;
    public bool loop = true;

    // Toggle states for each mesh
    private bool showHeart = true;
    private bool showPericardium = true;


    // List of all frames (each frame holds separate heart & pericardium)
    private List<FrameData> frames = new List<FrameData>();

    private int frameCount = 20; // Number of frames (assuming 0-19)
    private int currentFrame = 0;
    private float frameTimer = 0f;
    private float frameInterval; // Duration per frame

    void Start()
    {
        frameInterval = animationDuration / frameCount;

        // Populate frames: each child numbered 0-19 is a frame
        for (int i = 0; i < frameCount; i++)
        {
            Transform child = transform.Find(i.ToString());
            if (child != null)
            {
                Transform heartT = child.Find("heart");
                Transform periT = child.Find("pericardium");

                if (heartT != null && periT != null)
                {
                    FrameData data = new FrameData();
                    // Assign the actual GameObject (instead of parent's GameObject)
                    data.heart = heartT.gameObject;
                    data.pericardium = periT.gameObject;

                    frames.Add(data);

                    // Optionally, disable the frame initially
                    data.heart.SetActive(false);
                    data.pericardium.SetActive(false);
                }
            }
        }

        if (frames.Count == 0)
        {
            Debug.LogWarning("No frames found!");
        }
    }

    void FixedUpdate()
    {
        if (frames.Count == 0)
            return;

        frameTimer += Time.fixedDeltaTime;

        if (frameTimer >= frameInterval)
        {
            // Disable previous frame
            DisableFrame(currentFrame);

            // Move to next frame
            currentFrame = (currentFrame + 1) % frames.Count;
            frameTimer = 0f;

            // Enable the new current frame based on toggles
            EnableFrame(currentFrame);
        }
    }

    // Enables heart and/or pericardium for a given frame index
    void EnableFrame(int index)
    {
        FrameData data = frames[index];
        if (showHeart && data.heart != null)
        {
            data.heart.SetActive(true);
        }
        if (showPericardium && data.pericardium != null)
        {
            data.pericardium.SetActive(true);
        }
    }

    // Disables both objects in a given frame index
    void DisableFrame(int index)
    {
        FrameData data = frames[index];
        if (data.heart != null)
        {
            data.heart.SetActive(false);
        }
        if (data.pericardium != null)
        {
            data.pericardium.SetActive(false);
        }
    }

    // Expose the current frame as a public property
    public FrameData CurrentFrame
    {
        get
        {
            if (frames != null && frames.Count > 0)
                return frames[currentFrame];
            return null;
        }
    }
}
