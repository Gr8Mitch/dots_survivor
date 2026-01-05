# DOTS Survivor
<img width="2195" height="1236" alt="dots survivor" src="https://github.com/user-attachments/assets/04e43f52-3049-4148-9700-553d478db482" />

This project is simply a small technical demo to show some of my Unity DOTS code.
I chose to make a pseudo Vampire Survivor because the large amount of enemies that are simulated at the same time could make the ECS architecture shine.

## Scope
This project was never meant to be shippable at some point, or at least not as is.
The fact that it would remain small explains a few choices I did, including:
- the organization of the assets in the project
- the fact that I only have one assembly
- keeping some code that could actually have been cleaner
- the "programmer" art

## Implementation
The main goals in the implementation were to have a clean and optimized code, with a good and scalable architecture.
The character controller was strongly inspired by the [Character Controller package](https://docs.unity3d.com/Packages/com.unity.charactercontroller@1.3/manual/index.html). I picked the minimum required and adapted it for my purpose.
I did a few premature optimizations on this to be honest (for instance trying to interpolate the results to do less collider casts or using raycasts instead), but in the end the collider casts is not that heavy here, even with hundreds of enemies.
There are also few sync points but if the scope of the project grows, they could be removed by adding a few systems before the systems that needs to complete some jobs.

## Performances
The game runs very smoothly on an AMD Ryzen 5 5600X (10 worker threads).
With 600 enemies simulated, the player loop taking more or less 4-5 ms in total (less than 2.5ms if don't take into account the rendering).
This is merely linked to the fact that 95% of the code I wrote is Bursted and jobified. 

<img width="2349" height="1009" alt="dots survivor profile" src="https://github.com/user-attachments/assets/0b8818cb-2b34-450a-8c1c-93cae00cea8e" />
