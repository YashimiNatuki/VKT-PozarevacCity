using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Pozarevac.CityGame.Tests
{
    public sealed class CityParkedCarTests
    {
        [Test]
        public void ExitCandidatesFollowCarRotationAndStopAtFirstClearSide()
        {
            var origin = new Vector3(12f, 3f, -7f);
            var rotation = Quaternion.Euler(0f, 90f, 0f);
            var probes = new List<Vector3>();
            var checks = new List<Vector3>();

            bool found = FindExit(origin, rotation, candidate =>
            {
                probes.Add(candidate);
                return new Vector3(candidate.x, 2.75f, candidate.z);
            }, point =>
            {
                checks.Add(point);
                return checks.Count == 2;
            }, out Vector3 exit);

            Assert.That(found, Is.True);
            Assert.That(probes.Count, Is.EqualTo(2), "A clear second side should avoid a rear probe.");
            Assert.That(checks.Count, Is.EqualTo(2));
            AssertNear(probes[0], origin + new Vector3(0f, 0f, 1.65f));
            AssertNear(probes[1], origin + new Vector3(0f, 0f, -1.65f));
            AssertNear(exit, new Vector3(12f, 2.75f, -8.65f));
            AssertNear(exit, checks[1]);
        }

        [Test]
        public void ExitFallsBackBehindCarWhenSidesLackGroundOrClearance()
        {
            var origin = new Vector3(5f, 3f, 9f);
            var probes = new List<Vector3>();
            var checks = new List<Vector3>();
            bool found = FindExit(origin, Quaternion.identity, candidate =>
            {
                probes.Add(candidate);
                return probes.Count == 1 ? (Vector3?)null : candidate;
            }, point =>
            {
                checks.Add(point);
                return checks.Count == 2;
            }, out Vector3 exit);

            Assert.That(found, Is.True);
            Assert.That(probes.Count, Is.EqualTo(3));
            Assert.That(checks.Count, Is.EqualTo(2), "Missing ground must not trigger a clearance query.");
            AssertNear(checks[0], origin + Vector3.right * 1.65f);
            AssertNear(exit, origin + Vector3.back * 2.65f);
            AssertNear(exit, checks[1]);
        }

        [Test]
        public void ExitReturnsNoPositionWhenEveryGroundedCandidateIsBlocked()
        {
            int checkedExits = 0;
            bool found = FindExit(Vector3.one, Quaternion.identity, point => point, _ =>
            {
                checkedExits++;
                return false;
            }, out Vector3 exit);

            Assert.That(found, Is.False);
            Assert.That(exit, Is.EqualTo(Vector3.zero));
            Assert.That(checkedExits, Is.EqualTo(3));
        }

        [Test]
        public void ExitSkipsNonFiniteGroundBeforeClearance()
        {
            int groundProbes = 0;
            int clearanceChecks = 0;
            bool found = FindExit(Vector3.one, Quaternion.identity, point =>
            {
                groundProbes++;
                return groundProbes == 1
                    ? new Vector3(point.x, float.NaN, point.z)
                    : point;
            }, _ =>
            {
                clearanceChecks++;
                return true;
            }, out Vector3 exit);

            Assert.That(found, Is.True);
            Assert.That(groundProbes, Is.EqualTo(2));
            Assert.That(clearanceChecks, Is.EqualTo(1));
            AssertNear(exit, Vector3.one + Vector3.right * 1.65f);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void NonFiniteCarPoseNeverQueriesGroundOrClearance(bool invalidOrigin)
        {
            Vector3 origin = invalidOrigin ? new Vector3(float.NaN, 0f, 0f) : Vector3.zero;
            Quaternion rotation = invalidOrigin ? Quaternion.identity : new Quaternion(0f, 0f, 0f, float.NaN);

            bool found = FindExit(origin, rotation,
                _ => throw new AssertionException("Ground should not be queried for an invalid pose."),
                _ => throw new AssertionException("Clearance should not be queried for an invalid pose."),
                out Vector3 exit);

            Assert.That(found, Is.False);
            Assert.That(exit, Is.EqualTo(Vector3.zero));
        }

        private static bool FindExit(Vector3 origin, Quaternion rotation, Func<Vector3, Vector3?> resolveGround,
            Func<Vector3, bool> isClear, out Vector3 exit)
        {
            Type car = typeof(CityInteractable).Assembly.GetType("Pozarevac.CityGame.CityParkedCar");
            Assert.That(car, Is.Not.Null, "The parked car runtime must exist.");
            var find = car.GetMethod("TryFindExit", BindingFlags.Public | BindingFlags.Static, null,
                new[] { typeof(Vector3), typeof(Quaternion), typeof(Func<Vector3, Vector3?>), typeof(Func<Vector3, bool>),
                    typeof(Vector3).MakeByRefType() }, null);
            Assert.That(find, Is.Not.Null, "The runtime must expose a deterministic, testable exit search.");
            object[] arguments = { origin, rotation, resolveGround, isClear, Vector3.zero };
            bool found = (bool)find.Invoke(null, arguments);
            exit = (Vector3)arguments[4];
            return found;
        }

        private static void AssertNear(Vector3 actual, Vector3 expected)
        {
            Assert.That(Vector3.Distance(actual, expected), Is.LessThan(.0001f), $"Expected {expected}, got {actual}.");
        }
    }
}
