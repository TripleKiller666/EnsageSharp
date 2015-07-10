#region

using Ensage;
using System;
using System.Collections.Generic;
using System.Linq;
using SharpDX;

#endregion

namespace Ensage.Common
{
    public class Prediction
    {

        public string UnitName;
        public ClassId UnitClassId;
        public Vector3 Speed;
        public float RotSpeed;
        public Vector3 LastPosition;
        public float LastRotR;
        public float Lasttick;

        public Prediction() { }

        public Prediction(string unitName,
            ClassId unitClassId,
            Vector3 speed,
            float rotSpeed,
            Vector3 lastPosition,
            float lastRotR,
            float lasttick)
        {
            UnitName = unitName;
            UnitClassId = unitClassId;
            Speed = speed;
            RotSpeed = rotSpeed;
            LastPosition = lastPosition;
            LastRotR = lastRotR;
            Lasttick = lasttick;
        }

        public static List<Prediction> TrackTable = new List<Prediction>();

        public static void SpeedTrack(EventArgs args)
        {
            if (!Game.IsInGame || Game.IsPaused)
                return;
            var me = EntityList.Hero;
            if (me == null) return;

            var heroes = EntityList.GetEntities<Hero>();
            var tick = Environment.TickCount;
            foreach (var unit in heroes)
            {
                var data =
                    TrackTable.FirstOrDefault(
                        unitData => unitData.UnitName == unit.Name || unitData.UnitClassId == unit.ClassId);
                if (data == null && unit.IsAlive && unit.IsVisible)
                {
                    data = new Prediction(unit.Name, unit.ClassId, new Vector3(0, 0, 0), 0, new Vector3(0, 0, 0), 0, 0);
                    TrackTable.Add(data);
                }
                if (data != null && (!unit.IsAlive || !unit.IsVisible))
                {
                    TrackTable.Remove(data);
                    continue;
                }
                if (data == null ||
                    (data.LastPosition != new Vector3(0, 0, 0) && !((tick - data.Lasttick) > 0)))
                    continue;
                if (data.LastPosition == new Vector3(0, 0, 0))
                {
                    data.LastPosition = unit.Position;
                    data.LastRotR = unit.RotationRad;
                    data.Lasttick = tick;
                }
                else
                {
                    data.RotSpeed = data.LastRotR - unit.RotationRad;
                    var speed = (unit.Position - data.LastPosition) / (tick - data.Lasttick);
                    if (Math.Abs(data.RotSpeed) > 0.05 && data.Speed != new Vector3(0, 0, 0))
                    {
                        var rotSpeed = unit.RotationRad + data.RotSpeed;
                        data.Speed = VectorOp.UnitVectorFromXYAngle(rotSpeed) / (tick - data.Lasttick);
                    }
                    else
                    {
                        if (unit.Modifiers.Any(x => x.Name == "modifier_storm_spirit_ball_lightning"))
                        {
                            var ballLightning = Utils.FindSpell(unit, "storm_spirit_ball_lightning");
                            var firstOrDefault = ballLightning.AbilityData.LastOrDefault(x => x.Name == "ball_lightning_move_speed");
                            if (firstOrDefault != null)
                            {
                                var ballSpeed = firstOrDefault.GetValue(2);
                                Console.WriteLine(ballSpeed);
                                var newpredict = VectorOp.UnitVectorFromXYAngle(unit.RotationRad + data.RotSpeed) *
                                            (ballSpeed / 1000);
                                data.Speed = newpredict;
                            } 
                        }
                        else if (unit.LastActivity != 422)
                            data.Speed = speed;
                        else
                        {
                            var newpredict = VectorOp.UnitVectorFromXYAngle(unit.RotationRad + data.RotSpeed) *
                                             (unit.MovementSpeedTotal / 1000);
                            data.Speed = newpredict;
                        }
                    }
                    var predict = PredictedXYZ(unit, 1000);
                    var realspeed = VectorOp.GetDistance2D(predict, unit.Position);
                    if ((realspeed + 100 > unit.MovementSpeedTotal) && unit.LastActivity == 422)
                    {
                        var newpredict = VectorOp.UnitVectorFromXYAngle(unit.RotationRad) * (unit.MovementSpeedTotal / 1000);
                        data.Speed = newpredict;
                    }
                    data.LastPosition = unit.Position;
                    data.LastRotR = unit.RotationRad;
                    data.Lasttick = tick;
                }
            }
        }

        public static Vector3 InFront(Unit unit, float distance)
        {
            var alpha = unit.RotationRad;
            var v = unit.Position + VectorOp.UnitVectorFromXYAngle(alpha) * distance;
            return new Vector3(v.X, v.Y, 0);
        }

        public static Vector3 PredictedXYZ(Unit unit, float delay)
        {
            var data =
                TrackTable.FirstOrDefault(
                    unitData => unitData.UnitName == unit.Name || unitData.UnitClassId == unit.ClassId);
            if (IsIdle(unit) || data == null)
                return unit.Position;
            var fpsTolerancy = ((1 / HeroData.MaxCount) * 3 * (1 + (1 - 1 / HeroData.MaxCount))) * 1000;
            var v = unit.Position + data.Speed * (float)(delay + fpsTolerancy);
            return new Vector3(v.X, v.Y, 0);
        }

        public static Vector3 SkillShotXYZ(Unit source, Unit target, float delay, float speed, float radius)
        {
            var data =
                TrackTable.FirstOrDefault(
                    unitData => unitData.UnitName == target.Name || unitData.UnitClassId == target.ClassId);
            if (IsIdle(target) || data == null)
                return target.Position;
            var predict = PredictedXYZ(target, delay);
            var sourcePos = source.Position;
            var reachTime = (VectorOp.GetDistance2D(predict, sourcePos) / speed) * 1000;
            predict = PredictedXYZ(target, delay + reachTime);
            reachTime = (VectorOp.GetDistance2D(predict, sourcePos) / speed) * 1000;
            predict = PredictedXYZ(target, delay + reachTime);
            sourcePos = (source.Position - predict) *
                        (VectorOp.GetDistance2D(predict, source.Position) - radius / 2) /
                        VectorOp.GetDistance2D(predict, source.Position) +
                        predict;
            reachTime = (VectorOp.GetDistance2D(predict, sourcePos) / speed) * 1000;
            return PredictedXYZ(target, delay + reachTime);
        }

        public static bool AbilityMove(Unit unit)
        {
            return
                unit.Modifiers.Any(
                    x =>
                        x.Name == "modifier_spirit_breaker_charge_of_darkness" ||
                        x.Name == "modifier_earth_spirit_boulder_smash" ||
                        x.Name == "modifier_earth_spirit_rolling_boulder_caster" ||
                        x.Name == "modifier_earth_spirit_geomagnetic_grip" ||
                        x.Name == "modifier_spirit_breaker_charge_of_darkness" ||
                        x.Name == "modifier_huskar_life_break_charge" || x.Name == "modifier_magnataur_skewer_movement" ||
                        x.Name == "modifier_storm_spirit_ball_lightning" || x.Name == "modifier_faceless_void_time_walk" ||
                        x.Name == "modifier_mirana_leap" || x.Name == "modifier_slark_pounce");
        }

        public static bool IsIdle(Unit unit)
        {
            var data =
                TrackTable.FirstOrDefault(
                    unitData => unitData.UnitName == unit.Name || unitData.UnitClassId == unit.ClassId);
            return (data != null && data.Speed == new Vector3(0, 0, 0)) ||
                   unit.Modifiers.Any(x => x.Name == "modifier_eul_cyclone" || x.Name == "modifier_invoker_tornado") ||
                   (unit.LastActivity == 419 && !AbilityMove(unit) &&
                    unit.Modifiers.Any(x => x.Name == "modifier_invoker_deafening_blast_knockback")) ||
                   unit.LastActivity == 424;
        }
    }
}
