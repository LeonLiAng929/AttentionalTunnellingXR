using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System;

[System.Serializable]
public struct HazardLogEntry
{
    public float Timestamp;
    public string EventType; // Reacted, Missed, FalsePos
    public float TimeToReact;
    public Vector3 RawCamPos;
    public Quaternion RawCamRot;
    public Vector3 AlignedCamPos;
    public Vector3 HazardRawPos;
    public Vector3 AlignedHazardPos;
    public float FOV_HorizAngleToHazard;
    public float FOV_VertAngleToHazard;
    public string HazardProfile;
}

[System.Serializable]
public struct VisualLogEntry
{
    public float Timestamp;
    public string Line1Vals;
    public string Line2Vals;
    public int UserGuessLine;
    public int TruthLine;
    public float TimeTaken;
}

[System.Serializable]
public struct TelemetryLogEntry
{
    public float Timestamp;
    public Vector3 RawPos;
    public Quaternion RawRot;
    public Vector3 AlignedPos;
    public Quaternion AlignedRot;
    public Vector3 CamForward;
    public Vector3 AlignedCamForward;
}

public class CentralDataLogger : MonoBehaviour
{
    public static CentralDataLogger Instance { get; private set; }

    public int currentUserID;
    public int currentConditionIndex;
    public int currentConditionCode;

    public string sessionInfoString;

    private List<HazardLogEntry> hazardLog = new List<HazardLogEntry>();
    private List<VisualLogEntry> visualLog = new List<VisualLogEntry>();
    private List<TelemetryLogEntry> telemetryLog = new List<TelemetryLogEntry>();

    private string userInfoPath;
    private string studySchedulePath;

    public StudyFlowManager studyFlowManager;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;

        userInfoPath = Path.Combine(Application.persistentDataPath, "UserInfo.csv");
        studySchedulePath = Path.Combine(Application.persistentDataPath, "StudySchedule.csv");

        LoadConfigs();
    }

    private void Start()
    {
        if (studyFlowManager == null) studyFlowManager = FindObjectOfType<StudyFlowManager>();
        UpdateSessionInfo();
        Debug.Log(Application.persistentDataPath);
    }

    private void LoadConfigs()
    {
        // 1. Read UserInfo.csv
        if (!File.Exists(userInfoPath))
        {
            // Create default
            File.WriteAllText(userInfoPath, "UserID,CurrentConditionIndex\n1,-1\n");
            currentUserID = 1;
            currentConditionIndex = -1;
        }
        else
        {
            string[] lines = File.ReadAllLines(userInfoPath);
            if (lines.Length >= 2)
            {
                // Read from the last valid line to support history appending
                string lastLine = "";
                for (int i = lines.Length - 1; i >= 1; i--)
                {
                    if (!string.IsNullOrWhiteSpace(lines[i]))
                    {
                        lastLine = lines[i];
                        break;
                    }
                }

                if (!string.IsNullOrEmpty(lastLine))
                {
                    string[] parts = lastLine.Split(',');
                    if (parts.Length >= 2)
                    {
                        int.TryParse(parts[0], out currentUserID);
                        int.TryParse(parts[1], out currentConditionIndex);
                    }
                }
            }
        }

        // 2. Read StudySchedule.csv
        if (!File.Exists(studySchedulePath))
        {
            // Create default schedule
            File.WriteAllText(studySchedulePath, "UserID,Cond1,Cond2,Cond3,Cond4\n1,0,1,2,3\n2,1,2,3,0\n");
            currentConditionCode = 0; // Default fallback
        }
        else
        {
            string[] lines = File.ReadAllLines(studySchedulePath);
            bool foundUser = false;
            for (int i = 1; i < lines.Length; i++)
            {
                string[] parts = lines[i].Split(',');
                if (parts.Length >= 5 && int.TryParse(parts[0], out int uid) && uid == currentUserID)
                {
                    foundUser = true;
                    if (currentConditionIndex >= 0 && currentConditionIndex <= 3)
                    {
                        int.TryParse(parts[currentConditionIndex + 1], out currentConditionCode);
                    }
                    else
                    {
                        currentConditionCode = -1; // Trial mode or out of bounds
                    }
                    break;
                }
            }
            if (!foundUser) currentConditionCode = -1;
        }

        UpdateSessionInfo();
    }

    public void UpdateSessionInfo()
    {
        string state = studyFlowManager != null ? studyFlowManager.currentState.ToString() : "Unknown";
        sessionInfoString = $"UID: {currentUserID} | CondIndex: {currentConditionIndex} | Code: {currentConditionCode} | State: {state}";
        Debug.Log($"<color=orange>[CentralDataLogger] {sessionInfoString}</color>");
    }

    private bool ShouldLog()
    {
        if (currentConditionIndex == -1) return false;
        if (studyFlowManager == null || studyFlowManager.currentState != StudyFlowManager.StudyState.Running) return false;
        return true;
    }

    public void LogHazardEvent(string eventType, float timeToReact, Vector3 hazardRawPos, string hazardProfile)
    {
        if (!ShouldLog()) return;

        Transform cam = PlayerTelemetryManager.Instance != null ? PlayerTelemetryManager.Instance.centerCam : null;
        if (cam == null || SpatialAligner.Instance == null) return;

        HazardLogEntry entry = new HazardLogEntry
        {
            Timestamp = studyFlowManager != null ? studyFlowManager.GetConditionTime() : 0f,
            EventType = eventType,
            TimeToReact = timeToReact,
            RawCamPos = cam.position,
            RawCamRot = cam.rotation,
            AlignedCamPos = SpatialAligner.Instance.GetAlignedPosition(cam.position),
            HazardRawPos = hazardRawPos,
            AlignedHazardPos = SpatialAligner.Instance.GetAlignedPosition(hazardRawPos),
            HazardProfile = hazardProfile
        };

        Vector2 fovAngles = SpatialAligner.Instance.GetFOVAngles(hazardRawPos, cam);
        entry.FOV_HorizAngleToHazard = fovAngles.x;
        entry.FOV_VertAngleToHazard = fovAngles.y;

        hazardLog.Add(entry);
    }

    public void LogVisualEvent(float[] line1, float[] line2, int guess, int truth, float timeTaken)
    {
        if (!ShouldLog()) return;

        VisualLogEntry entry = new VisualLogEntry
        {
            Timestamp = studyFlowManager != null ? studyFlowManager.GetConditionTime() : 0f,
            Line1Vals = line1 != null ? string.Join(";", line1) : "",
            Line2Vals = line2 != null ? string.Join(";", line2) : "",
            UserGuessLine = guess,
            TruthLine = truth,
            TimeTaken = timeTaken
        };

        visualLog.Add(entry);
    }

    private void FixedUpdate()
    {
        if (!ShouldLog()) return;

        Transform cam = PlayerTelemetryManager.Instance != null ? PlayerTelemetryManager.Instance.centerCam : null;
        if (cam == null || SpatialAligner.Instance == null) return;

        TelemetryLogEntry entry = new TelemetryLogEntry
        {
            Timestamp = studyFlowManager != null ? studyFlowManager.GetConditionTime() : 0f,
            RawPos = cam.position,
            RawRot = cam.rotation,
            AlignedPos = SpatialAligner.Instance.GetAlignedPosition(cam.position),
            AlignedRot = SpatialAligner.Instance.GetAlignedRotation(cam.rotation),
            CamForward = cam.forward,
            AlignedCamForward = SpatialAligner.Instance.GetAlignedRotation(cam.rotation) * Vector3.forward
        };

        telemetryLog.Add(entry);
    }

    public void SaveAndAdvance()
    {
        string baseFilename = $"{currentUserID}_{currentConditionCode}";
        
        if (currentConditionIndex == -1) 
        {
            baseFilename = $"{currentUserID}_TrialMode";
        }
        else
        {
            // Save Hazard Log
            StringBuilder hazardSb = new StringBuilder();
            hazardSb.AppendLine("Timestamp,EventType,TimeToReact,RawCamPosX,RawCamPosY,RawCamPosZ,RawCamRotX,RawCamRotY,RawCamRotZ,RawCamRotW,AlignedCamPosX,AlignedCamPosY,AlignedCamPosZ,HazardRawPosX,HazardRawPosY,HazardRawPosZ,AlignedHazardPosX,AlignedHazardPosY,AlignedHazardPosZ,FOV_HorizAngle,FOV_VertAngle,HazardProfile");
            foreach (var e in hazardLog)
            {
                hazardSb.AppendLine($"{e.Timestamp},{e.EventType},{e.TimeToReact},{e.RawCamPos.x},{e.RawCamPos.y},{e.RawCamPos.z},{e.RawCamRot.x},{e.RawCamRot.y},{e.RawCamRot.z},{e.RawCamRot.w},{e.AlignedCamPos.x},{e.AlignedCamPos.y},{e.AlignedCamPos.z},{e.HazardRawPos.x},{e.HazardRawPos.y},{e.HazardRawPos.z},{e.AlignedHazardPos.x},{e.AlignedHazardPos.y},{e.AlignedHazardPos.z},{e.FOV_HorizAngleToHazard},{e.FOV_VertAngleToHazard},{e.HazardProfile}");
            }
            File.WriteAllText(Path.Combine(Application.persistentDataPath, $"{baseFilename}_HazardLog.csv"), hazardSb.ToString());

            // Save Visual Log
            StringBuilder visualSb = new StringBuilder();
            visualSb.AppendLine("Timestamp,Line1Vals,Line2Vals,UserGuessLine,TruthLine,TimeTaken");
            foreach (var e in visualLog)
            {
                visualSb.AppendLine($"{e.Timestamp},{e.Line1Vals},{e.Line2Vals},{e.UserGuessLine},{e.TruthLine},{e.TimeTaken}");
            }
            File.WriteAllText(Path.Combine(Application.persistentDataPath, $"{baseFilename}_VisualLog.csv"), visualSb.ToString());

            // Save Telemetry Log
            StringBuilder telemetrySb = new StringBuilder();
            telemetrySb.AppendLine("Timestamp,RawPosX,RawPosY,RawPosZ,RawRotX,RawRotY,RawRotZ,RawRotW,AlignedPosX,AlignedPosY,AlignedPosZ,AlignedRotX,AlignedRotY,AlignedRotZ,AlignedRotW,CamForwardX,CamForwardY,CamForwardZ,AlignedCamForwardX,AlignedCamForwardY,AlignedCamForwardZ");
            foreach (var e in telemetryLog)
            {
                telemetrySb.AppendLine($"{e.Timestamp},{e.RawPos.x},{e.RawPos.y},{e.RawPos.z},{e.RawRot.x},{e.RawRot.y},{e.RawRot.z},{e.RawRot.w},{e.AlignedPos.x},{e.AlignedPos.y},{e.AlignedPos.z},{e.AlignedRot.x},{e.AlignedRot.y},{e.AlignedRot.z},{e.AlignedRot.w},{e.CamForward.x},{e.CamForward.y},{e.CamForward.z},{e.AlignedCamForward.x},{e.AlignedCamForward.y},{e.AlignedCamForward.z}");
            }
            File.WriteAllText(Path.Combine(Application.persistentDataPath, $"{baseFilename}_TelemetryLog.csv"), telemetrySb.ToString());
        }

        // Clear Buffers
        hazardLog.Clear();
        visualLog.Clear();
        telemetryLog.Clear();

        // Advance Progression
        currentConditionIndex++;
        if (currentConditionIndex > 3) // Reached 4 means 0,1,2,3 completed
        {
            currentUserID++;
            currentConditionIndex = -1; // Reset to Trial Mode for new user
        }

        // Append to UserInfo
        File.AppendAllText(userInfoPath, $"{currentUserID},{currentConditionIndex}\n");

        // Reload to update currentConditionCode
        LoadConfigs();
    }
}
