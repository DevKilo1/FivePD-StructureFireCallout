using CitizenFX.Core;
using FivePD.API.Utils;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CitizenFX.Core.Native;
using CitizenFX.Core.NaturalMotion;
using FivePD.API;
using Newtonsoft.Json.Linq;

namespace FivePD_StructureFireCallout
{
    internal class Utils
    {
        public static void CalloutError(Exception err, Callout callout)
        {
            Debug.WriteLine("^3=================^2Kilo's Structure Fire Callout^3=================");
            Debug.WriteLine("^8ERROR DETECTED^7: "+err.Message);
            Debug.WriteLine("^3=================^2Kilo's Structure Fire Callout^3=================");
            Debug.WriteLine("^5Callout Ended");
            callout.EndCallout();
        }
        public static async Task TaskPedTakeTargetPedHostage(Ped ped, Ped targetPed)
        {
            // Define parameters
            string suspectAnimDict = "anim@gangops@hostage@";
            string suspectAnimSet = "perp_idle";
            string victimAnimDict = suspectAnimDict;
            string victimAnimSet = "victim_idle";
            // Request memory resources
            await RequestAnimDict(suspectAnimDict);
            await RequestAnimDict(victimAnimDict);
            RequestAnimSet(suspectAnimSet);
            RequestAnimSet(victimAnimSet);
            // Code
            if (ped == null)
                Debug.WriteLine("Ped is null");
            if (targetPed == null)
                Debug.WriteLine("Target is null");
            ped.Task.ClearSecondary();
            ped.Detach();

            API.AttachEntityToEntity(targetPed.Handle, ped.Handle, 0, -0.24f, 0.11f, 0f, 0.5f, 0.5f, 0f, false, false,
                false, false, 2, false);
            ped.Task.PlayAnimation(suspectAnimDict, suspectAnimSet, 8f, -8f, -1, AnimationFlags.Loop, 1f);
            targetPed.Task.PlayAnimation(victimAnimDict, victimAnimSet, 8f, -8f, -1, AnimationFlags.Loop, 1f);
        }

        public static async Task RequestAnimDict(string animDict)
        {
            if (!API.HasAnimDictLoaded(animDict))
                API.RequestAnimDict(animDict);
            while (!API.HasAnimDictLoaded(animDict))
                await BaseScript.Delay(100);
        }

        public static async Task RequestAnimSet(string animSet)
        {
            if (!API.HasAnimSetLoaded(animSet))
                API.RequestAnimSet(animSet);
            while (!API.HasAnimSetLoaded(animSet))
                await BaseScript.Delay(100);
        }

        public static void UnloadAnimDict(string animDict)
        {
            API.RemoveAnimDict(animDict);
        }

        public static void UnloadAnimSet(string animSet)
        {
            API.RemoveAnimSet(animSet);
        }
        
        public static List<Entity> EntitiesInMemory = new List<Entity>();
        public static async Task<Ped> SpawnPedOneSync(PedHash pedHash, Vector3 location, [Optional] bool keepTask, [Optional] float heading)
        {
           Ped ped = await World.CreatePed(new(pedHash), location, heading);
           ped.IsPersistent = true;
           EntitiesInMemory.Add(ped);
           if (keepTask)
           {
               ped.AlwaysKeepTask = true;
               ped.BlockPermanentEvents = true;
           }

           return ped;
        }

        public static void ReleaseEntity(Entity ent)
        {
            if (ent.Model.IsPed)
            {
                Ped ped = (Ped)ent;
                ped.AlwaysKeepTask = false;
                ped.BlockPermanentEvents = false;
            }
            if (EntitiesInMemory.Contains(ent))
                EntitiesInMemory.Remove(ent);
            ent.IsPersistent = false;
        }
        public static Vector4 GetRandomLocationFromArray(Array array)
        {
            var chance = new Random().Next(array.Length - 1);
            var result = array.GetValue(chance);
            return (Vector4)result;
        }
        public static List<Vector4> GetLocationArrayFromJArray(JArray jObject, string name)
        {
            List<Vector4> locationArray = new List<Vector4>();
            var locationsObject = (JArray)jObject;
            foreach (JObject jToken in locationsObject)
            {
                if (jToken.GetValue("x") == null)
                    Debug.WriteLine("No x");
                locationArray.Add(JSONCoordsToVector4((JObject)jToken[name]));
            }

            return locationArray;
        }
        public static class RandomProvider
        {    
            private static int seed = Environment.TickCount;

            private static ThreadLocal<Random> randomWrapper = new ThreadLocal<Random>
                (() => new Random(Interlocked.Increment(ref seed)));

            public static Random GetThreadRandom()
            {
                return randomWrapper.Value;
            }
        }
        
        public static JObject GetChildFromParent(JToken parent, string name)
        {
            return (JObject)parent[name];
        }
        public static JObject GetConfig()
        {
            JObject result = null;
            try
            {
                string data = API.LoadResourceFile("fivepd", "callouts/KiloStructureFire/settings.json");
                result = JObject.Parse(data);
            }
            catch (Exception err)
            {
                Debug.WriteLine("^8ERROR^7: ^3Please add the following line into the 'files' section of your fivepd fxmanifest.lua:");
                Debug.WriteLine("^1===================================================================================================");
                Debug.WriteLine("^9'callouts/KiloStructureFire/*.json'");
                Debug.WriteLine("^1===================================================================================================");
                Debug.WriteLine("^8ERROR (Kilo's Structure Fire)^7: ^2 The above line will allow the callout to read the json files here in the callout folder!");
            }
            return result;
        }

        public static Vector3 GetRandomLocationInRadius(int min, int max)
        {
            int distance = new Random().Next(min, max);
            float offsetX = new Random().Next(-1 * distance, distance);
            float offsetY = new Random().Next(-1 * distance, distance);


            return World.GetNextPositionOnStreet(Game.PlayerPed.GetOffsetPosition(new Vector3(offsetX, offsetY, 0)));
        }
        
        public static async Task PedTaskSassyChatAnimation(Ped ped)
        {
            if (!API.HasAnimDictLoaded("oddjobs@assassinate@vice@hooker"))
                API.RequestAnimDict("oddjobs@assassinate@vice@hooker");
            if (!API.HasAnimSetLoaded("argue_b"))
                API.RequestAnimSet("argue_b");
            while (!API.HasAnimDictLoaded("oddjobs@assassinate@vice@hooker"))
                await BaseScript.Delay(200);
            ped.Task.ClearAllImmediately();
            ped.Task.PlayAnimation("oddjobs@assassinate@vice@hooker", "argue_b", 8f, 8f, 10000, AnimationFlags.Loop,
                1f);
        }

        public static async Task KeepTaskEnterVehicle(Ped ped, Vehicle veh, VehicleSeat targetSeat)
        {
            SetIntoVehicleAfterTimer(ped, veh, VehicleSeat.Any, 30000);
            while (true)
            {
                Vector3 startPos = ped.Position;
                await BaseScript.Delay(2500);
                if (!ped.IsInVehicle(veh) && ped.Position.DistanceTo(startPos) < 1f)
                    ped.Task.EnterVehicle(veh, targetSeat);
                await BaseScript.Delay(2500);
            }
        }

        public static async Task WaitUntilPedIsInVehicle(Ped ped, Vehicle veh, VehicleSeat targetSeat)
        {
            while (true)
            {
                if (ped.IsInVehicle(veh) || ped.IsInVehicle() && ped.SeatIndex == targetSeat)
                    return;
                await BaseScript.Delay(500);
            }
        }

        public static async Task SetIntoVehicleAfterTimer(Ped ped, Vehicle veh, VehicleSeat targetSeat, int ms)
        {
            await BaseScript.Delay(ms);
            if (!ped.IsInVehicle(veh))
            {
                ped.SetIntoVehicle(veh, targetSeat);
            }
        }
        public static async Task KeepTaskGoToForPed(Ped ped, Vector3 pos, float buffer)
        {
            Vector3 startPos = ped.Position;
            while (true)
            {
                await BaseScript.Delay(1000);
                if (ped.Position == startPos)
                {
                    ped.Task.GoTo(pos);
                }

                if (ped.Position.DistanceTo(pos) < buffer)
                    return;
                await BaseScript.Delay(1000);
            }
        }
        public static async Task WaitUntilEntityIsAtPos(Entity ent, Vector3 pos, float buffer)
        {
            if (ent == null || !ent.Exists())
                return;
            while (true)
            {
                if (ent == null || !ent.Exists())
                    return;
                if (ent.Position.DistanceTo(pos) < buffer)
                    break;
                await BaseScript.Delay(200);
            }
        }
        public static PedHash GetRandomPed()
        {
            return RandomUtils.GetRandomPed(exclusions);
        }

        public static Vector3 JSONCoordsToVector3(JObject coordsObj)
        {
            return new Vector3((float)coordsObj["x"], (float)coordsObj["y"], (float)coordsObj["z"]);
        }
        public static Vector4 JSONCoordsToVector4(JObject coordsObj)
        {
            return new Vector4((float)coordsObj["x"], (float)coordsObj["y"], (float)coordsObj["z"],
                (float)coordsObj["w"]);
        }
        
        public static void KeepTask(Ped ped)
        {
            if (ped == null || !ped.Exists()) return;
            ped.IsPersistent = true;
            ped.AlwaysKeepTask = true;
            ped.BlockPermanentEvents = true;
        }

        public static void UnKeepTask(Ped ped)
        {
            ped.IsPersistent = false;
            ped.AlwaysKeepTask = false;
            ped.BlockPermanentEvents = false;
        }
        
        public static Vector3[] ConvenienceLocations = new Vector3[]
        {
            new(-712.12f, -913.06f, 19.22f),
            new(29.49f, -1346.94f, 29.5f),
            new(-50.78f, -1753.61f, 29.42f),
            new(376.4f, 325.75f, 103.57f),
            new(-1223.94f, -906.52f, 12.33f)
        };
        public static Vector3[] HomeLocations = new Vector3[]
        {
            new(-120.15f, -1574.39f, 34.18f),
            new(-148.07f, -1596.64f, 38.21f),
            new(-32.44f, -1446.5f, 31.89f),
            new(-14.11f, -1441.93f, 31.1f),
            new(72.21f, -1938.59f, 21.37f),
            new(126.68f, -1930.01f, 21.38f),
            new(270.2f, -1917.19f, 26.18f),
            new(325.68f, -2050.86f, 20.93f),
            new(1099.52f, -438.65f, 67.79f),
            new(1046.24f, -498.14f, 64.28f),
            new(980.1f, -627.29f, 59.24f),
            new(943.45f, -653.49f, 58.43f),
            new(1223.08f, -696.85f, 60.8f),
            new(1201.06f, -575.68f, 69.14f),
            new(1265.9f, -648.33f, 67.92f),
            new(1241.5f, -566.4f, 69.66f),
            new(1204.73f, -557.74f, 69.62f),
            new(1223.06f, -696.74f, 60.81f),
            new(930.88f, -244.82f, 69.0f),
            new(880.01f, -205.01f, 71.98f),
            new(798.39f, -158.83f, 74.89f),
            new(820.86f, -155.84f, 80.75f), // Second floor
            new(208.65f, 74.53f, 87.9f),
            new(119.34f, 494.13f, 147.34f),
            new(79.74f, 486.13f, 148.2f),
            new(151.2f, 556.09f, 183.74f),
            new(232.1f, 672.06f, 189.98f),
            new(-66.76f, 490.13f, 144.88f),
            new(-175.94f, 502.73f, 137.42f),
            new(-230.26f, 488.29f, 128.77f),
            new(-355.91f, 469.56f, 112.61f),
            new(-353.17f, 423.13f, 110.98f),
            new(-312.53f, 474.91f, 111.83f),
            new(-348.99f, 514.99f, 120.65f),
            new(-376.59f, 547.66f, 123.85f),
            new(-406.6f, 566.28f, 124.61f),
            new(-520.28f, 594.07f, 120.84f),
            new(-581.37f, 494.04f, 108.26f),
            new(-678.67f, 511.67f, 113.53f),
            new(-784.46f, 459.47f, 100.25f),
            new(-824.67f, 422.6f, 92.13f),
            new(-881.97f, 364.1f, 85.36f),
            new(-967.59f, 436.88f, 80.57f),
            new(-1570.71f, 23.0f, 59.55f),
            new(-1629.9f, 36.25f, 62.94f),
            new(-1750.22f, -695.19f, 11.75f),
            new(-1270.03f, -1296.53f, 4.0f),
            new(-1148.96f, -1523.2f, 10.63f),
            new(-1105.61f, -1596.67f, 4.61f)
        };

        public static bool IsPedNonLethalOrMelee(Ped ped)
        {
            WeaponHash weapon = ped.Weapons.Current;
            return nonlethals.Contains(weapon) || melee.Contains(weapon);
        }

        public static WeaponHash[] nonlethals =
        {
            WeaponHash.Ball,
            WeaponHash.Parachute,
            WeaponHash.Flare,
            WeaponHash.Snowball,
            WeaponHash.Unarmed,
            WeaponHash.StunGun,
            WeaponHash.FireExtinguisher
        };

        public static WeaponHash[] melee =
        {
            WeaponHash.Crowbar,
            WeaponHash.Bat,
            WeaponHash.Bottle,
            WeaponHash.Flashlight,
            WeaponHash.Hatchet,
            WeaponHash.Knife,
            WeaponHash.Machete,
            WeaponHash.Nightstick,
            WeaponHash.Unarmed,
            WeaponHash.PoolCue,
            WeaponHash.StoneHatchet
        };

        public static VehicleHash GetRandomVehicleForRobberies()
        {
            return RandomUtils.GetRandomVehicle(FourPersonVehicleClasses);
        }

        public static IEnumerable<VehicleClass> FourPersonVehicleClasses = new List<VehicleClass>()
        {
            VehicleClass.Compacts,
            VehicleClass.Sedans,
            VehicleClass.Vans,
            VehicleClass.SUVs
        };
        
        public static PedHash GetRandomSuspect()
        {
            return suspects[new Random().Next(suspects.Length - 1)];
        }
        public static WeaponHash GetRandomWeapon()
        {
            int index = new Random().Next(weapons.Length);
            return weapons[index];
        }

        public static WeaponHash[] weapons =
        {
            WeaponHash.AssaultRifle,
            WeaponHash.PumpShotgun,
            WeaponHash.CombatPistol
        };

        public static PedHash[] suspects =
        {
            PedHash.MerryWeatherCutscene,
            PedHash.Armymech01SMY,
            PedHash.MerryWeatherCutscene,
            PedHash.ChemSec01SMM,
            PedHash.Blackops01SMY,
            PedHash.CiaSec01SMM,
            PedHash.PestContDriver,
            PedHash.PestContGunman,
            PedHash.TaoCheng,
            PedHash.Hunter,
            PedHash.EdToh,
            PedHash.PrologueMournMale01,
            PedHash.PoloGoon01GMY
        };

        public static IEnumerable<WeaponHash> weapExclusions = new List<WeaponHash>
        {
            WeaponHash.Ball,
            WeaponHash.Bat,
            WeaponHash.Snowball,
            WeaponHash.RayMinigun,
            WeaponHash.RayCarbine,
            WeaponHash.BattleAxe,
            WeaponHash.Bottle,
            WeaponHash.BZGas,
            WeaponHash.Crowbar,
            WeaponHash.Dagger,
            WeaponHash.FireExtinguisher,
            WeaponHash.Firework,
            WeaponHash.Flare,
            WeaponHash.FlareGun,
            WeaponHash.Flashlight,
            WeaponHash.GolfClub,
            WeaponHash.Grenade,
            WeaponHash.GrenadeLauncher,
            WeaponHash.Gusenberg,
            WeaponHash.Hammer,
            WeaponHash.Hatchet,
            WeaponHash.StoneHatchet,
            WeaponHash.StunGun,
            WeaponHash.Musket,
            WeaponHash.HeavySniper,
            WeaponHash.HeavySniperMk2,
            WeaponHash.HomingLauncher,
            WeaponHash.Knife,
            WeaponHash.KnuckleDuster,
            WeaponHash.Machete,
            WeaponHash.Molotov,
            WeaponHash.Nightstick,
            WeaponHash.NightVision,
            WeaponHash.Parachute,
            WeaponHash.PetrolCan,
            WeaponHash.PipeBomb,
            WeaponHash.PoolCue,
            WeaponHash.ProximityMine,
            WeaponHash.Railgun,
            WeaponHash.RayPistol,
            WeaponHash.RPG,
            WeaponHash.SmokeGrenade,
            WeaponHash.SniperRifle,
            WeaponHash.StickyBomb,
            WeaponHash.SwitchBlade,
            WeaponHash.Unarmed,
            WeaponHash.Wrench
        };

        public static IEnumerable<PedHash> exclusions = new List<PedHash>()
        {
            PedHash.Acult01AMM,
            PedHash.Motox01AMY,
            PedHash.Boar,
            PedHash.Cat,
            PedHash.ChickenHawk,
            PedHash.Chimp,
            PedHash.Chop,
            PedHash.Cormorant,
            PedHash.Cow,
            PedHash.Coyote,
            PedHash.Crow,
            PedHash.Deer,
            PedHash.Dolphin,
            PedHash.Fish,
            PedHash.Hen,
            PedHash.Humpback,
            PedHash.Husky,
            PedHash.KillerWhale,
            PedHash.MountainLion,
            PedHash.Pig,
            PedHash.Pigeon,
            PedHash.Poodle,
            PedHash.Rabbit,
            PedHash.Rat,
            PedHash.Retriever,
            PedHash.Rhesus,
            PedHash.Rottweiler,
            PedHash.Seagull,
            PedHash.HammerShark,
            PedHash.TigerShark,
            PedHash.Shepherd,
            PedHash.Stingray,
            PedHash.Westy,
            PedHash.BradCadaverCutscene,
            PedHash.Orleans,
            PedHash.OrleansCutscene,
            PedHash.ChiCold01GMM,
            PedHash.DeadHooker,
            PedHash.Marston01,
            PedHash.Niko01,
            PedHash.PestContGunman,
            PedHash.Pogo01,
            PedHash.Ranger01SFY,
            PedHash.Ranger01SMY,
            PedHash.RsRanger01AMO,
            PedHash.Zombie01
        };
        
    }
}
