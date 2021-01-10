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

                // Do all the dynamic stuff in "OnMenuOpen" so it runs again when player returns from submenu
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

                    // Check if any object is blocking the spawn area. Currently only 1 raycast across the area. Enough? Probably.
                    rayHandle = CastRayPointToPoint(SpawnLocations[locationIndex].X - 2, SpawnLocations[locationIndex].Y - 2, SpawnLocations[locationIndex].Z, SpawnLocations[locationIndex].X + 2, SpawnLocations[locationIndex].Y + 3, SpawnLocations[locationIndex].Z + 1, -1, 0, 0);
                    GetRaycastResult(rayHandle, ref _Hit, ref _endCoords, ref _surfaceNormal, ref _entityHit);

                    // Check if the vehicle currently in the spawn area has liveries and add them to the menu
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


                    // Check if the entity in the spawn area is a vehicle and add color options to the menu
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


                    // Check if the vehicle in the spawn area has extras and show the menu button
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

                    // Check if the entity in the spawn area is a vehicle and show the delete button
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

                // Select Vehicle Submenu
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

                    // Max extras in GTA V is 14
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
                        SetEntityAsMissionEntity(_entityHit, true, true); // Can only delete vehicle if it is a mission entity!
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
                    // Delete previously spawned vehicle first
                    // TODO: Maybe do this only if the vehicle is still in the spawn area, so one player can spawn multiple vehicles (for other players)
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

        // Async task to spawn the vehicle, so the menu thread is not blocked during loading
        // Make sure to wait for the vehicle to be in the memory. Long timeout (maxretries) here as a addon vehicle download might take a while
        // But it should eventually time out if vehicle cannot be loaded at all
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

        // Small color selection. Those are the most used ones I guess?!
        // Probably no need for a big selection here.
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
    }
}
