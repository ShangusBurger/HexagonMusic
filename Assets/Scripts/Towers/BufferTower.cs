using System.Collections;
using System.Collections.Generic;
using CubeCoordinates;
using TMPro;
using UnityEngine;

public class BufferTower : Tower
{
    public int threshold = 8;
    public int currentAccumulated;
    public bool resetBufferNextUpdate = false;
    public bool goalCompleteFlag = false;

    [SerializeField] private TMP_Text bufferSizeText;

    //Visual feedback of buffer
    [SerializeField] private Transform bufferIndicatorTransform;
    internal override void Update()
    {
        base.Update();

        //disable the triggering of sound unless the buffer has been filled
        towerAlreadyActivatedThisBeat = true;

        if (resetBufferNextUpdate)
        {
            currentAccumulated = 0;
            resetBufferNextUpdate = false;
        }
    }

    internal override void OnPulseReceived(Pulse incomingPulse)
    {
        base.OnPulseReceived(incomingPulse);

        //Pulse is added to accumulated total
        currentAccumulated++;

        StartCoroutine(VisuallyUpdateBuffer(TempoHandler.nextBeatTime - AudioSettings.dspTime + TempoHandler.beatLength));
        
        if (currentAccumulated >= threshold)
        {
            towerAlreadyActivatedThisBeat = false;
            Pulse newPulse = new Pulse(incomingPulse.direction, source: true);
            tile.SchedulePulse(newPulse);
            resetBufferNextUpdate = true;
            goalCompleteFlag = true;
        }
    }

    internal override void PlayScheduledClip()
    {

        goalTime = TempoHandler.nextBeatTime;
        base.PlayScheduledClip();

    }

    public IEnumerator VisuallyUpdateBuffer(double beatDelay)
    {
        yield return new WaitForSeconds((float) beatDelay);

        float indicatorDifference = ((float) currentAccumulated + 1f) / (float) threshold;
        bufferIndicatorTransform.position = new Vector3(bufferIndicatorTransform.position.x, indicatorDifference + .001f, bufferIndicatorTransform.position.z);
        bufferIndicatorTransform.localScale = new Vector3(bufferIndicatorTransform.localScale.x, 2 * indicatorDifference, bufferIndicatorTransform.localScale.z);
    }

    public void UpdateBufferSize(float value)
    {
        threshold = (int) value;
        bufferSizeText.text = value.ToString();
    }

    public override void SetSelfUI()
    {
        towerUI.SetDropdown("Tom");
        towerUI.OnSampleSelected("Tom");
    }
}
