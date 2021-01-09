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

                    // Todo: Modelchanger
                    // void SetPlayerModel(Player player, uint model);
                    //RequestModel((uint)GetHashKey("csb_cop"));
                    //SetPlayerModel(PlayerId(), (uint)GetHashKey("csb_cop"));
                    //Common.DisplayNotification("Currently only EUP supported. If EUP is installed, create a multiplayer character first!");

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


                /*Menu menu_ChangeOutfit = new Menu("Change outfit");
                MenuController.AddSubmenu(menu, menu_ChangeOutfit);
                MenuController.BindMenuItem(menu, menu_ChangeOutfit, menuButton_ChangeOutfit);

                MenuListItem outfitmenuListItem_Department = new MenuListItem("Department", menuList_Loadouts, 0);
                menu_ChangeOutfit.AddMenuItem(outfitmenuListItem_Department);*/

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
        /*internal List<Vector3> Locations = new List<Vector3>
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
        };*/
    }
    


}
