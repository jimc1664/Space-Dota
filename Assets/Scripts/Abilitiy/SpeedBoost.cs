﻿using UnityEngine;

public class SpeedBoost : Ability {


    public float AccFactor, SpdFactor;

    protected override bool check() {
        return 0 > (Car.Tm.Glob_SpeedBoost - Time.time) && base.check();
    }
    protected override void apply() {
        //  Car.buff(Unit_Kinematic.Buffable.MaxSpeed, SpdFactor, Duration);
        //   Car.buff(Unit_Kinematic.Buffable.Acceleration, AccFactor, Duration);

        Car.Tm.speedBuff(Duration, AccFactor, SpdFactor);
    }

}
