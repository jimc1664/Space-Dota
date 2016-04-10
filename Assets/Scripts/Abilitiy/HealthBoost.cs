using UnityEngine;
using System.Collections;

public class HealthBoost : Ability {

    public float RepairFactor, MitigationFactor;

    protected override bool check() {
        return 0 > (Car.Tm.Glob_DamageBoost - Time.time) && base.check();
    }
    protected override void apply() {
        //  Car.buff(Unit_Kinematic.Buffable.MaxSpeed, SpdFactor, Duration);
        //   Car.buff(Unit_Kinematic.Buffable.Acceleration, AccFactor, Duration);

        Car.Tm.healthBuff(Duration, RepairFactor, MitigationFactor);
    }

}
