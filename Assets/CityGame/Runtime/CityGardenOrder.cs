using System;

namespace Pozarevac.CityGame
{
    public enum CityGardenOrderPhase { Idle, Deliver, Receipt }

    [Serializable]
    public sealed class CityGardenOrderData
    {
        public CityGardenOrderPhase phase;
        public int variety, quantity, cashReward, completed;
        public double remainingSeconds, cooldownSeconds;
    }

    // Cargo is removed at acceptance. Cancellation destroys cargo; it never creates stock or pays.
    public sealed class CityGardenOrder
    {
        public const int Quantity = 2, DeliveryStation = 1, ReputationReward = 3;
        public const double TimeLimit = 180;
        private readonly CitySession session;
        private readonly CityGarden garden;
        private readonly CityLifestyle lifestyle;
        private readonly CityGardenOrderData data;
        public event Action Changed;
        public CityGardenOrderPhase Phase => data.phase;
        public bool IsActive => Phase != CityGardenOrderPhase.Idle;
        public bool HasCargo => Phase == CityGardenOrderPhase.Deliver;
        public int Variety => data.variety;
        public int CashReward => data.cashReward;
        public int Completed => data.completed;
        public double RemainingSeconds => data.remainingSeconds;
        public double CooldownSeconds => data.cooldownSeconds;
        public string Message { get; private set; } = "Pult PORUDŽBINE u šteku: spakuj 2 komada iz zaliha.";
        public string BlockReason => lifestyle.Wanted ? "POTERA / izgubi policiju pre kontakta." :
            lifestyle.IsTraining || lifestyle.IsBoxing || lifestyle.IsConditioning ? "TRENING / prvo završi ili prekini trening." :
            session.Heat > 45 ? "Kure čeka da HEAT padne na 45 ili niže." : "";

        public CityGardenOrder(CitySession wallet, CityGarden inventory, CityLifestyle life, CityGardenOrderData saved = null)
        {
            session = wallet ?? throw new ArgumentNullException(nameof(wallet));
            garden = inventory ?? throw new ArgumentNullException(nameof(inventory));
            lifestyle = life ?? throw new ArgumentNullException(nameof(life));
            Validate(saved); data = Copy(saved) ?? new CityGardenOrderData();
            if (IsActive) Message = HasCargo ? "Paket učitan. Predaj ga Kuretu kod markera 2." : "Potvrda učitana. Vrati se na pult PORUDŽBINE u šteku.";
        }

        public static int Quote(int variety) => checked(CityPlantCatalog.Get(variety).BaseUnitPrice * Quantity + 80);

        internal bool TryAccept(int variety)
        {
            int reward = Quote(variety);
            if (IsActive || CooldownSeconds > 0 || BlockReason.Length > 0 || !garden.IsUnlocked(variety) || session.Cash > int.MaxValue - reward)
            { Message = BlockReason.Length > 0 ? BlockReason : "Porudžbina nije dostupna: proveri odmor kontakta, reputaciju i novac."; return false; }
            if (!garden.TryTakeStock(variety, Quantity))
            { Message = "Nema 2 komada izabranog strejna u skladištu. Prvo završi berbu."; return false; }
            data.phase = CityGardenOrderPhase.Deliver; data.variety = variety; data.quantity = Quantity;
            data.cashReward = reward; data.remainingSeconds = TimeLimit; data.cooldownSeconds = 0;
            Message = "Spakovano: 2 × " + CityGarden.VarietyName(variety) + ". Izađi iz šteka i prati marker 2. Otkaz/istek roka gube paket.";
            Changed?.Invoke(); return true;
        }

        public bool TryDeliver()
        {
            if (!HasCargo || BlockReason.Length > 0) { if (IsActive && BlockReason.Length > 0) Message = BlockReason; return false; }
            data.phase = CityGardenOrderPhase.Receipt;
            Message = "Kure: Roba je preuzeta. Vrati potvrdu na pult PORUDŽBINE u šteku za isplatu.";
            Changed?.Invoke(); return true;
        }

        public bool TrySettle()
        {
            if (Phase != CityGardenOrderPhase.Receipt || BlockReason.Length > 0) return false;
            int reward = CashReward;
            if (session.Cash > int.MaxValue - reward) { Message = "Novčanik je pun. Potroši deo novca pre naplate."; return false; }
            Reset(30); data.completed = Math.Min(CityStreetJobs.MaximumCompletedJobs - 1, data.completed) + 1;
            Message = "Porudžbina završena: +" + reward + " DIN / +3 REP. Novi dogovor za 30 s igre.";
            // All terminal fields are committed before wallet observers can capture the same atomic profile.
            session.Earn(reward, ReputationReward);
            Changed?.Invoke(); return true;
        }

        public bool Cancel(string reason = "Porudžbina otkazana: paket/potvrda izgubljeni, bez povraćaja robe i bez isplate.")
        {
            if (!IsActive) return false;
            if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("A reason is required.", nameof(reason));
            Reset(15); Message = reason; Changed?.Invoke(); return true;
        }

        public void Tick(double seconds)
        {
            if (!double.IsFinite(seconds) || seconds < 0) throw new ArgumentOutOfRangeException(nameof(seconds));
            if (!IsActive) { data.cooldownSeconds = Math.Max(0, CooldownSeconds - seconds); return; }
            data.remainingSeconds = Math.Max(0, RemainingSeconds - seconds);
            if (RemainingSeconds == 0) Cancel("Rok porudžbine je istekao. Paket/potvrda izgubljeni; nema povraćaja robe ni isplate.");
        }

        private void Reset(double cooldown)
        {
            data.phase = CityGardenOrderPhase.Idle; data.variety = data.quantity = data.cashReward = 0;
            data.remainingSeconds = 0; data.cooldownSeconds = cooldown;
        }
        public CityGardenOrderData Snapshot() => Copy(data);
        public static CityGardenOrderData Copy(CityGardenOrderData value) => value == null ? null : new CityGardenOrderData
        {
            phase=value.phase, variety=value.variety, quantity=value.quantity, cashReward=value.cashReward,
            completed=value.completed, remainingSeconds=value.remainingSeconds, cooldownSeconds=value.cooldownSeconds
        };
        public static bool IsEmpty(CityGardenOrderData value) => value == null || value.phase == 0 && value.variety == 0 && value.quantity == 0 &&
            value.cashReward == 0 && value.completed == 0 && value.remainingSeconds == 0 && value.cooldownSeconds == 0;
        public static void Validate(CityGardenOrderData value)
        {
            if (value == null) return;
            if (!Enum.IsDefined(typeof(CityGardenOrderPhase), value.phase) || value.completed < 0 || value.completed > CityStreetJobs.MaximumCompletedJobs ||
                !double.IsFinite(value.remainingSeconds) || value.remainingSeconds < 0 || value.remainingSeconds > TimeLimit ||
                !double.IsFinite(value.cooldownSeconds) || value.cooldownSeconds < 0 || value.cooldownSeconds > 30)
                throw new ArgumentException("Invalid garden order state.");
            if (value.phase == CityGardenOrderPhase.Idle)
            {
                if (value.variety != 0 || value.quantity != 0 || value.cashReward != 0 || value.remainingSeconds != 0)
                    throw new ArgumentException("Idle garden order retains cargo or reward.");
            }
            else if (value.variety < 0 || value.variety >= CityPlantCatalog.Count || value.quantity != Quantity ||
                value.cashReward <= 0 || value.cashReward > 1000000 || value.cooldownSeconds != 0 || value.remainingSeconds <= 0)
                throw new ArgumentException("Active garden order has invalid cargo or deadline.");
        }
    }
}
