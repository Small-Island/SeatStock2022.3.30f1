// using System;
// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;
// using System.IO;
// using System.Text;
// using UnityEngine.AddressableAssets;

[System.Serializable]
public class Epos4Node {
    private EposCmd.Net.Device device;
    private DeviceOperation deviceOperation;
    private ushort nodeId;
    private EposCmd.Net.DeviceManager connector;
    [UnityEngine.HideInInspector] public float targetTime = 2;
    [UnityEngine.HideInInspector] public float targetPosMilli = 0;

    [UnityEngine.HideInInspector] public bool oldUpForwardRepeatButton = false;
    [UnityEngine.HideInInspector] public bool oldDownBackwardRepeatButton = false;

    private double direction = 1;
    private float incPerRotation = 2000;

    public enum ConnectionStatus {
        success = 1,
        failed = 0
    }

    public enum WhichMode {
        Position,
        Velocity,
        Homing
    }

    [UnityEngine.SerializeField, ReadOnly] public ConnectionStatus cs;
    [UnityEngine.SerializeField, ReadOnly] public WhichMode whichMode;
    [UnityEngine.SerializeField, ReadOnly] public double milliPerRotation = 0;
    [UnityEngine.SerializeField, UnityEngine.Range(0.1f, 0.9f)] public double maxVelocityTimeRate = 0.5f;
    [UnityEngine.SerializeField, UnityEngine.Range(0.1f, 2f)] public double speedRate = 1f;

    [UnityEngine.HideInInspector] public string status = ""; 
    [UnityEngine.HideInInspector] public Profile profile;
    [UnityEngine.HideInInspector] public int actualPosition = 0;
    [UnityEngine.HideInInspector] public float current = 0;
    [UnityEngine.HideInInspector] public float actualVelocity = 0;

    public int keyHandle = 0;

    public Epos4Node(
        EposCmd.Net.DeviceManager arg_connector, 
        int arg_idx,
        double arg_milliPerRotation,
        double arg_direction,
        double arg_maxVelocityTimeRate,
        double arg_speedRate
    )
    {
        this.nodeId = (ushort)arg_idx;
        this.connector = arg_connector;
        this.milliPerRotation = arg_milliPerRotation;
        this.direction = arg_direction;
        this.maxVelocityTimeRate = arg_maxVelocityTimeRate;
        this.speedRate = arg_speedRate;
    }

    public void MotorInit()
    {
        // EposCmd.Net.DeviceManager connector = null;
        // try {
        //     connector = new EposCmd.Net.DeviceManager("EPOS4", "MAXON SERIAL V2", "USB", "USB0");
        // }
        // catch (EposCmd.Net.DeviceException) {
        //     this.status = "Connection failed";
        //     return;
        // }

        uint errorCode = 0;
        // this.keyHandle = EposCmd.Net.VcsWrapper.Device.VcsOpenDevice("EPOS4", "MAXON SERIAL V2", "USB", "USB0", ref errorCode);
        
        // EposCmd.Net.VcsWrapper.Device.VcsGetKeyHandle("EPOS4", "MAXON SERIAL V2", "USB", "USB0", ref this.keyHandle, ref errorCode);



        this.status = "";
        try {
            this.device = this.connector.CreateDevice((ushort)this.nodeId);
        }
        catch (EposCmd.Net.DeviceException) {
            this.status = "Connection failed";
            this.cs = ConnectionStatus.failed;
            return;
        }
        catch (System.Exception) {
            this.status = "Connection failed";
            this.cs = ConnectionStatus.failed;
            return;
        }
        this.deviceOperation = new DeviceOperation(this.device);

        this.getError();

        try {
            this.deviceOperation.ClearFaultAndSetEnableState();
        }
        catch (EposCmd.Net.DeviceException) {
            this.status = "Connection failed";
            this.cs = ConnectionStatus.failed;
            return;
        }
        this.ActivateProfilePositionMode();
        this.cs = ConnectionStatus.success;
        EposCmd.Net.VcsWrapper.Device.VcsGetKeyHandle("EPOS4", "MAXON SERIAL V2", "USB", "USB0", ref this.keyHandle, ref errorCode);
        return;
    }

    public void ActivateProfilePositionMode() {
        if (this.cs == ConnectionStatus.failed) return;
        try {
            this.deviceOperation.ActivateProfilePositionMode();
        }
        catch (System.Exception e) {
            this.status = e.ToString();
        }
        this.whichMode = WhichMode.Position;
        return;
    }

    public int getPositionMM() {
        if (this.cs == ConnectionStatus.failed) return 0;
        int value = 0;
        try {
            value = this.deviceOperation.GetPositionIs();
        }
        catch (System.Exception) {
            // this.status = e.ToString();
        }
        return (int)(this.direction*value/this.incPerRotation*this.milliPerRotation);
    }

    public float getCurrentA() {
        if (this.cs == ConnectionStatus.failed) return 0;
        int value = 0;
        try {
            value = this.deviceOperation.GetCurrentIs();
        }
        catch (System.Exception) {
            // this.status = e.ToString();
        }
        return value/1000f;
    }

    public float getVelocityIs() {
        if (this.cs == ConnectionStatus.failed) return 0;
        int value = 0;
        try {
            value = this.deviceOperation.GetVelocityIs();
        }
        catch (System.Exception) {
            // this.status = e.ToString();
        }
        return (float) this.direction * value;
    }

    public void getError() {
        if (this.cs == ConnectionStatus.failed) return;
        // uint ecode = 2000;
        // try {
        //     ecode = this.deviceOperation.getLastError();
        //     this.status = ecode.ToString();
        // }
        // catch (System.Exception e) {
        //     this.status = e.ToString();
        // }
        // EposCmd.Net.VcsWrapper.Device.Init();
        int isInFault = 0;
        uint errorCode = 0;
        EposCmd.Net.VcsWrapper.Device.VcsGetFaultState(this.keyHandle, this.nodeId, ref isInFault, ref errorCode);

        this.status = $"Falt State: {isInFault} Error Code: {errorCode:X}";

        return;
    }

    public void ActivateHomingMode() {
        if (this.cs == ConnectionStatus.failed) return;
        try {
            this.deviceOperation.ActivateHomingMode();
        }
        catch (System.Exception e)
        {
            this.status = e.ToString();
        }
        this.whichMode = WhichMode.Homing;
    }

    public void definePosition() {
        if (this.cs == ConnectionStatus.failed) return;
        this.profile.absolute     = false;
        this.profile.position     = 0;
        this.profile.velocity     = 120;
        this.profile.acceleration = 240;
        this.profile.deceleration = 240;
        try {
            this.deviceOperation.SetPositionProfile(
                this.profile.velocity,
                this.profile.acceleration,
                this.profile.deceleration
            );
            this.deviceOperation.MoveToPosition(
                this.profile.position,
                this.profile.absolute,
                true
            );
        }
        catch (System.Exception e) {
            this.status = e.ToString();
        }
        try {
            this.deviceOperation.DefinePosition(0);
        }
        catch (System.Exception e)
        {
            this.status = e.ToString();
        }
    }

    public void MoveToPositionInTime(double arg_pos_milli, double arg_sec_time, bool arg_activate) {
        if (this.cs == ConnectionStatus.failed) return;
        if (this.status != "") return;
        double arg_pos_r = arg_pos_milli / this.milliPerRotation;
        double arg_pos_inc = this.incPerRotation * arg_pos_r;

        double x_in = arg_pos_inc - this.deviceOperation.GetPositionIs() * this.direction;
        double x_r  = x_in / this.incPerRotation;

        if (System.Math.Abs(x_r) < 0.0001) {
            return;
        }

        // this.profile.absolute     = true;
        // this.profile.position     = (int)arg_pos_milli;
        // this.profile.velocity     = (int)System.Math.Abs(c * 2.0 * x_r / arg_sec_time * 60.0);
        // this.profile.acceleration = (int)System.Math.Abs(c * 4.0 * x_r / arg_sec_time / arg_sec_time * 60.0);
        // this.profile.deceleration = (int)System.Math.Abs(c * 4.0 * x_r / arg_sec_time / arg_sec_time * 60.0);

        this.profile.absolute     = true;
        this.profile.position     = (int)arg_pos_milli;
        this.profile.velocity     = (int)System.Math.Abs(this.speedRate * 2.0 * x_r / (arg_sec_time*(1 + this.maxVelocityTimeRate)) * 60.0);
        this.profile.acceleration = (int)System.Math.Abs(this.speedRate * 4.0 * x_r / (arg_sec_time * arg_sec_time *(1 - this.maxVelocityTimeRate*this.maxVelocityTimeRate)) * 60.0);
        this.profile.deceleration = (int)System.Math.Abs(this.speedRate * 4.0 * x_r / (arg_sec_time * arg_sec_time *(1 - this.maxVelocityTimeRate*this.maxVelocityTimeRate)) * 60.0);

        this.SetPositionProfile();
        this.MoveToPosition(arg_activate);
    }

    private double old_arg_pos_inc = 0;

    public void SetPositionProfileInTime(double arg_pos_milli, double arg_sec_time, double arg_arate, double arg_drate) {
        // if (this.cs == ConnectionStatus.failed) return;
        // if (this.status != "") return;
        double arg_pos_r = arg_pos_milli / this.milliPerRotation;
        double arg_pos_inc = this.incPerRotation * arg_pos_r;

        // double x_in = arg_pos_inc - this.deviceOperation.GetPositionIs() * this.direction;
        double x_inc = arg_pos_inc - this.old_arg_pos_inc;
        double x_r  = x_inc / this.incPerRotation;

        this.old_arg_pos_inc = arg_pos_inc;

        if (System.Math.Abs(x_r) < 0.0001) {
            return;
        }

        // this.profile.absolute     = true;
        // this.profile.position     = (int)arg_pos_milli;
        // this.profile.velocity     = (int)System.Math.Abs(c * 2.0 * x_r / arg_sec_time * 60.0);
        // this.profile.acceleration = (int)System.Math.Abs(c * 4.0 * x_r / arg_sec_time / arg_sec_time * 60.0);
        // this.profile.deceleration = (int)System.Math.Abs(c * 4.0 * x_r / arg_sec_time / arg_sec_time * 60.0);

        this.profile.absolute     = true;
        this.profile.position     = (int)arg_pos_milli;
        this.profile.velocity     = (int)System.Math.Abs(this.speedRate * 2.0 * x_r / (arg_sec_time*(1 + this.maxVelocityTimeRate)) * 60.0);
        this.profile.acceleration = (int)System.Math.Abs((arg_arate + arg_drate) / (2.0 * arg_drate) *this.speedRate * 4.0 * x_r / (arg_sec_time * arg_sec_time *(1 - this.maxVelocityTimeRate*this.maxVelocityTimeRate)) * 60.0);
        this.profile.deceleration = (int)System.Math.Abs((arg_arate + arg_drate) / (2.0 * arg_arate)*this.speedRate * 4.0 * x_r / (arg_sec_time * arg_sec_time *(1 - this.maxVelocityTimeRate*this.maxVelocityTimeRate)) * 60.0);

        if (this.profile.velocity < 1) {
            this.profile.velocity = 120;
        }

        if (this.profile.acceleration < 1) {
            this.profile.acceleration = 1000;
        }
        if (this.profile.deceleration < 1) {
            this.profile.deceleration = 1000;
        }

        this.SetPositionProfile();
    }

    public void SetPositionProfile() {
        if (this.cs == ConnectionStatus.failed) return;
        try {
            this.deviceOperation.SetPositionProfile(
                this.profile.velocity,
                this.profile.acceleration,
                this.profile.deceleration
            );
        }
        catch (System.Exception e) {
            this.status = e.ToString();
        }
    }

    public void MoveToPosition(bool arg_activate) {
        if (arg_activate == false) return;
        if (this.cs == ConnectionStatus.failed) return;
        try {
            this.deviceOperation.MoveToPosition(
                (int)(this.direction * this.profile.position/this.milliPerRotation*this.incPerRotation),
                this.profile.absolute,
                true
            );
        }
        catch (System.Exception e) {
            this.status = e.ToString();
        }
    }

    public void MoveStop() {
        if (this.cs == ConnectionStatus.failed) return;
        this.profile.velocity     = 0;
        this.profile.acceleration = (int)(System.Math.Abs(this.actualVelocity * 0.4));
        this.profile.deceleration = (int)(System.Math.Abs(this.actualVelocity * 0.4));
        if (this.profile.acceleration < 500) {
            this.profile.acceleration = 500;
        }
        if (this.profile.deceleration < 500) {
            this.profile.deceleration = 500;
        }
        // this.ActivateProfileVelocityMode();
        try {
            this.deviceOperation.SetVelocityProfile(
                this.profile.acceleration,
                this.profile.deceleration
            );
            this.deviceOperation.MoveWithVelocity(0);
        }
        catch (System.Exception e) {
            this.status = e.ToString();
        }
        // this.ActivateProfilePositionMode();
    }

    private System.Timers.Timer timerMoveToHome;

    public void MoveToHome() {
        if (this.cs == ConnectionStatus.failed) return;
        this.timerMoveToHome = new System.Timers.Timer(200);
        this.timerMoveToHome.AutoReset = true;
        this.timerMoveToHome.Elapsed += (sender, e) => {
            if (System.Math.Abs(this.actualVelocity) > 60) {
                UnityEngine.Debug.Log("still moving..");
                return;
            }
            this.profile.position     = 0;
            this.profile.velocity     = 720;
            // this.profile.acceleration = (int)(System.Math.Abs(this.actualVelocity * 5));
            // this.profile.deceleration = (int)(System.Math.Abs(this.actualVelocity * 2));
            // if (this.profile.acceleration < 1000) {
            //     this.profile.acceleration = 1000;
            // }
            // if (this.profile.deceleration < 1000) {
            //     this.profile.deceleration = 1000;
            // }
            this.profile.acceleration = 1000;
            this.profile.deceleration = 1000;
            this.profile.absolute = true;
            try {
                this.deviceOperation.SetPositionProfile(
                    this.profile.velocity,
                    this.profile.acceleration,
                    this.profile.deceleration
                );
                this.deviceOperation.MoveToPosition(
                    this.profile.position,
                    this.profile.absolute,
                    true
                );
            }
            catch (System.Exception exc) {
                this.status = exc.ToString();
            }
            this.profile.absolute     = false;
            this.profile.position     = 0;
            this.profile.velocity     = 120;
            this.profile.acceleration = 240;
            this.profile.deceleration = 240;
            this.timerMoveToHome.Stop();
        };
        this.timerMoveToHome.Start();
        // this.deviceOperation.SetQuickStopState();
    }

    public void QuickStop() {
        if (this.cs == ConnectionStatus.failed) return;
        try {
           this.deviceOperation.SetQuickStopState();
        }
        catch (System.Exception e) {
            this.status = e.ToString();
        }
    }

    public void ActivateProfileVelocityMode() {
        if (this.cs == ConnectionStatus.failed) return;
        try {
            this.deviceOperation.ActivateProfileVelocityMode();
        }
        catch (System.Exception e) {
            this.status = e.ToString();
        }
        this.whichMode = WhichMode.Velocity;
    }

    public void MoveWithVelocity(int arg_pm) {
        if (this.cs == ConnectionStatus.failed) return;
        try {
            this.deviceOperation.SetVelocityProfile(
                this.profile.acceleration,
                this.profile.deceleration
            );
            this.deviceOperation.MoveWithVelocity(
                (int) (this.direction * arg_pm * this.profile.velocity)
            );
        }
        catch (System.Exception e) {
            this.status = e.ToString();
        }
    }

    // Unit inc   2000 inc == 1 rotation == 2 mm
    private void OnDestroy()
    {
        this.deviceOperation.AdvancedDispose();
    }

    // private class VcsWrapper {
    //     EposCmd.Net.VcsWrapper.Device.VcsGetErrorInfo();
    // }

    private class DeviceOperation {
        private EposCmd.Net.Device device;
        private EposCmd.Net.DeviceCmdSet.Operation.StateMachine sm;
        private EposCmd.Net.DeviceCmdSet.Operation.ProfilePositionMode ppm;
        private EposCmd.Net.DeviceCmdSet.Operation.ProfileVelocityMode pvm;
        private EposCmd.Net.DeviceCmdSet.Operation.MotionInfo mi;
        private EposCmd.Net.DeviceCmdSet.Operation.HomingMode hm;
        public DeviceOperation(EposCmd.Net.Device arg_device) {
            this.device = arg_device;
            this.sm = this.device.Operation.StateMachine;
            this.ppm = this.device.Operation.ProfilePositionMode;
            this.pvm = this.device.Operation.ProfileVelocityMode;
            this.mi = this.device.Operation.MotionInfo;
            this.hm = this.device.Operation.HomingMode;
        }

        public void SetQuickStopState() {
            this.sm.SetQuickStopState();
        }

        public void ClearFaultAndSetEnableState() {
            if (this.sm.GetFaultState()) {
                this.sm.ClearFault();
            }
            this.sm.SetEnableState();
        }

        public void ActivateProfilePositionMode() {
            try {
                this.ppm.ActivateProfilePositionMode();
            }
            catch (System.Exception e) {
                throw e;
            }
        }

        public void ActivateProfileVelocityMode() {
            try {
                this.pvm.ActivateProfileVelocityMode();
            }
            catch (System.Exception e) {
                throw e;
            }
        }

        public void MoveToPosition(int arg_position, bool arg_absolute, bool arg_immediately) {
            try
            {
                // arg_position (inc) == 360/2000 (deg)
                this.ppm.MoveToPosition(arg_position, arg_absolute, arg_immediately);
            }
            catch (System.Exception e) {
                throw e;
            }
            return;
        }

        public void MoveWithVelocity(int arg_target_velocity) {
            try
            {
                // arg_position (inc) == 360/2000 (deg)
                this.pvm.MoveWithVelocity(arg_target_velocity);
            }
            catch (System.Exception e) {
                throw e;
            }
            return;
        }

        public void SetPositionProfile(
            int arg_ProfileVelocity,
            int arg_ProfileAcceleration,
            int arg_ProfileDeceleration
        )
        {
            try {
                this.ppm.SetPositionProfile(
                    (uint)System.Math.Abs(arg_ProfileVelocity),
                    (uint)System.Math.Abs(arg_ProfileAcceleration),
                    (uint)System.Math.Abs(arg_ProfileDeceleration)
                );
            }
            catch (System.Exception e) {
                throw e;
                // UnityEngine.MonoBehaviour.print(e);
            }
        }

        public void SetVelocityProfile(
            int arg_ProfileAcceleration,
            int arg_ProfileDeceleration
        )
        {
            try {
                this.pvm.SetVelocityProfile(
                    (uint)System.Math.Abs(arg_ProfileAcceleration),
                    (uint)System.Math.Abs(arg_ProfileDeceleration)
                );
            }
            catch (System.Exception e) {
                throw e;
                // UnityEngine.MonoBehaviour.print(e);
            }
        }

        public void ActivateHomingMode() {
            try {
                this.hm.ActivateHomingMode();
            }
            catch (System.Exception e)
            {
                throw e;
            }
        }

        public void DefinePosition(int arg_offsetPosition) {
            try {
                this.hm.DefinePosition(arg_offsetPosition);
            }
            catch (System.Exception e)
            {
                throw e;
            }
        }

        public int GetPositionIs() {
            int value = 0;
            try {
                value = (int)(this.mi.GetPositionIs());
            }
            catch (System.Exception) {
                // UnityEngine.MonoBehaviour.print(e);
            }
            return value;
        }

        public int GetCurrentIs() {
            int value = 0;
            try {
                value = (int)(this.mi.GetCurrentIs());
            }
            catch (System.Exception) {
                // UnityEngine.MonoBehaviour.print(e);
            }
            return value;
        }

        public int GetVelocityIs() {
             int value = 0;
            try {
                value = (int)(this.mi.GetVelocityIs());
            }
            catch (System.Exception) {
                // UnityEngine.MonoBehaviour.print(e);
            }
            return value;
        }

        public uint getLastError() {
            if (this.sm.GetFaultState()) {
                return this.sm.LastError;
            }
            return 1000;
        }

        public void AdvancedDispose() {
            if (this.ppm != null) {
                this.ppm.Advanced.Dispose();
            }
            if (this.pvm != null) {
                this.pvm.Advanced.Dispose();
            }
        }
    }

    [System.Serializable]
    public class Profile {
        public bool absolute = false;

        // [UnityEngine.SerializeField, UnityEngine.Range(-2000, 2000), UnityEngine.Header("Unit inc")]
        public int position = 0;
        // [UnityEngine.SerializeField, UnityEngine.Range(0, 120), UnityEngine.Header("Unit rpm")]
        public int velocity = 120;
        // [UnityEngine.SerializeField, UnityEngine.Range(0, 240), UnityEngine.Header("Unit rpm/s")]
        public int acceleration = 240;
        // [UnityEngine.SerializeField, UnityEngine.Range(0, 240), UnityEngine.Header("Unit rpm/s")]
        public int deceleration = 240;
    }
}