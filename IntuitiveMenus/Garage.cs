using CitizenFX.Core;
using FivePD.API;
using System.Collections.Generic;
using System.Threading.Tasks;
using static CitizenFX.Core.Native.API;
using MenuAPI;
using System;
using Newtonsoft.Json.Linq;
using System.Linq;
using FivePD.API.Utils;

namespace IntuitiveMenus
{
    class Garage
    {
        internal Menu menu;
        internal int SpawnedVehicle;

        internal void ShowMenu(int locationIndex)
        {
            PlayerData playerData = Utilities.GetPlayerData();

            int rayHandle;
            bool _Hit = false;
            Vector3 _endCoords = new Vector3(); ;
            Vector3 _surfaceNormal = new Vector3(); ;
            int _entityHit = 0;

            
            MenuItem menuButton_SelectVehicle = new MenuItem("Select Car");
            MenuListItem menuListItem_Liveries = new MenuListItem("Livery", null, 0);
            MenuListItem menuListItem_Colors = new MenuListItem("Color", null, 0);
            MenuItem menuButton_Extras = new MenuItem("Extras");
            MenuItem menuItem_DeleteVehicle = new MenuItem("Delete vehicle");

            try
            {
                menu = new Menu("Garage");
                MenuController.AddMenu(menu);

                Menu menu_SelectVehicle = new Menu("Select Vehicle");
                Menu menu_Extras = new Menu("Select Extras");


                menu.OnMenuOpen += (_menu) =>
                {
                    Common.IsMenuOpen = true;

                    menu.ClearMenuItems();

                    menuButton_SelectVehicle = new MenuItem("Select Car")
                    {
                        Label = "→→→"
                    };
                    menu.AddMenuItem(menuButton_SelectVehicle);
                    MenuController.AddSubmenu(menu, menu_SelectVehicle);
                    MenuController.BindMenuItem(menu, menu_SelectVehicle, menuButton_SelectVehicle);


                    rayHandle = CastRayPointToPoint(SpawnLocations[locationIndex].X - 2, SpawnLocations[locationIndex].Y - 2, SpawnLocations[locationIndex].Z, SpawnLocations[locationIndex].X + 2, SpawnLocations[locationIndex].Y + 3, SpawnLocations[locationIndex].Z + 1, -1, 0, 0);
                    GetRaycastResult(rayHandle, ref _Hit, ref _endCoords, ref _surfaceNormal, ref _entityHit);

                    List<string> menuList_Liveries = new List<string>() { };
                    menuListItem_Liveries = new MenuListItem("Livery", menuList_Liveries, 0);

                    int liveryCount = GetVehicleLiveryCount(_entityHit);

                    if (_Hit && liveryCount > 1)
                    {
                        for (int i = 0; i < liveryCount; i++)
                        {
                            menuList_Liveries.Add((i+1) + "/" + liveryCount);
                        }
                    }
                    else
                    {
                        menuListItem_Liveries.Enabled = false;
                        menuListItem_Liveries.Description = "No additional liveries found";
                    }
                    menu.AddMenuItem(menuListItem_Liveries);


                    List<string> menuList_Colors = new List<string>() { };
                    menuListItem_Colors = new MenuListItem("Color", menuList_Colors, 0);
                    if (!_Hit || GetEntityType(_entityHit) != 2)
                    {
                        menuListItem_Colors.Enabled = false;
                        menuListItem_Colors.Description = "No vehicle found";
                    }
                    else
                    {
                        int defaultPrimary = 0;
                        int defaultSecondary = 0;
                        GetVehicleColours(_entityHit, ref defaultPrimary, ref defaultSecondary);
                        Colors["Default"] = defaultPrimary;
                        foreach (var entry in Colors)
                        {
                            menuList_Colors.Add(entry.Key);
                        }
                        menuListItem_Colors.Enabled = true;
                        menuListItem_Colors.Description = "Not all liveries support colors";
                    }
                    menu.AddMenuItem(menuListItem_Colors);

                    menuButton_Extras = new MenuItem("Extras")
                    {
                        Label = "→→→"
                    };
                    if (_Hit && DoesExtraExist(_entityHit, 1))
                    {
                        menuButton_Extras.Enabled = true;
                        menuButton_Extras.Description = null;
                        MenuController.AddSubmenu(menu, menu_Extras);
                        MenuController.BindMenuItem(menu, menu_Extras, menuButton_Extras);
                    }
                    else
                    {
                        menuButton_Extras.Enabled = false;
                        menuButton_Extras.Description = "No extras found";
                    }
                    menu.AddMenuItem(menuButton_Extras);


                    menuItem_DeleteVehicle = new MenuItem("Delete vehicle");
                    menu.AddMenuItem(menuItem_DeleteVehicle);

                    if (!_Hit || GetEntityType(_entityHit) != 2)
                    {
                        menuItem_DeleteVehicle.Enabled = false;
                        menuItem_DeleteVehicle.Description = "No vehicle to delete";
                    }
                    else
                    {
                        menuItem_DeleteVehicle.Enabled = true;
                        menuItem_DeleteVehicle.Description = null;
                    }
                };

                menu_SelectVehicle.OnMenuOpen += (_menu) =>
                {
                    Common.IsMenuOpen = true;

                    menu_SelectVehicle.ClearMenuItems();

                    foreach (var Vehicle in Common.Vehicles["police"])
                    {
                        if ((bool)Vehicle["isAvailableForEveryone"])
                        {
                            menu_SelectVehicle.AddMenuItem(new MenuItem(Vehicle["name"].ToString())
                            {
                                Enabled = true
                            });
                        }
                        else if ((bool)Vehicle["useRanks"])
                        {
                            string[] availableForRanks = Vehicle["availableForRanks"].Values<string>().ToArray();
                            if (availableForRanks.Contains(playerData.Rank))
                            {
                                menu_SelectVehicle.AddMenuItem(new MenuItem(Vehicle["name"].ToString())
                                {
                                    Enabled = true
                                });
                            }
                        }
                        else if (Vehicle["availableForDepartments"] != null)
                        {
                            int[] availableForDepartments = Vehicle["availableForDepartments"].Values<int>().ToArray();
                            if (availableForDepartments.Contains(playerData.DepartmentID))
                            {
                                menu_SelectVehicle.AddMenuItem(new MenuItem(Vehicle["name"].ToString())
                                {
                                    Enabled = true
                                });
                            }
                        }
                    }
                };

                menu_Extras.OnMenuOpen += (_menu) =>
                {
                    Common.IsMenuOpen = true;

                    menu_Extras.ClearMenuItems();

                    for(int i=1;i<=14;i++)
                    {
                        if (DoesExtraExist(_entityHit, i))
                        {
                            
                            menu_Extras.AddMenuItem(new MenuCheckboxItem("Extra " + i, IsVehicleExtraTurnedOn(_entityHit, i))
                            {
                                Style = MenuCheckboxItem.CheckboxStyle.Tick
                            });
                        }
                    }
                };


                menu.OnItemSelect += (_menu, _item, _index) =>
                {
                    if(_item == menuItem_DeleteVehicle)
                    {
                        SetEntityAsMissionEntity(_entityHit, true, true);
                        DeleteVehicle(ref _entityHit);
                        _item.Enabled = false;
                        _item.Description = "Vehicle deleted";
                        menuButton_Extras.Enabled = false;
                        menuButton_Extras.Description = "No extras found";
                        menuListItem_Liveries.Enabled = false;
                        menuListItem_Liveries.Description = "No additional liveries found";
                        menuListItem_Colors.Enabled = false;
                        menuListItem_Colors.Description = "No vehicle found";
                    }
                };

                menu_SelectVehicle.OnItemSelect += (_menu, _item, _index) =>
                {
                    if (SpawnedVehicle > 0) DeleteVehicle(ref SpawnedVehicle);

                    // Check again if something is blocking
                    rayHandle = CastRayPointToPoint(SpawnLocations[locationIndex].X - 2, SpawnLocations[locationIndex].Y - 2, SpawnLocations[locationIndex].Z, SpawnLocations[locationIndex].X + 2, SpawnLocations[locationIndex].Y + 2, SpawnLocations[locationIndex].Z + 1, -1, 0, 0);
                    GetRaycastResult(rayHandle, ref _Hit, ref _endCoords, ref _surfaceNormal, ref _entityHit);
                    
                    if (!_Hit)
                    {
                        JObject vehicle = Common.Vehicles["police"].Values<JObject>()
                            .Where(m => m["name"].Value<string>() == _item.Text)
                            .FirstOrDefault();
                        _ = SpawnVehicle(locationIndex, vehicle["vehicle"].ToString());
                    }
                    else
                    {
                        Common.DisplayNotification("Something is blocking the spawn area");
                    }
                };

                menu.OnListIndexChange += (_menu, _listItem, _oldIndex, _newIndex, _itemIndex) =>
                {
                    if(_listItem == menuListItem_Liveries)
                    {
                        SetVehicleLivery(_entityHit, _newIndex);
                    }
                    else if(_listItem == menuListItem_Colors)
                    {
                        int newColor = Colors.ElementAt(_newIndex).Value;
                        if (newColor != 999)
                        {
                            SetVehicleColours(_entityHit, newColor, newColor);
                        }
                        else
                        {
                            ClearVehicleCustomPrimaryColour(_entityHit);
                        }
                    }
                    
                };

                menu_Extras.OnCheckboxChange += (_menu, _item, _index, _checked) =>
                {
                    SetVehicleExtra(_entityHit, _index + 1, !_checked);
                 };


                menu.OnMenuClose += (_menu) =>
                {
                    Common.IsMenuOpen = false;
                    SpawnedVehicle = 0;
                };

                menu_SelectVehicle.OnMenuClose += (_menu) =>
                {
                    Common.IsMenuOpen = false;
                };

                menu.OpenMenu();
            }
            catch (Exception e)
            {

                Debug.WriteLine(e.ToString());
            }
        }

        internal async Task SpawnVehicle(int locationIndex, string name)
        {
            uint vehicle = (uint)GetHashKey(name);
            RequestModel(vehicle);

            int maxretries = 0;
            while(!HasModelLoaded(vehicle) && maxretries < 300)
            {
                await BaseScript.Delay(100);
                maxretries++;
            }

            if (HasModelLoaded(vehicle))
            {
                SpawnedVehicle = CreateVehicle(vehicle, SpawnLocations[locationIndex].X, SpawnLocations[locationIndex].Y, SpawnLocations[locationIndex].Z, SpawnLocations[locationIndex].W, true, false);
                SetVehicleOnGroundProperly(SpawnedVehicle);
                SetModelAsNoLongerNeeded((uint)SpawnedVehicle);
            }
            else
            {
                Common.DisplayNotification("Vehicle could not be loaded in time. Try again!");
            }
        }


        internal Dictionary<string, int> Colors = new Dictionary<string, int>
        {
            { "Default", 0 },
            { "Black", 0 },
            { "Silver", 4 },
            { "Red", 27 },
            { "Blue", 64 },
            { "Dark Blue", 62 },
            { "White", 111 }
        };

        internal List<Vector3> Locations = new List<Vector3>();
        internal List<Vector4> SpawnLocations = new List<Vector4>();
        /*internal List<Vector3> Locations = new List<Vector3>
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

        internal List<Vector4> SpawnLocations = new List<Vector4>
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
        };*/
    }
}
