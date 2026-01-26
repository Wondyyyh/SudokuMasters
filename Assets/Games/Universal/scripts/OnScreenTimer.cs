using System;
using System.Collections;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class OnScreenTimer : MonoBehaviour
{
    [SerializeField] private TMP_Text timerText;
    private TimeSpan elapsedTime = new();
    private DateTime startTime;
    private DateTime stopTime;
    private DateTime pauseStart;
    private DateTime pauseStop;
    private bool isRunning = false;
    private int Optimizer = 0;
    void OnEnable()
    {
        StartCoroutine(StartTimer());
    }
    private void OnDisable()
    {
        StopTimer();
    }
    IEnumerator StartTimer()
    {
        yield return new WaitForEndOfFrame();
        timerText.text = "00:00:00";
        startTime = DateTime.Now;
        isRunning = true;
        yield return null;
    }
    public TimeSpan StopTimer()
    {
        stopTime = DateTime.Now;
        isRunning = false;

        return stopTime - startTime;
    }

    public void PauseTimer()
    {
        if (!isRunning) return; // block double calls
        isRunning = false;
        pauseStart = DateTime.Now;
    }
    public void ResumeTimer()
    {
        pauseStop = DateTime.Now;
        TimeSpan pauseDuration = pauseStop - pauseStart;
        startTime = startTime.Add(pauseDuration);
        isRunning = true;
    }

    // Update is called once per frame
    void Update()
    {
        if (isRunning)
        {
            if (Optimizer >= 100)
            {
                elapsedTime = DateTime.Now - startTime;
                timerText.text = (elapsedTime).ToString(@"hh\:mm\:ss");
                Optimizer = 0;
            }
            else
                Optimizer++;
        }
    }
}
