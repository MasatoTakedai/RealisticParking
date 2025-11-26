# Realistic Parking Mod for Cities Skylines II
Published!  Link to Paradox Mods: https://mods.paradoxplaza.com/mods/87313/Windows

Are you tired of your city roads getting gridlocked by traffic looking for (nonexistent) parking?
Tired of cars and bikes making erratic u-turns in the middle of your main roads?
Tired of parking garages for high-rise buildings barely having any parking spots?

This mod aims to make the parking behavior more realistic and improve traffic efficiency.

## Features
* **Induced Demand for Parking**: With this mod, parking spots will only accept a limited amount of pathfinding requests, so once enough cars and bikes start going towards a spot, it will become closed off to future pathfinding. This makes for a more realistic amount of cars and bikes in the city relative to the amount of available parking and keeps cims from parking far from their destination.
* **Reduced Parking Reroute Distance**: In vanilla, cars and bikes can detect their parking destination  getting taken up to 40000 nodes away, causing them to make erratic and dangerous u-turns when they reroute to a new spot. This mod allows you to reduce that range so that they will only see if their spot is taken once they are near their target parking spot.
* **Increased Parking Garage Capacities**: In vanilla, apartments only have 2-3 parking spots in their garages, even if there are hundreds of residents, and high-rise offices also usually only have 30 spots, regardless of the number of employees. This mod alters the parking garage capacity to be dependent on the household and worker count of the property.
* All features can be disabled if wanted

## Adding to an existing save
* This mod can be safely added to an existing save, but it may cause some weird behavior at first. Since the demand system does not affect existing pathfinding requests from before the mod, you may see cars and bikes flocking to one parking spot before rerouting away. To alleviate this behavior, I recommended that you keep your reroute distance high at first and wait for the them to reroute.

## Removing from a save
* This mod can be safely removed from a save, as while some features overwrite vanilla values, they should be reset to their vanilla values on load by the game.

## Compatability
* There are no current known incompatabilities with this mod.

## Notes
* Many building assets do not have a parking garage built in, and this mod does not add them in.

## Credits
* To krzychu124 for their Scene Explorer mod and mattswarthout for their Cim Route Highlighter mod which I used extensively for development
* All the lovely mod creators who I stalked their github repos to learn how to set up the code for this mod