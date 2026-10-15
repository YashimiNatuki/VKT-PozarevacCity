using System;

namespace Pozarevac.CityGame
{
    public enum CityWeapon { Unarmed, Stone, Stick }

    public readonly struct CityAttack
    {
        public CityWeapon Weapon { get; }
        public float Range { get; }
        public float Damage { get; }
        public float ProjectileSpeed { get; }
        public CityAttack(CityWeapon weapon,float range,float damage,float projectileSpeed)
        { Weapon=weapon;Range=range;Damage=damage;ProjectileSpeed=projectileSpeed; }
    }

    // Session-only fictional game combat; Unity owns targeting, LOS and projectile collision.
    public sealed class CityCombat
    {
        public const int MaxStones=12;
        public CityWeapon SelectedWeapon {get;private set;}
        public int StoneCount {get;private set;}
        public bool HasStick {get;private set;}
        public double CooldownRemaining {get;private set;}
        public bool TryCollectStone()
        {
            if(StoneCount>=MaxStones)return false;
            StoneCount++;return true;
        }
        public bool CollectStick()
        {
            if(HasStick)return false;
            HasStick=true;return true;
        }
        public bool SelectWeapon(CityWeapon weapon)
        {
            if(!Available(weapon))return false;
            SelectedWeapon=weapon;return true;
        }
        public CityWeapon CycleWeapon()
        {
            for(int step=1;step<=3;step++)
            {
                CityWeapon candidate=(CityWeapon)(((int)SelectedWeapon+step)%3);
                if(Available(candidate)){SelectedWeapon=candidate;break;}
            }
            return SelectedWeapon;
        }
        public bool TryAttack(bool canAct,out CityAttack attack)
        {
            attack=default;
            if(!canAct||CooldownRemaining>0||!Available(SelectedWeapon))return false;
            switch(SelectedWeapon)
            {
                case CityWeapon.Stone:
                    CooldownRemaining=.8d;StoneCount--;attack=new CityAttack(CityWeapon.Stone,18f,12f,14f);break;
                case CityWeapon.Stick:
                    CooldownRemaining=.9d;attack=new CityAttack(CityWeapon.Stick,2.1f,15f,0f);break;
                default:
                    CooldownRemaining=.65d;attack=new CityAttack(CityWeapon.Unarmed,1.6f,6f,0f);break;
            }
            return true;
        }
        public void Tick(double seconds)
        {
            if(double.IsNaN(seconds)||double.IsInfinity(seconds)||seconds<=0)return;
            CooldownRemaining=Math.Max(0d,CooldownRemaining-seconds);
        }
        private bool Available(CityWeapon weapon)
        {
            switch(weapon)
            {
                case CityWeapon.Unarmed:return true;
                case CityWeapon.Stone:return StoneCount>0;
                case CityWeapon.Stick:return HasStick;
                default:return false;
            }
        }
    }
}
