using UnityEngine;
using System.Collections;
using TMPro;
using UnityEngine.Serialization;

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
    [FormerlySerializedAs("testConditionIndex")]
    [Tooltip("Temporary condition code for testing (-1 = Trial, 0-3 = scheduled Anchor Modes)")]
    public int testConditionCode = -1;

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

        CentralDataLogger logger = CentralDataLogger.Instance;
        int conditionIndex = logger != null
            ? logger.currentConditionIndex
            : (testConditionCode == -1 ? -1 : 0);
        int conditionCode = logger != null
            ? logger.currentConditionCode
            : testConditionCode;

        // Trial Mode is determined by study progression, not by a missing or
        // malformed schedule code.
        if (conditionIndex == -1)
        {
            conditionCode = -1;
        }
        else if (conditionIndex < 0 || conditionIndex > 3 || conditionCode < 0 || conditionCode > 3)
        {
            Debug.LogError(
                $"[StudyFlowManager] Cannot prepare official condition. " +
                $"Invalid index/code pair: {conditionIndex}/{conditionCode}. " +
                "Check UserInfo.csv and StudySchedule.csv.");
            currentState = StudyState.Idle;
            return;
        }

        Debug.Log(
            $"<color=cyan>[StudyFlowManager] Preparing condition index {conditionIndex}, " +
            $"code {conditionCode}</color>");

        if (visualTaskManager != null && !visualTaskManager.SetModeByConditionCode(conditionCode))
        {
            currentState = StudyState.Idle;
            return;
        }

        if (hazardSpawner != null)
        {
            hazardSpawner.ResetPhase();
        }

        // Every condition begins from the same deterministic route. StartRoute
        // safely returns when path mapping has not been completed yet.
        if (StudyManager.instance != null)
        {
            StudyManager.instance.StartRoute();
        }

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

                CentralDataLogger logger = CentralDataLogger.Instance;
                if (logger != null)
                {
                    string completedLabel = logger.currentConditionIndex == -1
                        ? "Trial Mode"
                        : $"Condition {logger.currentConditionCode} (run {logger.currentConditionIndex + 1}/4)";

                    endLog.text = $"{completedLabel} Complete!\nPlease take off your headset\nUID: {logger.currentUserID}";
                    logger.SaveAndAdvance();
                }
                else
                {
                    endLog.text = "Condition Complete!\nPlease take off your headset";
                }
            }
        }
    }
}
