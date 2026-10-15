using NUnit.Framework;
using UnityEngine;
namespace Pozarevac.CityGame.Tests.Editor
{
    public sealed class CityParkDetailsTests
    {
        [Test] public void FurnitureCannotBePlacedOutsideParkOrAcrossARoad()
        {
            var polygon=new[]{new CityOsmStreets.Point{x=0,z=0},new CityOsmStreets.Point{x=20,z=0},new CityOsmStreets.Point{x=20,z=20},new CityOsmStreets.Point{x=0,z=20}};
            Assert.IsTrue(CityParkDetails.IsInside(new Vector2(10,10),polygon));
            Assert.IsFalse(CityParkDetails.IsInside(new Vector2(25,10),polygon));
            var data=new CityOsmStreets.Data{roads=new[]{new CityOsmStreets.Road{kind="residential",points=new[]{new CityOsmStreets.Point{x=0,z=10},new CityOsmStreets.Point{x=20,z=10}}}}};
            Assert.IsFalse(CityParkDetails.IsClear(new Vector2(10,10),data));
            Assert.IsTrue(CityParkDetails.IsClear(new Vector2(10,19),data));
            data.buildings=new[]{new CityOsmStreets.Building{points=polygon}};
            Assert.IsFalse(CityParkDetails.IsClear(new Vector2(10,19),data));
        }
    }
}
