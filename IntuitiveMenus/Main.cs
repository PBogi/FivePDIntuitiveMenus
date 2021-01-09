﻿using CitizenFX.Core;
using System;
using System.Threading.Tasks;
using static CitizenFX.Core.Native.API;
using Newtonsoft.Json.Linq;
using FivePD.API.Utils;
using System.Collections.Generic;
using System.Linq;

#pragma warning disable 1998
namespace IntuitiveMenus
{
    public class IntuitiveMenus : FivePD.API.Plugin
    {
        LockerRoom locker = new LockerRoom();
        Garage garage = new Garage();
        Trunk trunk = new Trunk();

        internal IntuitiveMenus()
        {
            _ = Initialize();
         }

        internal async Task Initialize()
        {
            // Todo: Check regularily
            TriggerServerEvent("FivePD::Allowlist::IsPlayerAllowed", new Action<bool>(allowed =>
            {
                if (allowed)
                {
                    _ = LoadConfigs();

                    Tick += IsPlayerNearLocker;
                    Tick += IsPlayerNearGarage;
                    Tick += CheckButtonPressed;

                    _ = CreateBlips();
                }
            }));

            /*while (!Common.IsAllowed)
            {
                // Wait until allowed
                await BaseScript.Delay(1000);
            }*/
        }

        internal async Task LoadConfigs()
        {
            try
            {
                Common.Loadouts = JObject.Parse(LoadResourceFile(GetCurrentResourceName(), "config/loadouts.json"));
                Common.SceneManagement = JObject.Parse(LoadResourceFile(GetCurrentResourceName(), "config/scene_management.json"));
                Common.Vehicles = JObject.Parse(LoadResourceFile(GetCurrentResourceName(), "config/vehicles.json"));
                Common.Coordinates = JObject.Parse(LoadResourceFile(GetCurrentResourceName(), "config/coordinates.json"));
            }
            catch (Exception e)
            {
                Debug.WriteLine("Could not read FivePD configs");
                Debug.WriteLine(e.ToString());
            }

            // Check if trunk loadouts are configured (Department 9999)
            try
            {
                foreach (var Loadout in Common.Loadouts)
                {
                    if (Loadout.Value["availableForDepartments"] != null && Loadout.Value["availableForDepartments"].Values<int>().ToArray()[0] == 9999)
                    {
                        trunk.Loadouts.Add(new JProperty(Loadout.Key, Loadout.Value));
                    }
                }
            }
            catch
            {
                Debug.WriteLine("Loading default loadouts for trunk");
            }

            if(!trunk.Loadouts.HasValues)
            {
                trunk.Loadouts.Add(new JProperty("Fire Extinguisher", JObject.Parse(@"{
                    'isAvailableForEveryone': false,
                    'useRanks': false,
                    'availableForDepartments': [9999],
                    'weapons': [
                        {
                            'weapon': 'WEAPON_FIREEXTINGUISHER',
                            'ammo': 9999
                        }
                    ]
                }")));

                trunk.Loadouts.Add(new JProperty("Rifle", JObject.Parse(@"{
                     'isAvailableForEveryone': false,
                     'useRanks': false,
                     'availableForDepartments': [9999],
                     'weapons': [
                         {
                             'weapon': 'WEAPON_CARBINERIFLE',
                             'ammo': 250
                         }
                     ]
                }")));

                trunk.Loadouts.Add(new JProperty("Shotgun", JObject.Parse(@"{
                     'isAvailableForEveryone': true,
                     'useRanks': false,
                     'availableForDepartments': [9999],
                     'weapons': [
                         {
                             'weapon': 'WEAPON_PUMPSHOTGUN',
                             'ammo': 250
                         }
                     ]
                }")));
            }


            // Check if custom ped config is available
            try
            {
                locker.PedModels = JObject.Parse(LoadResourceFile(GetCurrentResourceName(), "config/pedmodels.json"));
            }
            catch
            {
                string defaultModels = @"
{
	'Male Cop': {
		'model': 's_m_y_cop_01',
		'isAvailableForEveryone': true
	},
	'Female Cop': {
		'model': 's_f_y_cop_01',
		'isAvailableForEveryone': true
	},
	'Male Sheriff': {
		'model': 's_m_y_sheriff_01',
		'isAvailableForEveryone': true
	},
	'Female Sheriff': {
		'model': 's_f_y_sheriff_01',
		'isAvailableForEveryone': true
	},
	'Male Highway Cop': {
		'model': 's_m_y_hwaycop_01',
		'isAvailableForEveryone': true
	},
	'Male Park Ranger': {
		'model': 's_m_y_ranger_01',
		'isAvailableForEveryone': true
	},
	'Female Park Ranger': {
		'model': 's_f_y_ranger_01',
		'isAvailableForEveryone': true
	},
	'N.O.O.S.E.': {
		'model': 's_m_y_swat_01',
		'isAvailableForEveryone': true
	}	
}
";
                locker.PedModels = JObject.Parse(defaultModels);
            }

            // Check if custom locker locations are available
            if (Common.Coordinates["lockers"] != null)
            {
                var lockerLocations = Common.Coordinates["lockers"].Values<string>().ToArray();

                foreach (var lockerLocation in lockerLocations)
                {
                    string[] location = lockerLocation.Split(',');
                    float.TryParse(location[0], out float locationX);
                    float.TryParse(location[1], out float locationY);
                    float.TryParse(location[2], out float locationZ);

                    locker.Locations.Add(new Vector3(locationX, locationY, locationZ));
                }
            }
            else
            {
                locker.Locations = new List<Vector3>
                {
                    new Vector3(-1092.86f,-809.93f,19.28f), // Vespucci
                    new Vector3(-561.87f,-131.06f,38.43f), // Rockford Hills
                    new Vector3(449.93f,-993.16f,30.69f), // Mission Row
                    new Vector3(361.08f,-1584.79f,29.29f), // Davis Sheriff
                    new Vector3(619.72f,17f,87.82f), // Vinewood
                    new Vector3(827.45f,-1290.13f,28.24f), // La Mesa
                    new Vector3(1848.47f,3690.32f,34.27f), // SandyShores
                    new Vector3(-448.39f,6007.92f,31.72f), // Paleto Bay
                    new Vector3(387.07f,792.57f,187.69f) // Beaver Bush
                };
            }


            // Check if custom garage locations are available
            if (Common.Coordinates["garages"] != null)
            {
                foreach (var garageLocation in Common.Coordinates["garages"])
                {
                    string[] spawnLocation = garageLocation["spawn"].ToString().Split(',');
                    string[] interactionLocation = garageLocation["interaction"].ToString().Split(',');

                    float.TryParse(spawnLocation[0], out float spawnLocationX);
                    float.TryParse(spawnLocation[1], out float spawnLocationY);
                    float.TryParse(spawnLocation[2], out float spawnLocationZ);
                    float.TryParse(spawnLocation[3], out float spawnLocationH);
                    garage.SpawnLocations.Add(new Vector4(spawnLocationX, spawnLocationY, spawnLocationZ, spawnLocationH));

                    float.TryParse(interactionLocation[0], out float interactionLocationX);
                    float.TryParse(interactionLocation[1], out float interactionLocationY);
                    float.TryParse(interactionLocation[2], out float interactionLocationZ);
                    garage.Locations.Add(new Vector3(interactionLocationX, interactionLocationY, interactionLocationZ));
                }
            }
            else
            {
                garage.Locations = new List<Vector3>
                {
                    new Vector3(-1112.62f,-848.44f,12.46f), // Vespucci
                    new Vector3(-582.19f,-149.31f,37.24f), // Rockford Hills
                    new Vector3(409.46f,-976.54f,28.44f), // Mission Row
                    new Vector3(388f,-1607.84f,28.3f), // Davis Sheriff
                    new Vector3(631.64f,23.31f,86.37f), // Vinewood
                    new Vector3(836.98f,-1258.4f,25.38f), // La Mesa
                    new Vector3(1859.04f,3681.91f,32.84f), // SandyShores
                    new Vector3(-484.9f,6022.32f,30.35f), // Paleto Bay
                    new Vector3(376.35f,793.06f,186.5f) // Beaver Bush
                };

                garage.SpawnLocations = new List<Vector4>
                {
                    new Vector4(-1122.37f, -844.42f, 12.41f, 133.68f), // Vespucci
                    new Vector4(-583.03f,-156.17f,36.93f,112.8f), // Rockford Hills
                    new Vector4(407.83f,-979.45f,28.27f,50.68f), // Mission Row
                    new Vector4(391.16f,-1610.71f,28.29f,230.55f), // Davis Sheriff
                    new Vector4(627.48f,24.91f,86.58f,196.58f), // Vinewood
                    new Vector4(833.35f,-1258.09f,25.34f,180.19f), // La Mesa
                    new Vector4(1862.99f,3679.03f,32.65f,210.55f), // SandyShores
                    new Vector4(-482.53f,6024.86f,30.34f,223.38f), // Paleto Bay
                    new Vector4(374.02f,796.69f,186.29f,178.88f) // Beaver Bush
                };
            }
        }

        internal async Task CreateBlips()
        {
            // Create Blips
            //foreach (Vector3 garageLocation in garage.Locations)
            foreach (var garageLocation in garage.Locations)
            {
                int garageBlip = AddBlipForCoord(garageLocation.X, garageLocation.Y, garageLocation.Z);
                SetBlipAsShortRange(garageBlip, true);
                SetBlipSprite(garageBlip, 357);
                SetBlipHiddenOnLegend(garageBlip, true);
            }

            foreach (Vector3 lockerLocation in locker.Locations)
            {
                int lockerBlip = AddBlipForCoord(lockerLocation.X, lockerLocation.Y, lockerLocation.Z);
                SetBlipAsShortRange(lockerBlip, true);
                SetBlipSprite(lockerBlip, 175);
                SetBlipHiddenOnLegend(lockerBlip, true);
            }
        }


        internal async Task IsPlayerNearLocker()
        {
            foreach (Vector3 lockerLocation in locker.Locations)
            {
                float currentDistanceToLocker = Game.PlayerPed.Position.DistanceTo(lockerLocation);
                
                if (!Common.IsMenuOpen && currentDistanceToLocker < 1)
                {
                    BeginTextCommandDisplayHelp("STRING");
                    AddTextComponentSubstringKeyboardDisplay("Press ~INPUT_CONTEXT~ to access the locker room");
                    EndTextCommandDisplayHelp(0, false, false, -1);

                    if (Game.IsControlJustReleased(1, Control.Context))
                    {
                        locker.ShowMenu();
                    }                  
                }
            }
        }
        internal async Task IsPlayerNearGarage()
        {
            for(int i=0; i < garage.Locations.Count; i++)
            //foreach (Vector3 garageLocation in garage.Locations)
            {
                float currentDistanceToGarage = Game.PlayerPed.Position.DistanceTo(garage.Locations[i]);

                if (!Common.IsMenuOpen && currentDistanceToGarage < 25)
                {
                    Common.DrawMarker(23, garage.Locations[i], 2.0f);
                    Common.DrawMarker(1, new Vector3(garage.SpawnLocations[i].X, garage.SpawnLocations[i].Y, garage.SpawnLocations[i].Z), 4.0f);

                    if (currentDistanceToGarage < 2)
                    {
                        BeginTextCommandDisplayHelp("STRING");
                        AddTextComponentSubstringKeyboardDisplay("Press ~INPUT_CONTEXT~ to access garage");
                        EndTextCommandDisplayHelp(0, false, false, -1);

                        if (Game.IsControlJustReleased(1, Control.Context))
                        {
                            garage.ShowMenu(i);
                        }
                    }
                }
            }
        }


        internal async Task CheckButtonPressed()
        {
            if (Game.IsControlJustReleased(1, Control.Context))
            {
                _ = trunk.OpenTrunk();
            }
        }
    }
}