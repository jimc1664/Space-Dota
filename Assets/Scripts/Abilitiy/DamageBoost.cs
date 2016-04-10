using UnityEngine;
using System.Collections;

public class DamageBoost : Ability {

    public float RoFFactor, DmgFactor;

    protected override bool check() {
        return 0 > (Car.Tm.Glob_DamageBoost - Time.time) && base.check();
    }
    protected override void apply() {
        //  Car.buff(Unit_Kinematic.Buffable.MaxSpeed, SpdFactor, Duration);
        //   Car.buff(Unit_Kinematic.Buffable.Acceleration, AccFactor, Duration);

        Car.Tm.dmgBuff(Duration, DmgFactor, RoFFactor);
    }

}
