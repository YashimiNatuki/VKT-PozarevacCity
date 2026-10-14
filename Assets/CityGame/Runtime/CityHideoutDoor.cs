namespace Pozarevac.CityGame
{
    public sealed class CityHideoutDoor : CityInteractable
    {
        private CityHideout hideout;
        private bool enter;
        public void Configure(CityHideout owner, bool isEntrance) { hideout = owner; enter = isEntrance; }
        public override string Prompt => !enter ? "[E] Izađi na ulicu" : hideout.Progression.PhoneUnlocked ? "[E] Uđi u štek / baštu" : "Štek se otključava posle prvog ugovora";
        public override bool TryInteract(CitySession session) => ReferenceEquals(session, hideout.Progression.Session) && hideout.Travel(enter);
    }
}
