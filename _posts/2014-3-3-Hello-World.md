---
layout: post
title: "A Turn-Based Game Loop"
published: true
---

Now that my roguelike written in Dart is open source, I wanted to talk about a piece of it that I put a lot of time into. Well, I actually poured way too much of my life into lots of parts of this game, and maybe I’ll write about those too, but for now let’s start where most games start: with the game loop.

But today we have more computing power in our pants pocket than you need to travel to the moon, so we might as well spend it all on simulating the wave equations in realtime!

![](http://2.bp.blogspot.com/-yidM20wBEw0/U20xHf3OlSI/AAAAAAAAAEw/ps_acZ4t1uw/s1600/demo10.png)

The wave equation describes, as the name suggests, how waves behave. Of course, it is not limited to water waves - it's fully applicable for any other wibbly wobbley physical phenomenon you can imagine.

The equation is known as a partial differential equation, if that tells you anything. If not, then all you need to know is that they are awesome, and solving them analytically is a mathematician's worst nightmare. Thankfully, we have powerful computers that can solve anything, if we give them enough time.

A method of solving them is described here, taken from the page in the link below. There's also more cool stuff on that page - like how to simulate actual fluids, with density and pressure and stuff.

But basically, you divide your area of interest into an evenly spaced grid. For each grid position you store a height- and a velocity value. Then for each tick in your simulation you perform some math on these values and their neighbors, to approximate the next height and velocity at all points. (It boils down to solving a Poisson equation).

To render the water I calculate the surface normal for each pixel. Using the normal and the viewing vector, we can calculate a reflected and a refracted vector to use for lookup in a cubemap skybox. To blend between the reflection and the refraction, I use Fresnel's law. Or rather, an approximation to it, which uses the dot product between the view vector and normal vector.

Finally, to get the whole thing running fast, I perform the simulation on the GPU. I represent the grid of height and velocity values as a seperate rendertarget - a texture that can be written to, but not necessarily shown on screen. To move the simulation forward I need two of those, as one is used for input while the other is used for output. I then ping-pong between them. The solving part is done per-fragment in a shader, and written back to the rendertarget.

As usual, the code for this demo can be found on my Github.

##  Reading material

* Fluid simulation for Computer Animation
* Anton's OpenGL 4 Tutorials: Cube Maps