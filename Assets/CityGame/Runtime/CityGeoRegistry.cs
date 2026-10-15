using System;
using UnityEngine;

namespace Pozarevac.CityGame
{
    // User-supplied candidate pins, not surveyed district boundaries or safe arrival points.
    public static class CityGeoRegistry
    {
        public readonly struct Location
        {
            public readonly string Id, Name;
            public readonly double Latitude, Longitude;
            public readonly bool ConflictingPin;
            public Location(string id,string name,double latitude,double longitude,bool conflicting=false)
            {Id=id;Name=name;Latitude=latitude;Longitude=longitude;ConflictingPin=conflicting;}
        }
        private static readonly Location[] locations={
            new Location("opstina","Centar / Opština",44.620835,21.184205),
            new Location("kultura","Centar za kulturu",44.619056,21.187722),
            new Location("muzej","Narodni muzej",44.621750,21.186806),
            new Location("cacalica-zvezda","Čačalica / Zvezda",44.613667,21.203778),
            new Location("cacalica-ulaz","Čačalica / ulaz",44.613389,21.191444),
            new Location("ljubicevo","Ljubičevo / Hipodrom — proveriti",44.621006,21.184492,true),
            new Location("zabela","Zabela",44.646700,21.155800),
            new Location("burjan","Burjan",44.614400,21.180200),
            new Location("sopot","Sopot",44.609500,21.171000),
            new Location("seovac","Seovac",44.630000,21.160000),
            new Location("radna-mala","Radna mala",44.624100,21.192500),
            new Location("tulba","Tulba",44.605200,21.186500),
            new Location("kruska","Kruška brdo",44.632100,21.191500),
            new Location("busije","Busije",44.615300,21.171200),
            new Location("trijangl","Trijangl",44.631800,21.175400),
            new Location("cigan-mala","Cigan mala",44.616500,21.195600),
            new Location("meminac","Meminac",44.628500,21.168900),
            new Location("kravlji-do","Kravlji do",44.551000,21.236000)
        };
        public static int Count=>locations.Length;
        public static Location At(int index)=>locations[index];
        public static Vector3 Project(double latitude,double longitude,double originLatitude,double originLongitude)
        {
            if(!Valid(latitude,longitude)||!Valid(originLatitude,originLongitude))throw new ArgumentOutOfRangeException("coordinates");
            double east=(longitude-originLongitude)*111320*Math.Cos(originLatitude*Math.PI/180),north=(latitude-originLatitude)*111320;
            if(Math.Abs(east)>20000||Math.Abs(north)>20000)throw new ArgumentOutOfRangeException("coordinates","Use a separate city origin beyond the local 20 km approximation.");
            return new Vector3((float)east,0,(float)north);
        }
        private static bool Valid(double latitude,double longitude)=>!double.IsNaN(latitude)&&!double.IsInfinity(latitude)&&!double.IsNaN(longitude)&&!double.IsInfinity(longitude)&&Math.Abs(latitude)<=85&&Math.Abs(longitude)<=180;
    }
}
