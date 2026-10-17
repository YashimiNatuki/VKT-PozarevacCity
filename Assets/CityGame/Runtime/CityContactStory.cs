using System;

namespace Pozarevac.CityGame
{
    [Serializable] public sealed class CityContactFacts
    {
        public string contact,intent;
        public int chapter,campaign,heat,nutrition,stamina;
        public bool job_active;
    }
    [Serializable] public sealed class CityContactReply
    {
        public string title,line,tip,source,action,reason,model;
        public int retry_after;
    }
    [Serializable] public sealed class CityStoryHealth
    {
        public string service,status,reason,model;
        public bool available;
    }
    public static class CityContactStory
    {
        public static bool ValidHealth(CityStoryHealth health)=>health!=null&&health.service=="vkt-local-story"&&health.status=="ready";
        public static string Status(string reason)
        {
            switch(reason)
            {
                case "not_requested":return "Autorske replike su spremne. OSVEŽI proverava lokalni AI.";
                case "phone_offline":return "Telefon koristi autorske replike. PC AI servis nije povezan.";
                case "loading":return "Model se učitava. Sačekaj nekoliko sekundi pa osveži.";
                case "busy":return "Model već obrađuje razgovor. Sačekaj pa osveži.";
                case "model_missing":return "Izabrani model nije instaliran. Proveri AI uputstvo na računaru.";
                case "ollama_unavailable":return "Ollama ne odgovara. Pokreni Ollama na računaru pa osveži.";
                case "rate_limited":return "Dostignut je limit razgovora. Sačekaj pa osveži.";
                case "circuit_open":return "AI pravi kratku pauzu posle grešaka. Osveži kasnije.";
                case "deadline":return "AI nije odgovorio na vreme. Autorska replika ostaje dostupna.";
                case "generation_unavailable":return "Model nije vratio ispravnu repliku. Možeš ponovo da osvežiš.";
                case "suspended":return "Razgovor je prekinut pri pauziranju aplikacije. Osveži za novu vezu.";
                case "wrong_service":return "Na AI adresi je drugi servis. Proveri VKT LocalAI pokretač.";
                default:return "VKT AI servis nije dostupan. Pokreni Start-LocalAI.ps1 na računaru.";
            }
        }
        public static string Action(CityContactFacts facts)
        {
            if(facts.heat>=30)return "evade";
            if(facts.nutrition<20)return "meal";
            if(facts.stamina<30)return "rest";
            if(facts.campaign<3)return "campaign";
            return facts.job_active?"delivery":"jobs";
        }
        public static string Tip(string action)
        {
            switch(action)
            {
                case "evade":return "Potera u igri ima prednost. Završi je pre novog posla.";
                case "meal":return "Priđi kiosku i proveri obrok u BLOK / LIK meniju.";
                case "rest":return "Odmori lika. Energija se obnavlja dok ne sprintuješ.";
                case "campaign":return "Prati označeni glavni ugovor i koristi E na cilju.";
                case "delivery":return "Aktivna ruta i rok su u DNEVNIK / POSLOVI.";
                default:return "DNEVNIK / POSLOVI prikazuje trenutnu ponudu i uslove.";
            }
        }
        public static CityContactReply Offline(CityContactFacts facts,string reason)
        {
            string name=facts.contact=="mila"?"Mila":facts.contact=="vuk"?"Vuk":"Luka";
            string action=Action(facts);
            return new CityContactReply{title=name+" / veza iz bloka",
                line="Reč se drži, brate. Proveri stanje lika i cilj u dnevniku. Blok pamti pouzdane ljude.",
                tip=Tip(action),source="offline",action=action,reason=reason,model=""};
        }
        public static bool Valid(CityContactReply reply,string expectedAction)
        {
            return reply!=null&&reply.action==expectedAction&&
                (reply.source=="ollama"||reply.source=="cache"||reply.source=="offline")&&
                Plain(reply.title,3,60)&&Plain(reply.line,8,260)&&Plain(reply.tip,5,160);
        }
        private static bool Plain(string value,int min,int max)
        {
            if(string.IsNullOrWhiteSpace(value)||value.Length<min||value.Length>max)return false;
            foreach(char c in value)if(char.IsControl(c)||c=='<'||c=='>'||c=='`')return false;
            string lower=value.ToLowerInvariant();return !lower.Contains("http:")&&!lower.Contains("https:")&&!lower.Contains("www.");
        }
    }
}
