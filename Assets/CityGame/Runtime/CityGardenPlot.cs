namespace Pozarevac.CityGame
{
    public sealed class CityGardenPlot : CityInteractable
    {
        private CityHideout hideout;
        private int index;
        public void Configure(CityHideout owner, int plotIndex) { hideout = owner; index = plotIndex; }
        public override string Prompt => hideout.Prompt(index);
        public override bool TryInteract(CitySession session) => ReferenceEquals(session, hideout.Progression.Session) && hideout.Act(index);
    }
}
