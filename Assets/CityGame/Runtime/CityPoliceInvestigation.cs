using System;
using UnityEngine;

namespace Pozarevac.CityGame
{
    public enum CityPolicePhase { Patrol, Investigate, Chase, Search }
    public sealed class CityPoliceInvestigation
    {
        private bool known;
        private double searchClock;
        public Vector3 LastKnown {get;private set;}
        public CityPolicePhase Phase {get;private set;}
        public void ReportIncident(Vector3 position)
        {
            CheckPoint(position);known=true;LastKnown=position;searchClock=0;Phase=CityPolicePhase.Investigate;
        }
        public Vector3 Step(double seconds,bool wanted,bool visible,Vector3 observedPlayer,Vector3 officer,Vector3 patrolGoal)
        {
            if(double.IsNaN(seconds)||double.IsInfinity(seconds)||seconds<0)throw new ArgumentOutOfRangeException(nameof(seconds));
            CheckPoint(officer);CheckPoint(patrolGoal);
            if(!wanted){known=false;searchClock=0;Phase=CityPolicePhase.Patrol;return patrolGoal;}
            if(visible){CheckPoint(observedPlayer);known=true;LastKnown=observedPlayer;searchClock=0;Phase=CityPolicePhase.Chase;return LastKnown;}
            // Hidden player coordinates never update the investigation.
            if(!known){Phase=CityPolicePhase.Patrol;return patrolGoal;}
            Vector3 delta=LastKnown-officer;delta.y=0;
            if(Phase!=CityPolicePhase.Search && delta.sqrMagnitude>4){Phase=CityPolicePhase.Investigate;return LastKnown;}
            Phase=CityPolicePhase.Search;searchClock+=seconds;
            float angle=(float)(searchClock*.8),radius=Mathf.Min(4,(float)searchClock*.7f);
            return LastKnown+new Vector3(Mathf.Sin(angle)*radius,0,Mathf.Cos(angle)*radius);
        }
        private static void CheckPoint(Vector3 point)
        {
            if(!float.IsFinite(point.x)||!float.IsFinite(point.y)||!float.IsFinite(point.z))throw new ArgumentException("Non-finite investigation location.");
        }
    }
}
