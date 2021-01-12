# Optional configuration
## Change/Add Peds
If you have addon peds or want a different ped selection, you can do so by adding
a pedmodels.json in the fivepd\config folder.
Additionally you'll have to add './config/pedmodels.json' to the __resource.lua "files" section.

## Change/Add Weapons in Trunk
Edit the server-data\resources\fivepd\config\loadouts.json file and add loadouts with the first "availableForDepartments" value set to 9999.
You can also do this for existing loadouts if you want, but please note, that when the player
puts the loadout back into the truck, all weapons from the loadout will be affected.

## Change/Add Garage/Locker coordinates
Edit the server-data\resources\fivepd\config\coordinates.json file and add a "lockers" and/or "garages" section as per the example file.
