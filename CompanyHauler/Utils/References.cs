using CompanyHauler.Scripts;
using GameNetcodeStuff;
using UnityEngine;

namespace CompanyHauler.Utils;

public static class References
{
    // Optimisation
    internal static HaulerController truckController = null!;
    internal static PlayerControllerB lastDriver = null!;
    internal static ItemDropship itemShip = null!;
}
