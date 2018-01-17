# The space of rotations

**"If all you have is a hammer, everything looks like a nail."**

A familiar proverb that, as noted by Chris Hecker in his [GDC talk](todo), has an unappreciated cousin:

**"If you can turn anything into a nail, all you need is a hammer."**

Chris Hecker's message was that *numerical optimization* can solve *a lot* of different problems in the same way, making it a sort of hammer in your maths toolbox. Consequently, if you have a library or program (like MATLAB) that can do optimization, you can solve any problem that you can wrangle into the right form, all using the same tool.

With increasing computer power and a desire for rapid iteration, having a hammer readily available can be of great value, even if it doesn't solve the problem as cleanly or as run-time-efficiently as it could, because it can save you a lot of programmer time.

I have since come to appreciate optimization as a powerful technique that can solve problems whose closed-form solution (if it even exists) is so far beyond my mathematics / algorithms knowledge that I couldn't even begin to approach it in any other way.

I have also learned that 3D rotations are weird. But to explain this, I have to dive into a specific problem.

Estimating rotations
--------------------

A common problem in computer vision is finding out how a thing is rotated and translated.

If you want to make a quadcopter [land on a robotic vacuum cleaner](todo: iarc) using a camera, part of the problem is calculating where you are relative to the robot&mdash;or where the robot is relative to you&mdash;so you know where you need to go.

<!-- ![](lander.gif) -->

If you want reconstruct a 3D model of a scene from photographs, part of the problem is calculating how the camera was rotated and translated between each photo. Using that you can triangulate the 3D coordinate of corresponding pixels by casting rays in the direction they came from and computing where they intersect in 3D.

Calculating how your vacuum cleaner robot is positioned relative to your quadcopter, or how a camera moves through a scene as it takes photos of it, can both be turned into a type of optimization problem&mdash;a nail for our hammer.

However, it'll involve **3D rotations**, and that is where things can get nasty.

Example problem
---------------

Books and CD covers are often used in example problems (and in youtube videos of object tracking algorithms) because they have a lot of **texture**, making them an easy case for computer vision algorithms.

Here's a book I picked from my shelf.

<img src="book/book2.jpg" style="max-width:320px;width:100%;">

It's a **pretty good book**.

But to simplify our mathematical discussion we'll assume this book is nothing more than a 3D box filled with void. Thus, one way to find out how the book is positioned relative to the camera (or vice versa) starts by finding matching patches of pixels between the photo and the 3D model.

![](matches.png)

At this point, your typical computer vision text book will start to tell you about the Perspective-N-Point problem and show you how easily you can recover the rotation and translation matrices using linear algebra and Singular Value Decomposition...

...but that's an **elegant solution**.

We don't have time to learn this PnP stuff, but we do know how to use a hammer and we don't care about being efficient (maybe later we'll have to dig into it, but not right now). So let's turn this problem into a nail.

## Pose estimation as an optimization problem

A nail version of this problem is similar to most nail versions of problems, and consists of
1. guessing the answer,
2. measuring how wrong it was, and
3. guessing the answer again (but now you are educated).

In the context of book-pose-estimation, it means we guess the pose of the book. To measure how bad our guess was, we can render the book as seen by my camera (a handheld Canon with heavy lens distortion).

![](reproject1.jpg)

We can look at this as a person and say "yup that's pretty close". But it's too slow to ask a person after each guess. If we want to automate this with a computer we need to be *quantitative*. We have lots of options to measure the quantitative quality of our guess.

Here's one that's pretty popular...

![](reproject2.jpg)

When we found pixel patches in the photograph and searched for matching patches in our 3D book, we got a bunch of 2D-3D correspondences: for each 2D patch coordinate in the photo, we have one 3D coordinate on the 3D box. One measure of the quality of our guess is the average squared distance between those 3D coordinates (projected into the image) and their matching 2D coordinates.

![](reproject3.jpg)

In pseudo-code we could write this as

    measure_quality(matrix3x3 R, vector3 T):
        e = 0
        for u,v,p in patches
            u_est,v_est = camera_projection(R*p + T)
            du = u_est-u
            dv = v_est-v
            e += du*du + dv*dv
        return e / num_patches

`u,v` is the 2D coordinate for each patch in the photo and `p` is the corresponding 3D coordinate. The 3D vector `p` is first transformed (by the rotation matrix R and translation vector T) from box coordinates into camera coordinates, and then transformed to a 2D vector by perspective projection.

Our quality measure is a function of the rotation and translation. Plug in R and T, get a value. The value is zero when the predicted 2D coordinates match the observed ones, and positive otherwise (in that sense we should call it a measure of error rather than quality). So if we want to find the true pose of the book, we just need to find values for R and T that make the error as small as possible.

How? Well I wrote gradient descent in the title of this article, and somewhere along the line I was going to use it to make a point about Euler angles...

Gradient descent
----------------

We want to adjust R and T to make the error smaller. One way to do so is to look at how the error changes for a change in R and T. For example, if we had the function `f(x) = x^2`, the derivative with respect to x (the gradient) says how the value of f changes for an increase in x. In this case, the derivative is `dfdx(x) = 2x`, so f will decrease as x goes from negative infinity to zero, and increase as x goes from zero to positive infinity..

The gradient is an indication of the direction we can adjust our parameters: If the gradient is positive, it means f increases for an increase of x (so we should decrease x); if the gradient is negative, f will decrease for an increase of x (so we should increase x).

One way to adjust x, starting from an initial guess, could therefore be `x += -gain*dfdx(x)`.

![](gradientdescent.png)

This will make f(x) smaller and smaller until it stops, hopefully at zero. With some luck, the value of x at that point is even what you wanted. (Also likely is that it blows up to infinity, if you're not careful, but decent software packages do additional checks and number-massaging to prevent that)

The derivative of a rotation matrix
-----------------------------------

The derivative of `x^2` is simple, but it might take you longer than you'd like to differentiate more complex expressions, possibly involving matrix multiplications and stuff. Luckily we have some pretty neat tools to do that for us&mdash;gone are the days when it was a symbol of hard work and dedication if your paper had pages upon pages of calculus, rigorously deriving each expression by hand (I still see papers like that for some reason).

Look for libraries with *automatic differentiation*. Or, use a *symbolic processor* (found in MATLAB and Octave) to derive analytic expressions and translate them into your code. There's also an online [matrix calculus tool](todo). But the simplest solution might just be botch it with finite differences:

    dfdx = (f(x+dx) - f(x-dx)) / 2dx

carefully selecting dx to be small enough, but not so small as to cause a floating point catastrophy. If you have a function with multiple arguments, like our error function, you take the derivative of each one:

    dfdx = (f(x+dx, y, z) - f(x-dx, y, z)) / 2dx
    dfdy = (f(x, y+dy, z) - f(x, y-dy, z)) / 2dy
    dfdz = (f(x, y, z+dz) - f(x, y, z-dz)) / 2dz

This works for any ugly function you can reasonably code up. In fact, our error function is pretty ugly: it has matrix multiplications and a weird 3D-2D projection with lens distortion. It would surely be more efficient (runtime-wise) to derive an analytic expression, but the generality of finite differences makes it nice when you're pressed on time.

... wait ...

You know that feeling when you realize something is harder than you first thought?

How do we take the derivative with respect to a rotation matrix? It's not a 3x3 pile of numbers we can choose freely, since not all 3x3 matrices is a valid rotation matrix; there's some constraints between the elements. So we can't just do finite differences on 9 numbers `r11, r12, r13 ...` for each element in the matrix, and use that for our gradient &sup1;.

What people usually do at this point is to parametrize the rotation matrix in terms of something else - like Euler angles.

<span style="color:#999;">
&sup1; We could, but then we get a *constrained* optimization problem to ensure that `r11, r12, ...` are kept in the space of valid rotation matrices. We have tools to solve those, but for some reason I never see people do it for estimating rotations. Quaternions calls for constrained optimization, but I've only seen people treat it as unconstrained and normalize the quaternion after each update.
</span>

Using Euler angles
------------------

Euler angles is a so-called *minimal* parametrization, in that they use the minimal amount of numbers (three) to define a rotation. By virtue of being minimal, those numbers can each be chosen freely, without concern or being constrained by the others.

That sounds a bit like what we're after, so we'll add a function that takes three angles and returns a rotation matrix following some Euler angle convention, like x,y,z or z,y,x. In total we then have three variables for rotation (rx,ry,rz) and three variables for translation (tx,ty,tz).

We can now update those six variables with gradient descent like so (I abbreviated the quality measure function to E):

    update_parameters(rx,ry,rz, tx,ty,tz):
        dedrx = (E(euler(rx+drx, ry, rz), [tx, ty, tz]) -
                 E(euler(rx-drx, ry, rz), [tx, ty, tz])) / 2drx
             ...
        dedtz = (E(euler(rx, ry, rz), [tx, ty, tz+dtz]) -
                 E(euler(rx, ry, rz), [tx, ty, tz-dtz])) / 2dtz

        rx -= gain*dedrx
        ry -= gain*dedry
        rz -= gain*dedrz
        tx -= gain*dedtx
        ty -= gain*dedty
        tz -= gain*dedtz

This will **sort of** work!

![](gradientdescent.gif)

It's a bit slow and unstable... but there's ways to fix that (like using someone else's library)&sup1;.

<span style="color:#999;">
&sup1;When I made this gif my parameters did blow up on the first try. I hacked in a fix by adding a line search: instead of choosing an arbitrary gain thing, you instead check the error at several points along the gradient direction and go to the point that had the lowest error. I also normalized the differences in the error function by dividing by the image width: squaring pixel coordinates gave really big values. It's still super slow, as you can see. That can be improved by using cooler methods like Gauss-Newton or Levenberg-Marquardt.
</span>

What is gimbal lock?
--------------------

Consider a plate that you can rotate by three angles rx, ry and rz around the x-, y- and z-axes respectively with the matrix `Rz(rz)*Ry(ry)*Rx(rx)`. Adjusting one of the angles in isolation produces these three motions:

![](plates1xyz.png)

They all look clearly different, but a funny thing happens as the angle about the y-axis approaches 90 degrees. Here's an illustration:

<!-- ![](gimballock.gif) -->

<!-- * For the red plate I keep rz fixed and adjust rx back and forth.
* For the blue plate I keep rx fixed and adjust rz back and forth (in the opposite direction).
* I repeat this while slowly adjusting ry from zero to 90 degrees -->

<style>
.slider img {
    display:inline;
    max-width:none;
    padding:0;
    margin:0;
}
.slider {
    display:inline-block;
    overflow-y:hidden;
    overflow-x:hidden;
    border:1px solid #ccc;
}
.slider-wrap { width:fit-content; margin:0 auto; }
input { vertical-align: middle; }
@media screen and (max-width: 600px){
.slider { width:160px; height:160px; }
.slider img { width:160px; height:160px;}
}
</style>
<div class="slider-wrap">
    <div class="slider" id="slider1" style="max-width:160px;max-height:180px;">
        <div style="width:700px;">
            <img src="plates2x0.png"/><img src="plates2x3.png"/><img src="plates2x5.png"/><img src="plates2x8.png"/>
        </div>
    </div>
    <div class="slider" id="slider2" style="max-width:160px;max-height:180px;">
        <div style="width:700px;">
            <img src="plates2z0.png"/><img src="plates2z3.png"/><img src="plates2z5.png"/><img src="plates2z8.png"/>
        </div>
    </div>
    <br>
    <input type="range" min=0 max=3 step=1 value=0 oninput="document.getElementById('slider2').scrollLeft = this.value*160; document.getElementById('slider1').scrollLeft = this.value*160;"></input>
    <label>rotate y [0 to 90 degrees]</label>
</div>

The two plates start out rotating about different axes, as you'd expect, but along the way, the right one mysteriously starts looking more and more like the left one, until they finally look identical (although in opposite directions). So what? Well here's an interactive puzzle for you.

Try to rotate the book to match the photo:

<div class="slider-wrap">
    <img src="book/book1.jpg" style="max-width:240px;">
    <div class="slider" id="slider3" style="max-width:240px;max-height:240px;">
        <div style="width:800px;">
            <img src="ex0-11.png"/><img src="ex0-12.png"/><img src="ex0-13.png"/><br>
            <img src="ex0-21.png"/><img src="ex0-22.png"/><img src="ex0-23.png"/><br>
            <img src="ex0-31.png"/><img src="ex0-32.png"/><img src="ex0-33.png"/>
        </div>
    </div>
    <div class="slider" id="slider4" style="max-width:240px;max-height:240px;">
        <div style="width:800px;">
            <img src="ex1-31.png"/><img src="ex1-32.png"/><img src="ex1-33.png"/><br>
            <img src="ex0-21.png"/><img src="ex0-22.png"/><img src="ex0-23.png"/><br>
            <img src="ex1-11.png"/><img src="ex1-12.png"/><img src="ex1-13.png"/>
        </div>
    </div>
    <br>
    <input type="range" min=0 max=2 step=1 value=0 oninput="document.getElementById('slider3').scrollTop = this.value*document.getElementById('slider3').clientWidth;"></input>
    <label>rotate x (left book)</label>
    <br>
    <input type="range" min=0 max=2 step=1 value=0 oninput="document.getElementById('slider4').scrollTop = this.value*document.getElementById('slider3').clientWidth;"></input>
    <label>rotate z (right book)</label>
    <br>
    <input type="range" min=0 max=2 step=1 value=0 oninput="document.getElementById('slider4').scrollLeft = this.value*document.getElementById('slider3').clientWidth;document.getElementById('slider3').scrollLeft = this.value*document.getElementById('slider3').clientWidth;"></input>
    <label>rotate y (both books)</label>
</div>

Although you start out able to produce three distinctly different motions, you can only produce two around that magical 90 degree sideways angle, and you are unable to get that backward pitch you are after. This drop in degrees of freedom from three to two is called gimbal lock and happens no matter what Euler angle order you choose (although the point at which it happens will vary).

However, it's not like we literally cannot find three Euler angles to match the photo; I was just artifically limiting your input range. For example, (-90,45,-90) looks like this:

![](sideways45.png)

Indeed, if we rotate -90 degrees about the z-axis and -90 degrees about the x-axis, the middle rotation about the y-axis can now be used to control the pitch up or down. Try it:

<div class="slider-wrap">
    <div class="slider" id="slider5" style="max-width:240px;max-height:240px;">
        <div style="width:800px;">
            <img src="ex2-11.png"/><img src="ex2-12.png"/><img src="ex2-13.png"/><br>
            <img src="ex2-21.png"/><img src="ex2-22.png"/><img src="ex2-23.png"/><br>
            <img src="ex2-31.png"/><img src="ex2-32.png"/><img src="ex2-33.png"/>
        </div>
    </div>
    <div class="slider" id="slider6" style="max-width:240px;max-height:240px;">
        <div style="width:800px;">
            <img src="ex3-11.png"/><img src="ex3-12.png"/><img src="ex3-13.png"/><br>
            <img src="ex2-21.png"/><img src="ex2-22.png"/><img src="ex2-23.png"/><br>
            <img src="ex3-31.png"/><img src="ex3-32.png"/><img src="ex3-33.png"/>
        </div>
    </div>
    <br>
    <input type="range" min=0 max=2 step=1 value=0 onload="console.log(this)" oninput="document.getElementById('slider5').scrollTop = this.value*document.getElementById('slider5').clientWidth;"/>
    <label>rotate x (left book) [-120,-90,-60]</label>
    <br>
    <input type="range" min=0 max=2 step=1 value=0 oninput="document.getElementById('slider6').scrollTop = this.value*document.getElementById('slider5').clientWidth;"/>
    <label>rotate z (right book) [-120,-90,-60]</label>
    <br>
    <input type="range" min=0 max=2 step=1 value=0 oninput="document.getElementById('slider6').scrollLeft = this.value*document.getElementById('slider5').clientWidth;document.getElementById('slider5').scrollLeft = this.value*document.getElementById('slider5').clientWidth;"/>
    <label>rotate y (both books) [0,45,90]</label>
</div>

But alas, we find ourselves in the same rut at (-90,90,-90), where the book is seen head-on from the side. Again we can only rotate about two different axes, but now we have lost the ability to rotate the book left or right!

## How gimbal lock affects gradient descent

<!-- In the example from the previous section we didn't run into any issues because the true rotation was nowhere near gimbal lock. But what if the book is standing on its side and we take our photo from a 45 degree pitch: -->

While we can always *find* a set of angles that exactly reproduce the photo (as there is no rotation Euler angles cannot describe), the problem, in the context of gradient descent, is that those angles can be unintuitively far away from our current guess.

Remember, gradient descent only looks at small changes of the parameters&mdash;how the error increases or decreases in the small vicinity of our current estimate.

So if you're at (0, 90, 0)

![](gimballock-book.png)

but the true rotation is at (-90, 45, -90)

![](sideways45.png)

<!-- todo: overlay of translucent possible motions? -->
<!-- todo: or JS slider -->

then gradient descent will have trouble getting there, because neither of the motions you can produce with small changes of your parameters will tilt it backward.

If those motions both increase the error, it means the optimization gets stuck, unable to progress. Alternatively, it'll start adjusting the wrong parameters, say, the translation, because they are the only ones that decrease the error.

<!-- Intuitively, adjusting these parameters is less work than adjusting the gimbal-locked parameters. -->

Unless you're able to jump to the right solution directly, getting there might involve things getting worse before getting better: an intermediate rotation, say at (-45,45,-45), will look like this

![](gimballock3.png)

which is worse than the initial guess.

So if you happen to find yourself at that 90 degrees sideways angle, perhaps because you've been tracking the book for a while, then you're stuck!

<!-- Also a problem in Gauss Newton and Gradient-based methods in general. Show non-invertible Hessian. -->

## Next time

In [part two](todo) we'll actually get to the point and look at ways to solve the problem.

## (Aside) How gimbal lock affects other optimization methods

I used gradient descent for this article because I didn't want too much mathematical baggage to get in the way, but I can't think of any paper that uses it on these types of problems (those involving pose estimation). More often I see people prefer Gauss-Newton or Levenberg-Marquardt.

Like gradient descent, these also calculate the gradient of the error, but the way they use it to step toward the solution is more involved, and assumes that the error function is of a specific form (a sum of squared errors, like the one we looked at). The result is that they typically converge in much fewer steps, although each step now requires more computation.

You can read more about these methods, and some computer vision problems they're used in, at this documentation page for Ceres&mdash;an optimization library. todo. All I'll say about them is that they also suffer from this gimbal lock problem, but the way it manifests itself is actually more clear; in fact, the math shouts at you when it happens!

Skipping some details (because either you already know and you'll find it boring, or you don't know and a paragraph in a blog post won't be much help) both methods involve solving a linear system of equations, like

    J'J x = -J'y

<!-- where x is a small parameter update (that we want to find) and J is a matrix where the columns are the derivatives of each error term (distance between predicted point and observed point on the book) evaluated at the current estimate. With six parameters and 5 of those point-point correspondences, it means J will be a 6x5 matrix. -->

<!-- If you recall, what happens in gimbal lock is that two of the angle parameters produce identical rotations (although in opposite directions). This means that the derivative of the error function, with respect to those parameters, will -->

<!-- *Gradient-based methods*, like gradient descent, try find the solution by making local improvements. Other popular methods in this category are Gauss-Newton and Levenberg-Marquardt. An explanation of these is better had from an actual book than what I can type at the end of a blog post, but intuitively the difference between them and gradient descent can be illustrated by this picture:

![](gradientdescent.png)

Gauss-Newton and Levenberg-Marquardt are both based on fitting a quadratic bowl to the error function around the current estimate, and heading straight to the basin in one step. Gradient descent only looks at the slope, and makes a bunch of roundabout steps.

todo: well not really, line search

    E = sum (u' - u)^2 + (v' - v)^2
       = sum du^2 + dv^2

    du(x+h) ~ u' + Du'h - u
    dv(x+h) ~ v' + Dv'h - v

Doing this involves solving for the location of the basin, which is done by solving a matrix equation

    Hx = b

where H is called the Hessian, and is formed by taking the sum of inner-products between the Jacobians

    H = sum J'J

The Jacobian says how each error term (i.e. the difference between predicted and observed pixel patch centers) changes for a change in the parameters (the book's rotation and translation).

In this case, we would take the derivative of our predicted coordinates, u' and v', which gives us the motion that would occur if we were to adjust any one of our parameters.

Suppose our estimated state is currently located at $r_y=90^\circ$. Remember earlier how we could adjust either rx or rz and only ever produce one type of motion? This will affect the derivatives in a way that has bad consequences for the numerical stability of the optimization problem.

Intuitively, the loss of being able to move our image coordinates freely about all three axes, means that any direction we would like to move the coordinate, i.e. in order to minimize the objective, must be expressable as a linear combination of only those degrees of freedom. Otherwise, we cannot achieve that motion!

Another way to see the problem is to look at what happens to the Hessian. For a point at the top of the plate, adjusting rx alone will cause the point to move along (1,0), whereas adjusting rz alone will cause it to move exactly opposite along (-1,0). The Jacobian for this coordinate will therefore contain this submatrix inside it:

    +1 -1
     0  0

When we form the Hessian, we take the outer product of these Jacobians, which in this case would then contain the following submatrix:

    |+1 0||+1 -1|   |+1 -1|
    |-1 0|| 0  0| = |-1 +1|

Suppose we had an optimization problem where the above was our Hessian. Calculating the Gauss-Newton step involves solving the equation Hx = b for a small step x. However, H, in this case, is not invertible; there is only one degree of freedom, as Hx can only ever produce vectors of the form

         |+x1| + |-x2|   |x1-x2|   | (x1-x2)|
    Hx = |-x1| + |+x2| = |x2-x1| = |-(x1-x2)|

So we cannot even solve for an update, because there is no unique solution! Moreover, even if we were not exactly at the singular point, but nearby, H would have bad numerical properties, in that it looks like this

        +1.0000 -1.0001
    H = -1.0001 +1.0000

Matrices like this are called ill-conditioned, and have the nasty property that you will get really large values in your x vector.

One way to "fix" this problem generally, is to add a *damping* factor to the Hessian (depending on the damping factor the resulting method is then usually called Levenberg-Marquardt). For example, if we add the identity to H, we get

        +2.0000 -1.0001
    H = -1.0001 +2.0000

which is totally invertible. Of course, this doesn't fix the fundamental issue&mdash;which is that you can't adjust the parameters in the correct direction. Adding a damping factor will stabilize the solver, by "slowing down" the step, but that doesn't help if the direction is wrong. -->

There's also a class of *derivative-free optimization* methods. Particle Swarm Optimization, for one, works by evaluating the error at random locations in the parameter space, and sharing information between samples to pinpoint the precise solution. It's similar to genetic algorithms or Simulated Annealing. Another, the Nelder-Mead method, is similar in that it evaluates the error at corners of a space-filling shape, but it differs in that it moves the shape deterministically based on a set of rules.

I think these methods are less prone to getting stuck when using Euler angles because they are not confined to studying local changes of the error, as with gradient descent. Instead, they can jump to the solution (or nearby) and bypass places where, locally, it would seem like you're stuck. But without looking into it too closely, I think it *helps* to have three degrees of rotational freedom always: if the solution is visually similar to the current estimate, you don't want to require a leap of faith to somewhere else, just because your parameters don't reflect that similarity.
