# Realistic Parking Mod for Cities Skylines II
Published!  Link to Paradox Mods: https://mods.paradoxplaza.com/mods/87313/Windows

This mod aims to make the parking behavior more realistic and improve traffic efficiency.

## Features
* **Induced Demand for Parking**: With this mod, parking spots will only accept a limited amount of pathfinding requests, so once enough cars start driving towards a spot, it will become closed off to future pathfinding. This makes it so that there will be a more realistic amount of cars in the city relative to the amount of available parking.
* **Reduced Parking Reroute Distance**: In vanilla, cars can detect an unavailable parking spot up to 4000 nodes away, causing them to make erratic and dangerous u-turns when they reroute to a new spot. This mod allows you to reduce that number so that cars will only try to find new parking once they are near their target parking spot.
* **Increased Parking Garage Capacities**: In vanilla, apartments only have 2-3 parking spots in their garages, even if there are hundreds of residents, and high-rise offices also usually only have 30 spots, regardless of the number of employees. This mod alters the parking garage capacity to be dependent on the household and worker count of the property. Note that many building assets do not have a parking garage built in, and this mod does not add them in.
* All features can be disabled if wanted

## Adding to an existing save
* This mod can be safely added to an existing save, but it may cause some weird behavior at first. Since the demand system does not affect existing pathfinding requests from before the mod, if you have limited parking in your city, you may see cars flocking to one parking spot before rerouting away. To alleviate this behavior, I recommended that you keep your reroute distance high at first and wait for the these cars to reroute.
* The garage capacity modification should be set with the button in the settings or else it will take a while to take affect, as it is only updated in-game when a car routes to or leaves the garage.

## Removing from a save
* This mod can be safely removed from a save, but the induced demand system does alter some vanilla values, so I recommend disabling the system in the settings and letting it run for a couple seconds before saving and removing the mod. This resets parking availability back to vanilla. If you don't do this, in the small possibility that all cars routing to a parking spot are despawned or deleted, the parking spot may remain closed off to pathfinding forever.
* The garage capacity system also alters vanilla values, but it will be reset by the vanilla system once the mod is removed.
* The parking reroute distance system does not affect vanilla values.

## Notes
* Parking garage's vehicle count values may appear bugged, as it will rise without cars entering. This is a limitation of the current implementation, as this count is used in pathfinding and representative of the garage's demand + actual count. I might change it in the future to show the actual count if many people want it.

## Credits
* To krzychu124 for their Scene Explorer mod and mattswarthout for their Cim Route Highlighter mod which I used extensively for development
* All the lovely mod creators who I stalked their github repos to learn how to set up the code for this mod