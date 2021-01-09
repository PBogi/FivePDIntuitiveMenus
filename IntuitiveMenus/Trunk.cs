using CitizenFX.Core;
using FivePD.API;
using System;
using System.Threading.Tasks;
using static CitizenFX.Core.Native.API;
using Newtonsoft.Json.Linq;
using FivePD.API.Utils;
using System.Collections.Generic;
using MenuAPI;
using System.Linq;


#pragma warning disable 1998
namespace IntuitiveMenus
{
    class Trunk
    {
        internal async Task OpenTrunk()
        {
            float triggerDistance = 2.0f;

            Vector3 entityWorld = GetOffsetFromEntityInWorldCoords(PlayerPedId(), 0.0f, triggerDistance + 5, 0.0f);
            int rayHandle = CastRayPointToPoint(Game.PlayerPed.Position.X, Game.PlayerPed.Position.Y, Game.PlayerPed.Position.Z, entityWorld.X, entityWorld.Y, entityWorld.Z, 10, PlayerPedId(), 0);
            bool _Hit = false;
            Vector3 _endCoords = new Vector3();
            Vector3 _surfaceNormal = new Vector3();
            

            GetRaycastResult(rayHandle, ref _Hit, ref _endCoords, ref _surfaceNormal, ref vehicleHandle);

            if (DoesEntityExist(vehicleHandle) && GetVehicleClass(vehicleHandle) == 18)
            {
                
                Vector3 trunkPos = GetWorldPositionOfEntityBone(vehicleHandle, GetEntityBoneIndexByName(vehicleHandle, "boot"));
                if (Game.PlayerPed.Position.DistanceTo(trunkPos) < triggerDistance)
                {
                    RequestAnimDict(AnimDict);

                    int maxretries = 0;
                    while (!HasAnimDictLoaded(AnimDict) && maxretries < 10)
                    {
                        await BaseScript.Delay(25);
                        maxretries++;
                    }

                    if (GetVehicleDoorAngleRatio(vehicleHandle, 5) > 0)
                    {
                        SetVehicleDoorShut(vehicleHandle, 5, false);
                        StopAnimTask(PlayerPedId(), AnimDict, "fixing_a_ped", 4f);
                    }
                    else
                    {
                        _ = OpenMenu();

                        SetCurrentPedWeapon(PlayerPedId(), (uint)GetHashKey("WEAPON_UNARMED"), true);
                        SetVehicleDoorOpen(vehicleHandle, 5, false, false);

                        if (HasAnimDictLoaded(AnimDict))
                        {
                            TaskPlayAnim(
                                PlayerPedId(), // ped
                                AnimDict, // Anim Dictionary
                                "fixing_a_ped", // Animation
                                4.0f, // Blend in speed
                                4.0f, // Blend out speed
                                -1, // Duration
                                1, // Flag
                                0.5f, // Playback Rate
                                false, // Lock X
                                false, // Lock Y
                                false // Lock Z
                            );
                        }
                        await BaseScript.Delay(100);
                        SetEntityNoCollisionEntity(PlayerPedId(), vehicleHandle, true);
                    }
                }
                else if (IsEntityPlayingAnim(PlayerPedId(), AnimDict, "fixing_a_ped", 3))
                {
                    StopAnimTask(PlayerPedId(), AnimDict, "fixing_a_ped", 4f);
                }
            }
        }

        internal Menu menu;
        string AnimDict = "mini@repair";
        int vehicleHandle = 0;

        internal async Task OpenMenu()
        {
            PlayerData playerData = Utilities.GetPlayerData();

            menu = new Menu("Trunk");
            MenuController.AddMenu(menu);


            List<string> menuList_Loadouts = new List<string>() { };

            foreach (var Loadout in Loadouts)
            {
                if ((bool)Loadout.Value["isAvailableForEveryone"])
                {
                    MenuItem _menuButton = new MenuItem("Take " + Loadout.Key);
                    foreach (var weapon in Loadout.Value["weapons"])
                    {
                        if (HasPedGotWeapon(PlayerPedId(), (uint)GetHashKey(weapon["weapon"].ToString()), false)) _menuButton.Text = "Put back " + Loadout.Key;
                    }                        
                    menu.AddMenuItem(_menuButton);
                }
                else if ((bool)Loadout.Value["useRanks"])
                {
                    string[] availableForRanks = Loadout.Value["availableForRanks"].Values<string>().ToArray();
                    if (availableForRanks.Contains(playerData.Rank))
                    {
                        MenuItem _menuButton = new MenuItem("Take " + Loadout.Key);
                        foreach (var weapon in Loadout.Value["weapons"])
                        {
                            if (HasPedGotWeapon(PlayerPedId(), (uint)GetHashKey(weapon["weapon"].ToString()), false)) _menuButton.Text = "Put back " + Loadout.Key;
                        }
                        menu.AddMenuItem(_menuButton);
                    }
                }
                else if (Loadout.Value["availableForDepartments"] != null)
                {
                    int[] availableForDepartments = Loadout.Value["availableForDepartments"].Values<int>().ToArray();
                    if (availableForDepartments.Count() == 1 || availableForDepartments.Contains(playerData.DepartmentID))
                    {
                        MenuItem _menuButton = new MenuItem("Take " + Loadout.Key);
                        foreach (var weapon in Loadout.Value["weapons"])
                        {
                            if (HasPedGotWeapon(PlayerPedId(), (uint)GetHashKey(weapon["weapon"].ToString()), false)) _menuButton.Text = "Put back " + Loadout.Key;
                        }
                        menu.AddMenuItem(_menuButton);
                    }
                }
            }           

            menu.OnItemSelect += (_menu, _item, _index) =>
            {
                if (_item.Text.StartsWith("Take"))
                {
                    foreach (var weapon in Loadouts[_item.Text.Replace("Take ", "")]["weapons"])
                    {
                        int ammo = (int)weapon["ammo"];
                        uint weaponHash = (uint)GetHashKey((string)weapon["weapon"]);

                        GiveWeaponToPed(PlayerPedId(), weaponHash, ammo, false, true);

                        if (weapon["components"] != null)
                        {
                            foreach (string weaponComponent in weapon["components"].Values<string>().ToArray())
                            {
                                Console.WriteLine(weaponComponent);
                                GiveWeaponComponentToPed(PlayerPedId(), weaponHash, (uint)GetHashKey(weaponComponent));
                            }

                        }

                    }
                    _item.Text = _item.Text.Replace("Take", "Put back");
                }
                else if (_item.Text.StartsWith("Put back"))
                {
                    foreach (var weapon in Loadouts[_item.Text.Replace("Put back ", "")]["weapons"])
                    {
                        uint weaponHash = (uint)GetHashKey((string)weapon["weapon"]);
                        RemoveWeaponFromPed(PlayerPedId(), weaponHash);
                        _item.Text = _item.Text.Replace("Put back", "Take");
                    }
                }

                /*

                if (_item == menuButton_Shotgun)
                {
                    ammo = 250;
                    weaponHash = (uint)GetHashKey("WEAPON_PUMPSHOTGUN");
                }
                else if(_item == menuButton_Rifle)
                {
                    ammo = 250;
                    weaponHash = (uint)GetHashKey("WEAPON_CARBINERIFLE");
                }
                else if(_item == menuButton_FireExt)
                {
                    ammo = 1000;
                    weaponHash = (uint)GetHashKey("WEAPON_FIREEXTINGUISHER");
                }
                else
                {
                    ammo = 250;
                    weaponHash = (uint)GetHashKey("WEAPON_PISTOL");
                }

                if (_item.Text.StartsWith("Take"))
                {
                    GiveWeaponToPed(PlayerPedId(), weaponHash, ammo, false, true);
                        SetAmmoInClip(PlayerPedId(), weaponHash, ammo);
                        //SetPedInfiniteAmmo(PlayerPedId(), true, weaponHash);
                        // TODO: More ammo
                    
                    _item.Text = _item.Text.Replace("Take", "Put back");
                }
                else if (_item.Text.StartsWith("Put back"))
                {
                    RemoveWeaponFromPed(PlayerPedId(), weaponHash);
                    _item.Text = _item.Text.Replace("Put back", "Take");
                }*/
            };

            menu.OnMenuClose += (_menu) =>
            {
                StopAnimTask(PlayerPedId(), AnimDict, "fixing_a_ped", 4f);
                SetVehicleDoorShut(vehicleHandle, 5, false);
                vehicleHandle = 0;
            };

            menu.OpenMenu();
        }

        internal JObject Loadouts = new JObject();
    }
}
