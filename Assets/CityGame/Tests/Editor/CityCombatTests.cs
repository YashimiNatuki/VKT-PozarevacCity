using NUnit.Framework;

namespace Pozarevac.CityGame.Tests.Editor
{
    public sealed class CityCombatTests
    {
        [Test]
        public void StoneInventoryIsBoundedAndOnlySuccessfulAttackConsumesAmmo()
        {
            CityCombat combat=new CityCombat();
            for(int i=0;i<CityCombat.MaxStones;i++)Assert.That(combat.TryCollectStone(),Is.True);
            Assert.That(combat.TryCollectStone(),Is.False);
            Assert.That(combat.SelectWeapon(CityWeapon.Stone),Is.True);
            Assert.That(combat.TryAttack(false,out _),Is.False,"Dead or paused actors must not fire.");
            Assert.That(combat.StoneCount,Is.EqualTo(CityCombat.MaxStones));Assert.That(combat.CooldownRemaining,Is.Zero);
            Assert.That(combat.TryAttack(true,out CityAttack attack),Is.True);
            Assert.That(attack.ProjectileSpeed,Is.GreaterThan(0));Assert.That(attack.Damage,Is.GreaterThan(0));
            Assert.That(combat.TryAttack(true,out _),Is.False);
            Assert.That(combat.StoneCount,Is.EqualTo(CityCombat.MaxStones-1));
        }
        [Test]
        public void SwitchingWeaponsDoesNotBypassCooldownAndInvalidTimeDoesNotAdvanceIt()
        {
            CityCombat combat=new CityCombat();combat.CollectStick();
            Assert.That(combat.TryAttack(true,out _),Is.True);
            combat.SelectWeapon(CityWeapon.Stick);
            combat.Tick(double.NaN);combat.Tick(double.PositiveInfinity);combat.Tick(-2);combat.Tick(0);
            Assert.That(combat.TryAttack(true,out _),Is.False);
            combat.Tick(.65d);
            Assert.That(combat.TryAttack(true,out CityAttack stick),Is.True);
            Assert.That(stick.Weapon,Is.EqualTo(CityWeapon.Stick));
            Assert.That(stick.Range,Is.LessThan(3));Assert.That(combat.HasStick,Is.True);
            combat.Tick(10);Assert.That(combat.TryAttack(true,out _),Is.True);
        }
        [Test]
        public void EmptyWeaponCannotAttackAndCycleAlwaysFindsAvailableFallback()
        {
            CityCombat combat=new CityCombat();
            Assert.That(combat.SelectWeapon(CityWeapon.Stick),Is.False);
            Assert.That(combat.SelectWeapon((CityWeapon)999),Is.False);
            Assert.That(combat.CycleWeapon(),Is.EqualTo(CityWeapon.Unarmed));
            combat.TryCollectStone();combat.SelectWeapon(CityWeapon.Stone);combat.TryAttack(true,out _);combat.Tick(1);
            Assert.That(combat.TryAttack(true,out _),Is.False);Assert.That(combat.StoneCount,Is.Zero);
            Assert.That(combat.CycleWeapon(),Is.EqualTo(CityWeapon.Unarmed));
            Assert.That(combat.TryAttack(true,out CityAttack punch),Is.True);
            Assert.That(punch.Weapon,Is.EqualTo(CityWeapon.Unarmed));Assert.That(punch.ProjectileSpeed,Is.Zero);
            Assert.That(combat.CollectStick(),Is.True);Assert.That(combat.CollectStick(),Is.False);
            Assert.That(combat.CycleWeapon(),Is.EqualTo(CityWeapon.Stick));
        }
    }
}
