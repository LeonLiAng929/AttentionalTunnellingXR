using UnityEngine;
using System.Collections;
using TMPro;

public class StudyFlowManager : MonoBehaviour
{
    
    public AudioSource startSound;
    public AudioSource endSound;
    public TMP_Text endLog;
    public enum StudyState
    {
        Idle,
        Prepared,
        Starting,
        Running,
        Completed
    }

    public StudyState currentState = StudyState.Idle;

    [Header("Testing")]
    [Tooltip("Temporary condition index for testing (-1 = Trial, 0-3 = Anchor Modes)")]
    public int testConditionIndex = -1;

    private HazardSpawner hazardSpawner;
    private VisualTaskManager visualTaskManager;
    private float conditionStartTime = 0f;

    public float GetConditionTime()
    {
        if (currentState == StudyState.Running)
        {
            return Time.time - conditionStartTime;
        }
        return 0f;
    }

    void Start()
    {
        hazardSpawner = FindObjectOfType<HazardSpawner>();
        visualTaskManager = FindObjectOfType<VisualTaskManager>();
        if (DebugMode.instance.DebugOn)
        {
            StudyManager.instance.PathMapping();
            PrepareStudy();
            StartCoroutine(StartCountdown());
        }
    }

    void Update()
    {
        HandleInputs();
        MonitorCompletion();
    }

    private void HandleInputs()
    {
        if (OVRInput.GetDown(OVRInput.RawButton.LThumbstickUp))
        {
            PrepareStudy();
            endLog.gameObject.SetActive(false);
        }

        if (currentState == StudyState.Prepared && OVRInput.GetDown(OVRInput.RawButton.RThumbstickUp))
        {
            startSound.Play();
            StartCoroutine(StartCountdown());
        }
    }

    private void PrepareStudy()
    {
        if (currentState == StudyState.Running || currentState == StudyState.Starting) return;

        // In the future, this will read from CentralDataLogger
        int condition = CentralDataLogger.Instance != null ? CentralDataLogger.Instance.currentConditionIndex : testConditionIndex;
        
        Debug.Log($"<color=cyan>[StudyFlowManager] Preparing condition: {condition}</color>");

        if (DimensionVisualiser.instance != null && DimensionVisualiser.instance.anchorList != null && DimensionVisualiser.instance.anchorList.Count >= 2)
        {
            if (SpatialAligner.Instance != null)
            {
                SpatialAligner.Instance.SetAnchors(DimensionVisualiser.instance.anchorList[0].transform, DimensionVisualiser.instance.anchorList[1].transform);
            }
            else
            {
                Debug.LogWarning("[StudyFlowManager] SpatialAligner Instance is missing in the scene!");
            }
        }
        else
        {
            Debug.LogWarning("[StudyFlowManager] Anchors not loaded or not enough anchors. Cannot align spatial coordinates.");
        }

        if (visualTaskManager != null)
        {
            visualTaskManager.SetModeByConditionIndex(condition);
        }

        currentState = StudyState.Prepared;
        if (CentralDataLogger.Instance != null) CentralDataLogger.Instance.UpdateSessionInfo();
    }

    private IEnumerator StartCountdown()
    {
        currentState = StudyState.Starting;
        Debug.Log("<color=yellow>[StudyFlowManager] Starting in 3...</color>");
        yield return new WaitForSeconds(1f);
        Debug.Log("<color=yellow>[StudyFlowManager] Starting in 2...</color>");
        yield return new WaitForSeconds(1f);
        Debug.Log("<color=yellow>[StudyFlowManager] Starting in 1...</color>");
        yield return new WaitForSeconds(1f);

        currentState = StudyState.Running;
        conditionStartTime = Time.time;
        if (CentralDataLogger.Instance != null) CentralDataLogger.Instance.UpdateSessionInfo();
        Debug.Log("<color=green>[StudyFlowManager] Study Running!</color>");

        if (hazardSpawner != null)
        {
            hazardSpawner.isSpawningActive = true;
        }

        if (visualTaskManager != null)
        {
            visualTaskManager.GenerateNewTrial();
            visualTaskManager.isUpdatingActive = true;
        }
    }

    private void MonitorCompletion()
    {
        if (currentState == StudyState.Running)
        {
            if (hazardSpawner != null && hazardSpawner.isPhaseComplete)
            {
                currentState = StudyState.Completed;
                if (CentralDataLogger.Instance != null) CentralDataLogger.Instance.UpdateSessionInfo();
                Debug.Log("<color=magenta>[StudyFlowManager] Phase Complete! Stopping updates.</color>");
                endSound.Play();
                hazardSpawner.isSpawningActive = false;
                if (visualTaskManager != null)
                {
                    visualTaskManager.isUpdatingActive = false;
                }
                
                endLog.gameObject.SetActive(true);
                endLog.text = $"Condition {CentralDataLogger.Instance.currentConditionIndex} Complete!\nPlease take off your headset\nUID: {CentralDataLogger.Instance.currentUserID}";
                
                if (CentralDataLogger.Instance != null)
                {
                    CentralDataLogger.Instance.SaveAndAdvance();
                }
            }
        }
    }
}
