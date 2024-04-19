// using System;
// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;
// using System.IO;
// using System.Text;
// using UnityEngine.AddressableAssets;

public class Epos4Main : UnityEngine.MonoBehaviour {
    [UnityEngine.SerializeField]
    public Epos4Node lifter, leftPedal, leftSlider, rightPedal, rightSlider;

    // private UnityEngine.Coroutine coroutineActualPosition = null;

    // private UnityEngine.WaitForSeconds waitForSeconds;

    private System.Threading.Thread th = null;

    private bool Destroied = false;

    void Start() {
        EposCmd.Net.DeviceManager connector = null;
        try {
            connector = new EposCmd.Net.DeviceManager("EPOS4", "MAXON SERIAL V2", "USB", "USB0");
        }
        catch (EposCmd.Net.DeviceException) {
        }
        this.lifter      = new Epos4Node(connector, 1, "Lifter",       2);
        this.lifter.MotorInit();
        this.leftPedal   = new Epos4Node(connector, 2, "Left Pedal",   6);
        this.leftPedal.MotorInit();
        this.leftSlider  = new Epos4Node(connector, 3, "Left Slider",  12);
        this.leftSlider.MotorInit();
        this.rightPedal  = new Epos4Node(connector, 4, "Right Pedal",  6);
        this.rightPedal.MotorInit();
        this.rightSlider = new Epos4Node(connector, 5, "Right Slider", 12);
        this.rightSlider.MotorInit();
        // this.waitForSeconds = new UnityEngine.WaitForSeconds(0.1f);
        // this.coroutineActualPosition = StartCoroutine(this.getActualPositionAsync());
        this.th = new System.Threading.Thread(new System.Threading.ThreadStart(this.getActualPositionAsync));
        // System.Threading.Tasks.Task.Run(this.getActualPositionAsync);
        this.th.Start();
    }

    public void clearError() {
        EposCmd.Net.DeviceManager connector = null;
        try {
            connector = new EposCmd.Net.DeviceManager("EPOS4", "MAXON SERIAL V2", "USB", "USB0");
        }
        catch (EposCmd.Net.DeviceException) {
        }
        this.lifter      = new Epos4Node(connector, 1, "Lifter",       2);
        this.lifter.MotorInit();
        this.leftPedal   = new Epos4Node(connector, 2, "Left Pedal",   6);
        this.leftPedal.MotorInit();
        this.leftSlider  = new Epos4Node(connector, 3, "Left Slider",  12);
        this.leftSlider.MotorInit();
        this.rightPedal  = new Epos4Node(connector, 4, "Right Pedal",  6);
        this.rightPedal.MotorInit();
        this.rightSlider = new Epos4Node(connector, 5, "Right Slider", 12);
        this.rightSlider.MotorInit();
    }

    // void Update() {
    // }

    private void getActualPositionAsync() {
        while (!this.Destroied) {
            this.lifter.actualPosition      = (int)(-(float) this.lifter.getPositionIs()/2000f*2f);
            this.leftPedal.actualPosition   = (int)(-(float) this.leftPedal.getPositionIs()/2000f*6f);
            this.leftSlider.actualPosition  = (int)(-(float) this.leftSlider.getPositionIs()/2000f*12f);
            this.rightPedal.actualPosition  = (int)(-(float) this.rightPedal.getPositionIs()/2000f*6f);
            this.rightSlider.actualPosition = (int)(-(float) this.rightSlider.getPositionIs()/2000f*12f);

            this.lifter.current      = this.lifter.getCurrentIs()/1000f;
            this.leftPedal.current   = this.leftPedal.getCurrentIs()/1000f;
            this.leftSlider.current  = this.leftSlider.getCurrentIs()/1000f;
            this.rightPedal.current  = this.rightPedal.getCurrentIs()/1000f;
            this.rightSlider.current = this.rightSlider.getCurrentIs()/1000f;
            System.Threading.Thread.Sleep(20);
        }
        return;
    }

    public void AllNodeActivateProfilePositionMode()
    {
        this.lifter.ActivateProfilePositionMode();
        this.leftPedal.ActivateProfilePositionMode();
        this.leftSlider.ActivateProfilePositionMode();
        this.rightPedal.ActivateProfilePositionMode();
        this.rightSlider.ActivateProfilePositionMode();
    }

    public void AllNodeMoveToHome()
    {
        this.lifter.MoveToHome();
        this.leftPedal.MoveToHome();
        this.leftSlider.MoveToHome();
        this.rightPedal.MoveToHome();
        this.rightSlider.MoveToHome();
    }

    public void AllNodeDefinePosition()
    {
        this.lifter.definePosition();
        this.leftPedal.definePosition();
        this.leftSlider.definePosition();
        this.rightPedal.definePosition();
        this.rightSlider.definePosition();
    }

    private void OnDestroy()
    {
        this.Destroied = true;
        this.th.Abort();
    }
}
