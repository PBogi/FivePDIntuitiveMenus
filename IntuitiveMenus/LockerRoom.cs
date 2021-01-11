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
            PlayerData playerData = Utilities.GetPlayerData();
            bool _isPlayerOnDuty = Utilities.IsPlayerOnDuty();

            try
            {
                menu = new Menu("Locker Room");
                MenuController.AddMenu(menu);


                MenuItem menuItem_ChangeOutfit = new MenuItem("Change outfit");
                MenuListItem menuListItem_PedModels = new MenuListItem("Change outfit", new List<string>(), 0);

                if (GetResourceState("eup-ui") == "started")
                {
                    menuItem_ChangeOutfit = new MenuItem("Change outfit")
                    {
                        Label = "→→→"
                    };
                    menu.AddMenuItem(menuItem_ChangeOutfit);
                }
                else
                {
                    menuListItem_PedModels.ItemData = new List<string>();
                    
                    foreach (PedModel _PedModel in PedModels)
                    {
                        bool _isAllowed = false;

                        if (_PedModel.IsAvailableForEveryone) _isAllowed = true;
                        else if (_PedModel.UseRanks)
                        {
                            if (_PedModel.AvailableForRanks.Contains(playerData.Rank)) _isAllowed = true;
                        }
                        else if (!_PedModel.UseRanks)
                        {
                            if (_PedModel.AvailableForDepartments.Contains(playerData.DepartmentID)) _isAllowed = true;
                        }

                        if (_isAllowed)
                        {
                            menuListItem_PedModels.ListItems.Add(_PedModel.Name);
                            menuListItem_PedModels.ItemData.Add(_PedModel.Model); 
                        }
                    }

                    menu.AddMenuItem(menuListItem_PedModels);
                }

                
                MenuListItem menuListItem_Loadouts = new MenuListItem("Get Loadout", new List<string>(), 0);
                menuListItem_Loadouts.ItemData = new List<List<Weapon>>();
                foreach (Loadout _Loadout in Common.Loadouts)
                {
                    bool _isAllowed = false;

                    if (_Loadout.IsAvailableForEveryone) _isAllowed = true;
                    else if (_Loadout.UseRanks)
                    {
                        if (_Loadout.AvailableForRanks.Contains(playerData.Rank)) _isAllowed = true;
                    }
                    else if (!_Loadout.UseRanks)
                    {
                        if (_Loadout.AvailableForDepartments.Contains(playerData.DepartmentID)) _isAllowed = true;
                    }

                    if (_isAllowed)
                    {
                        menuListItem_Loadouts.ListItems.Add(_Loadout.Name);
                        menuListItem_Loadouts.ItemData.Add(_Loadout.Weapons);
                    }
                }
                menu.AddMenuItem(menuListItem_Loadouts);

                MenuItem menuItem_ReturnWeapons = new MenuItem("Return all weapons");
                menu.AddMenuItem(menuItem_ReturnWeapons);

                MenuItem menuItem_HealthArmor = new MenuItem("Refill health & armor");
                menu.AddMenuItem(menuItem_HealthArmor);

                MenuItem menuItem_DutyToggle = new MenuItem("Go " + (_isPlayerOnDuty ? "off" : "on") + " duty", "")
                {
                    Enabled = true,
                    ItemData = _isPlayerOnDuty
                };
                menu.AddMenuItem(menuItem_DutyToggle);


                menu.OnItemSelect += (_menu, _item, _index) =>
                {
                    if(_index == menuItem_ChangeOutfit.Index)
                    {
                        menu.CloseMenu();
                        ExecuteCommand("eup");
                    }
                    else if(_index == menuItem_ReturnWeapons.Index)
                    {
                        Game.PlayerPed.Weapons.RemoveAll();
                        Common.DisplayNotification("All weapons returned to armory");
                    }
                    else if(_index == menuItem_HealthArmor.Index)
                    {
                        Game.PlayerPed.Health = Game.PlayerPed.MaxHealth;
                        Game.PlayerPed.Armor = Game.Player.MaxArmor;
                        Common.DisplayNotification("Health & armor refilled");
                    }
                    else if(_index == menuItem_DutyToggle.Index)
                    {
                        Utilities.SetPlayerDuty(!(bool)_item.ItemData);
                        _item.Text = "Go " + (!(bool)_item.ItemData ? "off" : "on") + " duty";
                        _item.ItemData = !(bool)_item.ItemData;
                    }
                };


                menu.OnListItemSelect += (_menu, _listItem, _listIndex, _itemIndex) =>
                {
                    if (_itemIndex == menuListItem_PedModels.Index)
                    {
                        List<string> _ItemData = _listItem.ItemData;
                        _ = ChangePlayerPed(_ItemData.ElementAt(_listIndex));
                    }
                    else if (_itemIndex == menuListItem_Loadouts.Index)
                    {
                        Game.PlayerPed.Weapons.RemoveAll();

                        List<List<Weapon>> _ItemData = _listItem.ItemData;
                        foreach (Weapon _Weapon in _ItemData.ElementAt(_listIndex))
                        {
                            uint _weaponHash = (uint)GetHashKey(_Weapon.Model);

                            GiveWeaponToPed(PlayerPedId(), _weaponHash, _Weapon.Ammo, false, false);
                            SetPedAmmo(PlayerPedId(), _weaponHash, _Weapon.Ammo); // Need to call this; GiveWeaponToPed always adds ammo up

                            if (_Weapon.Components.Length > 0) {
                                foreach (string _weaponComponent in _Weapon.Components) {
                                    GiveWeaponComponentToPed(PlayerPedId(), _weaponHash, (uint)GetHashKey(_weaponComponent));
                                }
                            }
                        }
                        Common.DisplayNotification("Loadout received");
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
        internal List<PedModel> PedModels = new List<PedModel>();

    }
}
