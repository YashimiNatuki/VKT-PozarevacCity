using System;
namespace Pozarevac.CityGame
{
    public enum CityOutfit { Korzo, Midnight, Busije }
    [Serializable] public sealed class CityAppearanceData { public int outfit; }
    public sealed class CityAppearance
    {
        public event Action Changed;
        public CityOutfit Outfit { get; private set; }
        public CityAppearance(CityAppearanceData saved = null) { Validate(saved); Outfit=(CityOutfit)(saved?.outfit??0); }
        public static void Validate(CityAppearanceData saved)
        { if(saved!=null&&(saved.outfit<0||saved.outfit>2))throw new ArgumentException("Invalid character outfit."); }
        public bool Select(CityOutfit outfit)
        {
            if((int)outfit<0||(int)outfit>2)throw new ArgumentOutOfRangeException(nameof(outfit));
            if(Outfit==outfit)return false;
            Outfit=outfit;Changed?.Invoke();return true;
        }
        public CityAppearanceData Snapshot()=>new CityAppearanceData{outfit=(int)Outfit};
        public static CityAppearanceData Copy(CityAppearanceData data)=>data==null?null:new CityAppearanceData{outfit=data.outfit};
        public static string Name(CityOutfit outfit)=>outfit==CityOutfit.Korzo?"KORZO / petrolej":outfit==CityOutfit.Midnight?"NOĆNI BLOK / crna":"BUSIJE / bordo";
    }
}
