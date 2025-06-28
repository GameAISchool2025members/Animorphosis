using UnityEngine;
using TMPro;              
using UnityEngine.UI;      

public class CountdownTimer : MonoBehaviour
{
    [Header("Inställningar")]
    public TextMeshProUGUI timerTextTMP;
    public Text timerTextUI;

    private float elapsedTime = 0f;
    private bool isRunning = false;

    void Start()
    {
        StartTimer();
    }

    void Update()
    {
        if (!isRunning) return;

        elapsedTime += Time.deltaTime;
        UpdateTimerDisplay();
    }

    private void UpdateTimerDisplay()
    {
        int minutes = Mathf.FloorToInt(elapsedTime / 60f);
        int seconds = Mathf.FloorToInt(elapsedTime % 60f);
        string text = string.Format("{0:00}:{1:00}", minutes, seconds);

        if (timerTextTMP != null) timerTextTMP.text = text;
        if (timerTextUI != null) timerTextUI.text = text;
    }

    public void StartTimer()
    {
        elapsedTime = 0f;
        isRunning = true;
    }

    public void StopTimer()
    {
        isRunning = false;
       
        
    }
}
