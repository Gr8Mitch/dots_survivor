# DOTS Survivor
This project is simply a small technical demo to show some of my Unity DOTS code.
I chose to make a pseudo Vampire Survivor because the large amount of enemies that are simulated at the same time could make the ECS architecture shine.

[![Watch the video](https://i.ytimg.com/vi/pgxC2rv8ITI/maxresdefault.jpg?sqp=-oaymwEmCIAKENAF8quKqQMa8AEB-AH-CYAC0AWKAgwIABABGCMgZSgoMA8=&amp;rs=AOn4CLDg4ePh_yY_Pr_Lpwv2VNyRGgcORw)](https://www.youtube.com/watch?v=pgxC2rv8ITI)

The gameplay is really basic: the player must avoid the enemies. It has an ability that deals periodic damages in a zone.
The enemies can hurt him by touching him or sending a projectile on him.

## Scope
This project was never meant to be shippable at some point, or at least not as is.
The fact that it would remain small explains a few choices I did, including:
- the organization of the assets in the project
- the fact that I only have one assembly
- keeping some code that could actually have been cleaner
- the "programmer" art

## Implementation
### Character controller
The main goals in the implementation were to have a clean and optimized code, with a good and scalable architecture.
The character controller was strongly inspired by the [Character Controller package](https://docs.unity3d.com/Packages/com.unity.charactercontroller@1.3/manual/index.html). I picked the minimum required and adapted it for my purpose.
I did a few optimizations (like using raycast for the ground and interpolate the result for the enemies), although the collider casts are not that heavy here, even with hundreds of enemies. I wanted to see how far I could go.

### Navigation
The navigation is really basic here. The enemies just go towards the player, and the character colliders does the rest like making them slide along the obstacles and between themselves.
A propably more suitable solution could have been to use a Burst/Job integration of Recast Navigation and the navmesh to find the ground height (we probably don't need to be that accurate for the enemies) and maybe the crowd management.
I did such an integration in my previous job (only the navmesh building/pathfinding requests) from the c++ code, and it is very performant once the navmesh is built. But it was quite out of scope.

## Performances
The game runs very smoothly on an AMD Ryzen 5 5600X (10 worker threads).
With 600 enemies simulated, the player loop taking more or less 4-5 ms in total (less than 2.5ms if don't take into account the rendering).
This is merely linked to the fact that 95% of the code I wrote is Bursted and jobified.
There are also few sync points but if the scope of the project grows, they could be removed by adding a few systems before the systems that needs to complete some jobs.

<img width="3808" height="1748" alt="dots survivor profiler 2" src="https://github.com/user-attachments/assets/df9d8ce7-4e56-4d13-a299-e60759ad2e28" />

## Backlog
- Menu Scene
- Death + respawn of the player character
- Vfx (death, damages, etc...)
- Sounds
- Abilities leveling
