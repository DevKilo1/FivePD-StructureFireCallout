using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using CitizenFX.Core;
using FireScript;
using FivePD.API;
using FivePD.API.Utils;
using Newtonsoft.Json.Linq;

namespace FivePD_StructureFireCallout;
[Guid("EEF16B95-E19B-4B15-997E-156435FF449C")]
[CalloutProperties("Structure Fires", "DevKilo","1.0.3")]
public class FireCall : Callout
{
    private JObject config = script.config;
    private JObject selectedLoc;
    private Blip alarmBoxBlip;
    private bool alarmActive = false;
    private string integration = "None";
    public static ExportDictionary exports;
    private void Startup()
    {
        try
        {
            InitInfo(GetLocation());
            ShortName = (string)config["ColoredShortName"];
            CalloutDescription = (string)config["CalloutDescription"];
            ResponseCode = (int)config["ResponseCode"];
            StartDistance = (float)config["StartDistance"];
            integration = (string)config["Integration"];
        }
        catch (Exception err)
        {
            Debug.WriteLine(err.ToString());
        }
        
    }
    public FireCall()
    {
        Startup();
    }

    public override async Task OnAccept()
    {
        AcceptHandler();
    }

    private void AcceptHandler()
    {
        ShortName = (string)config["ShortName"];
        InitBlip(75f, BlipColor.Red);
        if (integration == "None")
            BaseScript.TriggerEvent("KiloFires:StartFireAtPos", Location.X, Location.Y, Location.Z, (int)selectedLoc["MaxFlames"], (int)selectedLoc["FireRadius"], false);
        else if (integration == "SmartFires")
        {
            #pragma error disable CS0656
            exports["SmartFires"]["CreateFire"](Location, (float)selectedLoc["FireRadius"], "normal");
        }
        alarmActive = true;
    }

    public override async void OnStart(Ped closest)
    {
        // Do alarm
        if ((bool)selectedLoc["enableAlarm"])
        {
            FireAlarm(Location);
            Vector3 ogLoc = Location;
            Location = Game.PlayerPed.Position;
            ShowNetworkedNotification("Press ~y~E~s~ at the ~f~marked alarm box~s~ to stop the alarm!", "CHAR_CALL911",
                "CHAR_CALL911", "Inner-thought", "Hint", 10f);
            Vector3 alarmBoxLocation = Utils.JSONCoordsToVector3((JObject)selectedLoc["alarmBoxCoords"]);
            alarmBoxBlip = World.CreateBlip(alarmBoxLocation);
            alarmBoxBlip.Color = BlipColor.MichaelBlue;
            alarmBoxBlip.Name = "Alarm Box";
            Location = ogLoc;
            await WaitUntilPlayerPressesKeyAtLocation(alarmBoxLocation);
            alarmActive = false;
        }
    }

    public override void OnCancelBefore()
    {
        BaseScript.TriggerEvent("KiloFires:StopFireAtPos",Location.X,Location.Y,Location.Z,5f);
        if (alarmActive)
            alarmActive = false;
        if (alarmBoxBlip != null)
        {
            alarmBoxBlip.Delete();
            alarmBoxBlip = null;
        }
            
        base.OnCancelBefore();
    }

    private async Task WaitUntilPlayerPressesKeyAtLocation(Vector3 pos)
    {
        Tick += async () =>
        {
            if (!alarmActive) return;
            if (Game.PlayerPed.Position.DistanceTo(pos) < 2f)
            {
                if (Game.IsControlJustPressed(0, Control.Pickup))
                    alarmActive = false;
            }
        };
        while (true)
        {
            if (!alarmActive)
                return;
            await BaseScript.Delay(100);
        }
    }

    private async Task FireAlarm(Vector3 pos)
    {
        while (alarmActive)
        {
            BaseScript.TriggerServerEvent("Server:SoundToRadius", Game.PlayerPed.NetworkId, 20f, "firealarm", 1f);
            await BaseScript.Delay(250);
        }
    }

    private Vector3 GetLocation()
    {
        Vector3 result = Vector3.Zero;
        try
        {
            JArray locationsArray = (JArray)config["Locations"];
            int chance = new Random().Next(locationsArray.Count);
            JObject location = (JObject)locationsArray[chance];
            selectedLoc = location;

            Vector3 coordinates = Utils.JSONCoordsToVector3((JObject)location["coords"]);
            result = coordinates;
        }
        catch (Exception err)
        {
            Utils.CalloutError(err, this);
        }

        return result;
    }
}

public class script : BaseScript
{
    public static JObject config;
    private List<Fire> ActiveFires = new List<Fire>();
    private const int ManageFireTimeout = 50;
    private List<Tuple<CoordinateParticleEffect, Vector3>> SmokeWithoutFire = new List<Tuple<CoordinateParticleEffect, Vector3>>();
    public script()
    {
        FireCall.exports = Exports;
        config = Utils.GetConfig();
        EventHandlers["KiloFires:StartFireAtPos"] += startFire;
        EventHandlers["KiloFires:StopFireAtPos"] += stopFireAtPos;
        Main();
    }

    private void stopFireAtPos(float x, float y, float z, float radius)
    {
        Vector3 pos = new Vector3(x, y, z);
        foreach (Fire f in ActiveFires.ToArray())
        {
            if (f.Active)
                if (f.Position.DistanceTo(pos) < radius)
                {
                    f.Remove(false);
                    ActiveFires.Remove(f);
                }
        }
    }
    private void startFire(float x, float y, float z, int maxFlames, int maxRange, bool explosion)
    {
        Vector3 Pos = new Vector3(x, y, z);
        Pos.Z -= 0.87f;
        if (maxRange > 30) { maxRange = 30; }
        if (maxFlames > 100) { maxRange = 100; }
        Fire f = new Fire(Pos, maxFlames, false, maxRange, explosion);
        ActiveFires.Add(f);
        f.Start();
    }
    private async void Main()
    {
        DateTime timekeeper = DateTime.Now;
        while (true)
        {
            await Delay(10);
            if ((System.DateTime.Now - timekeeper).TotalMilliseconds > ManageFireTimeout)
            {
                timekeeper = DateTime.Now;
                foreach (Fire f in ActiveFires.ToArray())
                {

                    if (f.Active)
                    {
                        f.Manage();
                    }
                    else
                    {
                        ActiveFires.Remove(f);
                    }
                }
            }
        }
    }
    
    private void stopFires(bool onlyNearbyFires, Vector3 pos, float distance = 35)
    {
        foreach (Fire f in ActiveFires.ToArray())
        {
            if (!onlyNearbyFires || Vector3.Distance(f.Position, pos) < distance)
            {
                f.Remove(false);
                ActiveFires.Remove(f);
            }
        }
    }
    private async Task startSmoke(Vector3 pos, float scale)
    {
        ParticleEffectsAsset asset = new ParticleEffectsAsset("scr_agencyheistb");
        await asset.Request(1000);
        SmokeWithoutFire.Add(Tuple.Create(asset.CreateEffectAtCoord("scr_env_agency3b_smoke", pos, scale: scale, startNow: true), pos));
    }

    private void stopSmoke(bool allSmoke, Vector3 position)
    {
        foreach (Tuple<CoordinateParticleEffect, Vector3> f in SmokeWithoutFire.ToArray())
        {
            if (!allSmoke || Vector3.Distance(f.Item2, position) < 30f)
            {
                f.Item1.RemovePTFX();
                SmokeWithoutFire.Remove(f);
            }
        }
    }
    
}
