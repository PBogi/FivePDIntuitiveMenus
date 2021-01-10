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
    internal class LockerRoom : BaseScript
    {
        internal Menu menu;

        internal void ShowMenu()
        {
            //TODO: Clean code
            PlayerData playerData = Utilities.GetPlayerData();

            try
            {
                menu = new Menu("Locker Room");
                MenuController.AddMenu(menu);
                MenuItem menuButton_ChangeOutfit = new MenuItem("Change outfit");
                MenuListItem menuListItem_PedModels = new MenuListItem("Change outfit", null, 0);
                List<string> menuList_PedModels = new List<string>() { };

                if (GetResourceState("eup-ui") == "started")
                {
                    menuButton_ChangeOutfit = new MenuItem("Change outfit")
                    {
                        Label = "→→→"
                    };
                    menu.AddMenuItem(menuButton_ChangeOutfit);
                }
                else
                {
                    menuList_PedModels = new List<string>() { };

                    foreach (var PedModel in PedModels)
                    {
                        if ((bool)PedModel.Value["isAvailableForEveryone"])
                        {
                            menuList_PedModels.Add(PedModel.Key.ToString());
                        }
                        else if ((bool)PedModel.Value["useRanks"])
                        {
                            string[] availableForRanks = PedModel.Value["availableForRanks"].Values<string>().ToArray();
                            if (availableForRanks.Contains(playerData.Rank))
                            {
                                menuList_PedModels.Add(PedModel.Key.ToString());
                            }
                        }
                        else if (PedModel.Value["availableForDepartments"] != null)
                        {
                            int[] availableForDepartments = PedModel.Value["availableForDepartments"].Values<int>().ToArray();
                            if (availableForDepartments.Contains(playerData.DepartmentID))
                            {
                                menuList_PedModels.Add(PedModel.Key.ToString());
                            }
                        }
                    }

                    menuListItem_PedModels = new MenuListItem("Change outfit", menuList_PedModels, 0);
                    menu.AddMenuItem(menuListItem_PedModels);
                }

                List<string> menuList_Loadouts = new List<string>() { };
                foreach (var Loadout in Common.Loadouts)
                {
                    if((bool)Loadout.Value["isAvailableForEveryone"])
                    {
                        menuList_Loadouts.Add(Loadout.Key.ToString());
                    }
                    else if((bool)Loadout.Value["useRanks"])
                    {
                        string[] availableForRanks = Loadout.Value["availableForRanks"].Values<string>().ToArray();
                        if (availableForRanks.Contains(playerData.Rank))
                        {
                            menuList_Loadouts.Add(Loadout.Key.ToString());
                        }
                    }
                    else if(Loadout.Value["availableForDepartments"] != null)
                    {
                        int[] availableForDepartments = Loadout.Value["availableForDepartments"].Values<int>().ToArray();
                        if (availableForDepartments.Contains(playerData.DepartmentID))
                        {
                            menuList_Loadouts.Add(Loadout.Key.ToString());
                        }
                    }
                }

                MenuListItem menuListItem_Loadout = new MenuListItem("Get Loadout", menuList_Loadouts, 0);
                menu.AddMenuItem(menuListItem_Loadout);

                menu.AddMenuItem(new MenuItem("Return all weapons")
                {
                    Enabled = true,
                });

                menu.AddMenuItem(new MenuItem("Refill health & armor")
                {
                    Enabled = true
                });

                string dutyText;
                if (Utilities.IsPlayerOnDuty()) dutyText = "Go off duty";
                else dutyText = "Go on duty";

                menu.AddMenuItem(new MenuItem(dutyText, "")
                {
                    Enabled = true
                });


                menu.OnItemSelect += (_menu, _item, _index) =>
                {
                    switch(_item.Text)
                    {
                        case "Change outfit":
                            menu.CloseMenu();
                            ExecuteCommand("eup");
                            break;
                        case "Return all weapons":
                            Common.DisplayNotification("All weapons returned to armory");
                            Game.PlayerPed.Weapons.RemoveAll();
                            break;
                        case "Refill health & armor":
                            Common.DisplayNotification("Health & armor refilled");
                            Game.PlayerPed.Health = Game.PlayerPed.MaxHealth;
                            Game.PlayerPed.Armor = Game.Player.MaxArmor;
                            break;
                        case "Go off duty":
                            Utilities.SetPlayerDuty(false);
                            _item.Text = "Go on duty";
                            break;
                        case "Go on duty":
                            Utilities.SetPlayerDuty(true);
                            _item.Text = "Go off duty";
                            break;
                    }
                };


                menu.OnListItemSelect += (_menu, _listItem, _listIndex, _itemIndex) =>
                {

                    if (_listItem == menuListItem_Loadout)
                    {
                        Game.PlayerPed.Weapons.RemoveAll();
                        
                        foreach (var weapon in Common.Loadouts[menuList_Loadouts.ElementAt(_listIndex)]["weapons"])
                        {
                            int ammo = (int)weapon["ammo"];
                            uint weaponHash = (uint)GetHashKey((string)weapon["weapon"]);

                            GiveWeaponToPed(PlayerPedId(), weaponHash, ammo, false, false);

                            if (weapon["components"] != null) {
                                foreach (string weaponComponent in weapon["components"].Values<string>().ToArray()) {
                                    Console.WriteLine(weaponComponent);
                                    GiveWeaponComponentToPed(PlayerPedId(), weaponHash, (uint)GetHashKey(weaponComponent));
                                }
                                
                             }
                           
                        }
                        Common.DisplayNotification("Loadout received");
                    }
                    else if(_listItem == menuListItem_PedModels)
                    {
                        _ = ChangePlayerPed(PedModels[menuList_PedModels.ElementAt(_listIndex)]["model"].ToString());
                    }
                };

                menu.OnMenuClose += (_menu) =>
                {
                    Common.IsMenuOpen = false;
                };

                menu.OnMenuOpen += (_menu) =>
                {
                    Common.IsMenuOpen = true;
                };

                menu.OpenMenu();
            }
            catch(Exception e)
            {

                Debug.WriteLine(e.ToString());
            }
        }

        // Async task to spawn the ped, so the menu thread is not blocked during loading
        // Make sure to wait for the ped to be in the memory. Custom peds may take a while to download, so the timeout (maxretries) is a bit longer
        // But it should eventually time out if ped cannot be loaded at all
        internal async Task ChangePlayerPed(string name)
        {
            uint model = (uint)GetHashKey(name);
            RequestModel(model);

            int maxretries = 0;
            while (!HasModelLoaded(model) && maxretries < 100)
            {
                await BaseScript.Delay(100);
                maxretries++;
            }

            if (HasModelLoaded(model))
            {
                SetPlayerModel(Game.Player.Handle, model);
                SetPedRandomComponentVariation(PlayerPedId(), false);
                SetPedRandomProps(PlayerPedId());                
                Game.PlayerPed.Health = Game.PlayerPed.MaxHealth;
                Game.PlayerPed.Armor = Game.Player.MaxArmor;
            }
            else
            {
                Common.DisplayNotification("Ped could not be loaded in time. Try again!");
            }
        }

        internal List<Vector3> Locations = new List<Vector3>();
        internal JObject PedModels = new JObject();
    }
    


}
