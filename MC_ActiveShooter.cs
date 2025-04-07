using System;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using FivePD.API;
using FivePD.API.Utils;

namespace MC_MixedCallouts
{
    [CalloutProperties("[MC] Active Shooter", "87", "1.0.0")]
    public class MC_ActiveShooter : Callout
    {
        public Ped suspect1;
        public bool isSuspectAiming = false;

        public MC_ActiveShooter()
        {
            Random random = new Random();
            float offsetX = random.Next(100, 400);
            float offsetY = random.Next(100, 400);
            InitInfo(World.GetNextPositionOnSidewalk(Game.PlayerPed.GetOffsetPosition(new Vector3(offsetX, offsetY, 0))));
            ShortName = "[MC] Active Shooter";
            CalloutDescription = "Multiple callers report active shooter situation.";
            ResponseCode = 99;
            StartDistance = 200f;
        }
        
        public async override void OnStart(Ped player)
        {
            base.OnStart(player);
            suspect1.MarkAsNoLongerNeeded();
        }
        
        public async void InitActiveShooter(Ped suspect)
        {
           // Set up relationship groups
            World.AddRelationshipGroup("SHOOTERS");
            Function.Call(Hash.SET_PED_RELATIONSHIP_GROUP_HASH, suspect.Handle, Game.GenerateHash("SHOOTERS"));
            
            API.SetRelationshipBetweenGroups(5, (uint)API.GetHashKey("SHOOTERS"),(uint)API.GetHashKey("PLAYER"));
            API.SetRelationshipBetweenGroups(5, (uint)API.GetHashKey("SHOOTERS"),(uint)API.GetHashKey("COP"));
            API.SetRelationshipBetweenGroups(5, (uint)API.GetHashKey("SHOOTERS"),(uint)API.GetHashKey("CIVMALE"));
            API.SetRelationshipBetweenGroups(5, (uint)API.GetHashKey("SHOOTERS"),(uint)API.GetHashKey("CIVFEMALE"));
            // Ensure other groups will hate the shooter
            API.SetRelationshipBetweenGroups(5, (uint)API.GetHashKey("PLAYER"),(uint)API.GetHashKey("SHOOTERS"));
            API.SetRelationshipBetweenGroups(5, (uint)API.GetHashKey("COP"),(uint)API.GetHashKey("SHOOTERS"));
            API.SetRelationshipBetweenGroups(5, (uint)API.GetHashKey("CIVMALE"),(uint)API.GetHashKey("SHOOTERS"));
            API.SetRelationshipBetweenGroups(5, (uint)API.GetHashKey("CIVFEMALE"),(uint)API.GetHashKey("SHOOTERS"));
                
            // Give the suspect a weapon
           WeaponHash weapon = WeaponHash.AssaultRifle; // or choose randomly
           
           suspect.Weapons.Give(weapon, 999, true, true); // Give them the weapon with ammo
           
           // Set the suspect to be aggressive
           Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, suspect.Handle, 0, true);  // take cover
           Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, suspect.Handle, 5, true); // can fight armed peds while not armed
           Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, suspect.Handle, 46, true); // always fight
           Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, suspect.Handle, 24, true); // prox fire mode
           Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, suspect.Handle, 30, true); // shoot without LOS
           
           // None of these make a difference - Allow shooting at friendlies
           API.SetCanAttackFriendly(suspect1.Handle, true, false);
           API.SetPedConfigFlag(suspect1.Handle, 140, true);
           //API.SetPedConfigFlag(suspect1.Handle, 186, false);
           
           suspect.Task.WanderAround();
           suspect.Task.FightAgainstHatedTargets(100f);

           suspect.MaxHealth = 250;
           suspect.Health = 250;
           suspect.Armor = 50;
           
           // Optional: Make the suspect keep moving and shooting
           Function.Call(Hash.SET_PED_FLEE_ATTRIBUTES, suspect.Handle, 512, false);  // Prevents suspect from fleeing the area
    
           // Example of a loop that keeps the suspect engaged in combat
           while (suspect.Exists() && suspect.IsAlive)
           {
               if (suspect.IsIdle)
               {
                   suspect.Task.FightAgainstHatedTargets(100f);
               }
               await BaseScript.Delay(1000); // Check every second
           }

        }

        public async override Task OnAccept()
        {
            InitBlip();
            UpdateData();
            
            suspect1 = await SpawnPed(RandomUtils.GetRandomPed(),World.GetNextPositionOnSidewalk(Location));
            suspect1.AlwaysKeepTask = true;
            suspect1.BlockPermanentEvents = true;
            InitActiveShooter(suspect1);
        }
        
        public override void OnCancelBefore()
        {
            if (suspect1 != null)
            {
                suspect1.MarkAsNoLongerNeeded();
            }
        }
    }
}