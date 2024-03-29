﻿using CitizenFX.Core;
using CitizenFX.Core.Native;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FireScript
{
    internal class Flame
    {
        public int FlameID;
        public Vector3 Position;
        public CoordinateParticleEffect FlamePTFX;
        public bool Active = false;

        public Flame(Vector3 position)
        {
            this.Position = position;
            Start();
        }

        public async Task Start()
        {
            Active = true;
            this.FlameID = API.StartScriptFire(Position.X, Position.Y, Position.Z, 25, false);
            ParticleEffectsAsset asset = new ParticleEffectsAsset("scr_trevor3");
            await asset.Request(1000);
            Vector3 smokepos = Position;
            smokepos.Z += 0.4f;
            FlamePTFX = asset.CreateEffectAtCoord("scr_trev3_trailer_plume", smokepos, scale: 0.7f, startNow: true);
            //FlamePTFX = Function.Call<int>(Hash.START_PARTICLE_FX_LOOPED_AT_COORD, "scr_trev3_trailer_plume", Position.X, Position.Y, Position.Z + 0.4, 0, 0, 0, 0.7, 0, 0, 0, 0);
            //Function.Call(Hash.SET_PARTICLE_FX_LOOPED_ALPHA, FlamePTFX, 1);

        }

        public void Remove()
        {
            Function.Call(Hash.REMOVE_SCRIPT_FIRE, FlameID);

            Function.Call(Hash.STOP_FIRE_IN_RANGE, Position.X, Position.Y, Position.Z, 20);
            FlamePTFX.RemovePTFX();
            Active = false;
            //Debug.WriteLine("Removed flame with ID: " + FlameID);
        }

        public void Manage()
        {

            if (FlamePTFX != null && API.DoesParticleFxLoopedExist(FlamePTFX.Handle))
            {
                int numberInRange = Function.Call<int>(Hash.GET_NUMBER_OF_FIRES_IN_RANGE, Position.X, Position.Y, Position.Z, 1.5f);
                if (numberInRange < 1)
                {
                    this.Remove();
                    //Debug.WriteLine("Removed flame due to smallerthan 1: " + numberInRange);

                }
                //Screen.ShowSubtitle("NumFlames: " + numberInRange);
            }
            else if (FlamePTFX != null && Active)
            {
                this.Remove();
            }
        }
    }
}
