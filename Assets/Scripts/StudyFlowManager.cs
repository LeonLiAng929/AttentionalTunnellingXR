using UnityEngine;
using System.Collections;

public class StudyFlowManager : MonoBehaviour
{
    
    public AudioSource startSound;
    public AudioSource endSound;
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

    void Start()
    {
        hazardSpawner = FindObjectOfType<HazardSpawner>();
        visualTaskManager = FindObjectOfType<VisualTaskManager>();
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
        int condition = testConditionIndex;
        
        Debug.Log($"<color=cyan>[StudyFlowManager] Preparing condition: {condition}</color>");

        if (visualTaskManager != null)
        {
            visualTaskManager.SetModeByConditionIndex(condition);
        }

        currentState = StudyState.Prepared;
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
                Debug.Log("<color=magenta>[StudyFlowManager] Phase Complete! Stopping updates.</color>");
                endSound.Play();
                hazardSpawner.isSpawningActive = false;
                if (visualTaskManager != null)
                {
                    visualTaskManager.isUpdatingActive = false;
                }

                // In the future: call CentralDataLogger.SaveAndAdvance()
            }
        }
    }
}
