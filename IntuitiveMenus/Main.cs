using CitizenFX.Core;
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
            Common.IsMenuOpen = false;
            // Check if player is allowed to use FivePD at all
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
        }

        internal async Task LoadConfigs()
        {
            // Try to load FivePD config files
            try
            {
                // Loadouts
                JObject _Loadouts = JObject.Parse(LoadResourceFile(GetCurrentResourceName(), "config/loadouts.json"));
                Common.Loadouts = new List<Loadout>() { };
                foreach (var _Loadout in _Loadouts)
                {
                    bool.TryParse((string)_Loadout.Value["isAvailableForEveryone"], out bool _isAvailableForEveryone);
                    bool.TryParse((string)_Loadout.Value["useRanks"], out bool _useRanks);

                    List<string> _availableForRanks = new List<string>();
                    if (_Loadout.Value["availableForRanks"] != null)
                    {
                        _availableForRanks = _Loadout.Value["availableForRanks"].Values<string>().ToList();
                    }

                    List<int> _availableForDepartments = new List<int>();
                    if (_Loadout.Value["availableForDepartments"] != null)
                    {
                        _availableForDepartments = _Loadout.Value["availableForDepartments"].Values<int>().ToList();
                    }

                    List<Weapon> _Weapons = new List<Weapon>() { };
                    foreach(var _Weapon in _Loadout.Value["weapons"])
                    {
                        Int32.TryParse((string)_Weapon["ammo"], out int _Ammo);

                        string[] _Components = new string[] { };
                        if (_Weapon["components"] != null)
                        {
                            _Components = _Weapon["components"].Values<string>().ToArray();
                        }

                        _Weapons.Add(new Weapon
                        {
                            Model = (string)_Weapon["weapon"],
                            Components = _Components,
                            Ammo = _Ammo
                        });
                    }

                    Common.Loadouts.Add(new Loadout
                    {
                        Name = _Loadout.Key,
                        IsAvailableForEveryone = _isAvailableForEveryone,
                        UseRanks = _useRanks,
                        AvailableForRanks = _availableForRanks,
                        AvailableForDepartments = _availableForDepartments,
                        Weapons = _Weapons
                    });

                    if(_availableForDepartments.Count > 0 && _availableForDepartments.ElementAt(0) == 9999)
                    {
                        trunk.Loadouts.Add(new Loadout
                        {
                            Name = _Loadout.Key,
                            IsAvailableForEveryone = _isAvailableForEveryone,
                            UseRanks = _useRanks,
                            AvailableForRanks = _availableForRanks,
                            AvailableForDepartments = _availableForDepartments,
                            Weapons = _Weapons
                        });
                    }
                }

                // Vehicles
                JObject _Vehicles = JObject.Parse(LoadResourceFile(GetCurrentResourceName(), "config/vehicles.json"));

                foreach (var _Vehicle in _Vehicles["police"])
                {
                    bool.TryParse((string)_Vehicle["isAvailableForEveryone"], out bool _isAvailableForEveryone);
                    bool.TryParse((string)_Vehicle["useRanks"], out bool _useRanks);

                    List<string> _availableForRanks = new List<string>();
                    if (_Vehicle["availableForRanks"] != null)
                    {
                        _availableForRanks = _Vehicle["availableForRanks"].Values<string>().ToList();
                    }

                    List<int> _availableForDepartments = new List<int>();
                    if (_Vehicle["availableForDepartments"] != null)
                    {
                        _availableForDepartments = _Vehicle["availableForDepartments"].Values<int>().ToList();
                    }


                    garage.Vehicles.Add(new Vehicle
                    {
                        Name = (string)_Vehicle["name"],
                        Model = (string)_Vehicle["vehicle"],
                        IsAvailableForEveryone = _isAvailableForEveryone,
                        UseRanks = _useRanks,
                        AvailableForRanks = _availableForRanks,
                        AvailableForDepartments = _availableForDepartments,
                    });
                }

                // Coordinates
                JObject _Coordinates = JObject.Parse(LoadResourceFile(GetCurrentResourceName(), "config/coordinates.json"));
                // Check if custom locker locations are available
                if (_Coordinates["lockers"] != null)
                {
                    var lockerLocations = _Coordinates["lockers"].Values<string>().ToArray();

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
                if (_Coordinates["garages"] != null)
                {
                    foreach (var garageLocation in _Coordinates["garages"])
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
            catch (Exception e)
            {
                Debug.WriteLine("Could not read FivePD configs");
                Debug.WriteLine(e.ToString());
            }


            // Add default loadouts
            if(trunk.Loadouts.Count <= 0)
            {
                trunk.Loadouts.Add(new Loadout
                {
                    Name = "Fire Extinguisher",
                    IsAvailableForEveryone = true,
                    UseRanks = false,
                    AvailableForRanks = new List<string>(),
                    AvailableForDepartments = new List<int>(),
                    Weapons = new List<Weapon> { new Weapon
                        {
                            Model = "WEAPON_FIREEXTINGUISHER",
                            Ammo = 1000
                        }
                    }
                });
                trunk.Loadouts.Add(new Loadout
                {
                    Name = "Rifle",
                    IsAvailableForEveryone = true,
                    UseRanks = false,
                    AvailableForRanks = new List<string>(),
                    AvailableForDepartments = new List<int>(),
                    Weapons = new List<Weapon> { new Weapon
                        {
                            Model = "WEAPON_CARBINERIFLE",
                            Ammo = 250
                        }
                    }
                });
                trunk.Loadouts.Add(new Loadout
                {
                    Name = "Shotgun",
                    IsAvailableForEveryone = true,
                    UseRanks = false,
                    AvailableForRanks = new List<string>(),
                    AvailableForDepartments = new List<int>(),
                    Weapons = new List<Weapon> { new Weapon
                        {
                            Model = "WEAPON_PUMPSHOTGUN",
                            Ammo = 250
                        }
                    }
                });
            }


            // Check if custom ped config is available
            try
            {
                JObject _PedModels = JObject.Parse(LoadResourceFile(GetCurrentResourceName(), "config/pedmodels.json"));

                foreach (var _PedModel in _PedModels)
                {
                    bool.TryParse((string)_PedModel.Value["isAvailableForEveryone"], out bool _isAvailableForEveryone);
                    bool.TryParse((string)_PedModel.Value["useRanks"], out bool _useRanks);

                    List<string> _availableForRanks = new List<string>();
                    if(_PedModel.Value["availableForRanks"] != null)
                    {
                        _availableForRanks = _PedModel.Value["availableForRanks"].Values<string>().ToList();
                    }

                    List<int> _availableForDepartments = new List<int>();
                    if (_PedModel.Value["availableForDepartments"] != null)
                    {
                        _availableForDepartments = _PedModel.Value["availableForDepartmentss"].Values<int>().ToList();
                    }

                    locker.PedModels.Add(new PedModel
                    {
                        Name = _PedModel.Key,
                        Model = (string)_PedModel.Value["model"],
                        IsAvailableForEveryone = _isAvailableForEveryone,
                        UseRanks = _useRanks,
                        AvailableForRanks = _availableForRanks,
                        AvailableForDepartments = _availableForDepartments
                    });
                }
            }
            catch
            {
                locker.PedModels.Add(new PedModel
                {
                    Name = "Male Cop",
                    Model = "s_m_y_cop_01",
                    IsAvailableForEveryone = true,
                    UseRanks = false,
                    AvailableForRanks = new List<string>(),
                    AvailableForDepartments = new List<int>()
                });
                locker.PedModels.Add(new PedModel
                {
                    Name = "Female Cop",
                    Model = "s_f_y_cop_01",
                    IsAvailableForEveryone = true,
                    UseRanks = false,
                    AvailableForRanks = new List<string>(),
                    AvailableForDepartments = new List<int>()
                });
                locker.PedModels.Add(new PedModel
                {
                    Name = "Male Sheriff",
                    Model = "s_m_y_sheriff_01",
                    IsAvailableForEveryone = true,
                    UseRanks = false,
                    AvailableForRanks = new List<string>(),
                    AvailableForDepartments = new List<int>()
                });
                locker.PedModels.Add(new PedModel
                {
                    Name = "Female Sheriff",
                    Model = "s_f_y_sheriff_01",
                    IsAvailableForEveryone = true,
                    UseRanks = false,
                    AvailableForRanks = new List<string>(),
                    AvailableForDepartments = new List<int>()
                });
                locker.PedModels.Add(new PedModel
                {
                    Name = "Male Highway Patrol",
                    Model = "s_m_y_hwaycop_01",
                    IsAvailableForEveryone = true,
                    UseRanks = false,
                    AvailableForRanks = new List<string>(),
                    AvailableForDepartments = new List<int>()
                });
                locker.PedModels.Add(new PedModel
                {
                    Name = "Male Park Ranger",
                    Model = "s_m_y_ranger_01",
                    IsAvailableForEveryone = true,
                    UseRanks = false,
                    AvailableForRanks = new List<string>(),
                    AvailableForDepartments = new List<int>()
                });
                locker.PedModels.Add(new PedModel
                {
                    Name = "Female Park Ranger",
                    Model = "s_f_y_ranger_01",
                    IsAvailableForEveryone = true,
                    UseRanks = false,
                    AvailableForRanks = new List<string>(),
                    AvailableForDepartments = new List<int>()
                });
                locker.PedModels.Add(new PedModel
                {
                    Name = "N.O.O.S.E.",
                    Model = "s_m_y_swat_01",
                    IsAvailableForEveryone = true,
                    UseRanks = false,
                    AvailableForRanks = new List<string>(),
                    AvailableForDepartments = new List<int>()
                });
            }           
        }

        internal async Task CreateBlips()
        {
            // Create Blips for garages and lockers
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

        // Show helptext if player is near a locker and react on button press
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

        // Show helptext and markers if player is near a garage and react on button press
        internal async Task IsPlayerNearGarage()
        {
            for(int i=0; i < garage.Locations.Count; i++)
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
                            garage.ShowMenu(i); // Call the method with the index of the garage to find the appropriate spawn location
                        }
                    }
                }
            }
        }

        // React on button press
        internal async Task CheckButtonPressed()
        {
            if (Game.IsControlJustReleased(1, Control.Context))
            {
                _ = trunk.OpenTrunk();
            }
        }
    }
}
