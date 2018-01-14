You probably shouldn't use Euler angles in gradient descent but not for the reasons you think
====================================================================

**"If all you have is a hammer, everything looks like a nail."**

A familiar proverb that, as noted by Chris Hecker in his [GDC talk](), has an unappreciated cousin:

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

![](reproject1-v3.jpg)

We can look at this as a person and say "yup that's pretty close". But it's too slow to ask a person after each guess. If we want to automate this with a computer we need to be *quantitative*. We have lots of options to measure the quantitative quality of our guess.

Here's one that's pretty popular...

![](reproject2-v3.jpg)

When we found pixel patches in the photograph and searched for matching patches in our 3D book, we got a bunch of 2D-3D correspondences: for each 2D patch coordinate in the photo, we have one 3D coordinate on the 3D box. One measure of the quality of our guess is the average squared distance between those 3D coordinates (projected into the image) and their matching 2D coordinates.

![](reproject3-v2.jpg)

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

Look for libraries with *automatic differentiation*. Or, use a *symbolic processor* (found in MATLAB and Octave) to derive analytic expressions and translate them into your code. There's also an online [matrix calculus tool](). But the simplest solution might just be botch it with finite differences:

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

<!-- euler_xyz(rx, ry, rz):
    Rx = 1    0        0
         0 cos(rx) -sin(rx)
         0 sin(rx)  cos(rx)

    Ry =  cos(ry) 0 sin(ry)
             0    1    0
         -sin(ry) 0 cos(ry)

    Rz = cos(rz) -sin(rz) 0
         sin(rz)  cos(rz) 0
            0        0    1

    return Rx*Ry*Rz -->

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

Consider a plate that you can rotate by three angles rx, ry and rz around the x-, y- and z-axes respectively with the matrix `Rz(rz)*Ry(ry)*Rx(rx)`. Adjusting one of the angles in isolation will produce these three motions:

![](plates1xyz.png)

They all look different, as you can see, but a funny thing happens when the y-axis angle gets close to 90 degrees...

![](gimballock.gif)

For the red plate I am adjusting rx and keeping rz fixed, for the blue plate I am adjusting rz in the opposite direction and keeping rx fixed. I repeat this adjustment while adjusting ry for both plates toward 90 degrees, at which point they end up producing the same motion!

This loss of a degree of freedom is called gimbal lock, and happens no matter what Euler angle convention you use (although the specific rotation at which it happens will vary depending on the convention). This can be a problem if the true rotation is close to, or at, a gimbal lock.

For example

<img src="book/book1.jpg" style="max-width:320px;width:100%;">

Your initial guess for the book's rotation now might be (0, 90, 0), which looks like this:

![](gimballock-book.png)

<!-- Or it'll adjust unrelated parameters because they are now the ones that reduce the cost the most: for example, translating upwards and backwards. -->

Remember, gradient descent only looks at small changes of the parameters. It's true that there is a set of parameters that produces the rotation we want, but seen from our initial place, those are far away and require our optimization to get worse before it gets better.

However, neither adjusting rx or rz will produce that backward tilt.

There *is* a set of parameters that describe the rotation: if we... rx = 90, ry = 45, rz = 90. But that's way different from rx = 0, ry = 90, rz = 0, and getting there from where we are would involve increasing the cost function - getting worse before it gets better.

And once we get there we have the same problem.

<!-- In our local region (0,90,0), any small perturbation will probably produce an increase of the cost function. This means that gradient descent cannot progress any further. From the algorithm's point of view, doing anything is worse than doing nothing. -->

So if your initial guess happened to be 0,90,0&mdash;which doesn't *look* unreasonable&mdash;you'll get stuck!

Also a problem in Gauss Newton and Gradient-based methods in general. Show non-invertible Hessian.

<!-- ## Aside: Non-gradient-based optimization methods -->
<!-- One such parametrization is the *rotation matrix*: a 3x3 matrix of mutually perpendicular and unit length columns. This is not a nice parametrization, because not all 3x3 matrices are valid rotation matrices. So if you, say, wanted to generate a random rotation, you could not just sample 9 numbers and put them in a matrix. -->

<!-- Some optimization methods, like *particle swarm optimization*, try to find the optimal parameters by evaluating the error at random locations in the parameter space and share information between samples to pinpoint the location of the minimum. -->

<!-- Those methods work fine with Euler angles because they don't rely on the gradient of the error. -->

<!-- Gradient-based methods, like the first order Gauss-Newton method, try find the optimal parameters by iteratively solving a *linear* least squares problem, which involves taking the derivative of the cost function with respect to the pose parameters. It turns out that this opens a can of worms when your parameters involve rotation. -->

## Aside: It's also problematic for Gauss-Newton
When we use Gauss-Newton to find the optimal pose, our goal is to adjust the parameters with small updates, such that the resulting motion of our model will cause the cost function to decrease. For nonlinear least squares problems, this is done by linearizing each error term:

     E = sum (u' - u)^2 + (v' - v)^2
       = sum du^2 + dv^2

    du(x+h) ~ u' + Du'h - u
    dv(x+h) ~ v' + Dv'h - v

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

Fixing gimbal lock with localized Euler angles
----------------------------------------------
The example above shows that the Euler angle parametrization can lead to numerical instability, potentially causing gradient descent to slow to a grind under the right conditions, or causing Gauss-Newton to blow up when the Hessian is close to singular.

<!-- todo: replace with 3D textured book. -->
<!-- todo: show book rotating into singularity, with euler angle printed on the side -->
<!-- todo: show that we have lost a degree of freedom -->
<!-- todo: Let R0 = singularity rotation. And left-mul variable R -->

What we did above was to parametrize our pose estimate with a set of minimal global parameters (absolute Euler angles). During optimization we would then update these directly, using the Gauss-Newton step. Another approach, if we still want to use Euler angles, is to optimize with respect to *local* Euler angles, that are afterwards used to update our *global* Euler angles. We can do this by concatenating two rotations: a local rotation, that is expressed with our optimization variables, and the global rotation, that stays constant during an optimization iteration. In other words, we parametrize an incremental rotation around a stationary orientation:

    R = Rz(ez)Ry(ey)Rx(ex) R0

The key that will make this work is to keep our local coordinates small, because if they were allowed to roam freely we might end up close to the singularity again. We avoid this problem by iteratively updating the stationary orientation after every time we solve for a Gauss-Newton step. R0 is treated as constant during each optimization iteration, and would be updated by the above equation successively after each iteration. In other words, each time we formulate our optimization problem we only search for a small perturbation away from zero, and reset the 'zero'-point after each iteration.

If you try to visualize what happens here in our plate example, you will discover that even if R0 is located at the singular orientation, the left-hand matrix allows us to rotate the plate about any of the three axes, thereby recovering our lost degree of freedom! The following animation tries to illustrate this:

![](cv-why-not-euler-anim3.gif)

<!-- todo: replace with 3D textured cube. -->

The red plate is adjusting ez, the green is adjusting ey and the blue is adjusting ex. As you can see, even though the nominal rotation is located at the singularity, we can rotate about all three axes. This ought to not be very surprising, since this is equivalent to just multiplying all your points with a constant rotation matrix R, and then doing a standard Euler rotation on the result. Again, this will have gimbal lock problems as soon as ey gets close to 90 degrees, but we're avoiding that by keeping our local parameters small!

Reducing computational cost
---------------------------
The above is fine if you're doing finite differences. But if you want analytic derivatives, or you're doing automatic differentiation, you'll find it to be kinda computationally nasty&mdash;with all those cosines and sines.

The local parametrization above is pretty intuitive, but it's not very practical from a computing standpoint since exactly evaluating the derivative will involve all sorts of costly sines and cosines. However, with our assumption that the optimization parameters remain small, we can make some useful approximations.

For small values of x: $\cos(x) \approx 1$ and $\sin(x) \approx x$, so we can replace all those nasty trigonmetric functions in the local Euler matrix with linear expressions. We could also parametrize our local rotation in terms of an angle-axis rotation:

    R = (I + sin(|w|) K + (1-cos(|w|)) KK ) R0
    K = (w/|w|)^x

Again, we have some sines and cosines, and also a divide by the length of something, so this doesn't seem like an improvement. But again, for small parameters, |w| is close to zero, and we can approximate the above as

    R = (I + w^x) R0

where w^x is the skew-symmetric form of a vector containing what we will be our local rotation parameters. The derivative of this with respect to our parameters is now extraordinarily simple, and involves no trigonometric functions whatsoever.

    q' = (I + w^x) R0 p + T + dT
       = (I + w^x) (R0 p + T) + dT - (I + w^x) T
       = (I + w^x) q + (dT - w^x T) - T
       = (I + w^x) q + v - T

    diff(q',w) = diff(w^x q,w) = diff(-q^x w,w) = -q^x
    diff(q',v) = I

In either case, since we are now updating our stationary point, R0, by concatenating *approximations of* rotation matrices, we had better ensure that the resulting matrix stays orthogonal during our program lifetime. There's a neat and simple trick for doing this, that I can show later.

But before then, you might wonder which one of the above you should use: axis-angle or Euler? And if you use Euler, which order should you use? There seems to be too many choices here, and trying them all will take time! But I'll let you in on a little secret: it doesn't matter!

Wait what?
----------
It turns out that for small angles, rotation matrices are actually commutative! This means that we really could have used any order we wanted in the above expression; they would all evaluate to roughly the same matrix. If you write out the matrix product Rz(ez)Ry(ey)Rx(ex), and replace the sines and cosines with the above approximations, you will get this:

    [   1,   ex*ey - ez, ey + ex*ez]
    [  ez, ex*ey*ez + 1, ey*ez - ex]
    [ -ey,           ex,          1]

Assuming ex,ey,ez are small, we'll drop everything but the first order terms to get:

    [   1, -ez,  ey ]
    [  ez,  1,  -ex ]
    [ -ey,  ex,  1  ]

And you will indeed get this same matrix no matter which order you apply the rotations. However, since the order appears to not matter, why should it matter if we use angle-axis over Euler angles? It turns out that it doesn't either! The matrix above is *exactly* the approximated angle-axis matrix for small rotations:

    (I + w^x) where w=(ex,ey,ez)

Now you might ask, when can I use this approximation? How small is 'small'? And how do I know if the incremental update I get from my optimization is compliant with that?

Here's a comparison for you where ex, ey and ez are each varied between -25 and +25 degrees. The red cube uses the Euler rotation R = Rz(rz)Ry(ry)Rx(rx). The green and the blue cubes are using the exact axis-angle rotation formula, (I + sin(|w|) K + (1-cos(|w|)) K^2 ), and its approximation, (I + w^x), respectively. The blue cube orthogonalizes the result to ensure that it remains a proper rotation matrix.

![](cv-why-not-euler-anim1.gif)

They look pretty similiar. But here's what happens if rz is linearly increased forever:

![](cv-why-not-euler-anim2.gif)

As you can see, all three approaches are pretty close for small angles, but for angles above 45 degrees or so the approximation starts to break down to the point where it fails to accomplish a full 90 degree rotation.

<!--
## Aside: add a damping term

One way to fix this is to add a damping term. In the example above, suppose we add a damping term along the diagonal:

         2 -1
    H = -1  2

Now the system Hx=b has a unique solution. Another way to fix it is to avoid the singular point, if you can, but that is not very robust! I mean, you would need to guarantee that your system *never* get close to that orientation. I'm sure that you could get away with it in certain use cases, and if you are certain, then go ahead, but I would rather have a solution that handles this nonetheless. -->

<!--
## Other questions
(Q) But do we actually care about that? How big *are* Gauss-Newton steps? In the end, aren't we just linearizing and approximating stuff *anyway*, since Gauss-Newton is a first order method? 25 degrees seems like a lot! I think we can just go ahead and use the (I + w^x) approximation.

(Q) What about the exponential map?

(A) The exponential map takes a vector w = (wx,wy,wz) and computes exp(w^x), which is exactly equal to the angle-axis rotation matrix (see wikipedia):

    R = exp(w^x) R0 = (I + sin(|w|)K + (1-cos(|w|))K^2)

(Q) Why would I use that over a local Euler parametrization?

(A) You don't have to! You can go ahead an use a local Euler parametrization, but you need to remember that the approximations we did in the above section do not hold for large angles. The advantage of the exponential map (or equivalently, the angle-axis matrix) is that computing the corresponding rotation matrix, without approximation, is cheaper than computing the Euler matrix, in that it contains fewer trigonometric operations and multiplications.

... But why is that an advantage? Again, back to the first question. I don't think it's really all that useful as people make it out to be, and I'd rather just use the theoretically simpler axis-angle approximation.

(Q) Why do we evaluate the derivative of the local rotation at zero?

(A) That is a choice that we make, whose consequences we have to deal with if it is a bad one. This is what sucks about nonlinear optimization problems. If it were linear, the derivative would be a constant, and would therefore hold equally well over our entire parameter space. But for nonlinear optimization, in a first-order method like Gauss-Newton, we need to make an assumption on how big a step can get, and keep in our heads the fact that this is an approximation that has a limited range where it holds well. This would affect the maximum step-size we try to take in, say, a line-search or a trust-region framework.

(Q) What is the derivative for the exponential map / axis-angle?

    q(w) = exp(w^x) Rp
    q(0) = exp(0) Rp = Rp := q
    diff(q,w)|0 = diff(w^x,w)|0 (exp(w^x))|0 Rp
                 = diff(w^x,w)|0 Rp
                 = diff(w^x q,w)|0
                 = diff(-q^x w, w)|0
                 = -q^x diff(w,w)|0
                 = -q^x I
                 = -q^x

    exp(w^x) = I + sin|w| K + (1-cos|w|) K^2
    -> Same derivative

For the approximation we get the same thing

    q(w) = (I + w^x) Rp
    q(0) = Rp := q
    diff(q,w)|0 = diff(w^x q,w)|0
                = diff(-q^x w,w)|0
                = -q^x

(Q) What about translation?

Translation is a nicer space than rotations, so I think we can get away with a global parametrization there. That means that our locally perturbed model, parametrized in terms of an incremental rotation w and an global translation T, is

    q(w,T) = exp(w^x) Rp + T

where R is the stationary orientation. After each optimization cycle we would then update the stationary rotation with R <- exp(w^x)R, while T would be updated by adding whatever increment we got from the Newton step. This is equivalent to saying:

    q(w,v) = exp(w^x) Rp + (T+v)

and doing T <- T+v after each iteration. In practice I guess you wouldn't add the entirety of w and v, though, and instead use step size control with line-search or trust-region optimization.

In some papers I see people doing this:

    q(w,v) = exp(w^x) Rp + T + v'
           = exp(w^x) (Rp + T) - exp(w^x) T + v'
           = exp(w^x) (Rp + T) + v

where the last step is allowed because the expression can be described by any three free variables, I guess? This trick is related to the exponential map for SE(3). After this foray into parametrizations, I'm not sure if this last form is actually all that more useful than the one above. I see it alot in papers, but people don't talk about the stuff like I've done here, maybe because it's so obvious to everyone, or maybe because people haven't really thought about it and just follow other people by example.

My opinion is that this last form is kind of nasty numerically, because the origin T affects the rotation, and you lose the ability to rotate around the body origin by adjusting w alone. From a numerical point of view this is bad, because you need to modify both w and v, by quite large amounts, in order to achieve a small rotation about the body origin.

I don't think this coupling between the origin and the body rotation makes much sense, and if you compute the derivatives by finite-differences, it will be kinda weird. I did an experiment with that once, and my solver had problems achieving what amounted to a small body-relative rotation, because it required adjusting several of the parameters by quite large amounts --- a motion which was not captured by finite-differences.

(Q) What is done in LSD?

(A) In LSD they do the same type of local parametrization around a stationary point that I described above. Their local parametrization uses the 4x4 exponential map, which, as I also described above, I am not particularly fond of. Mathematically, and this equation actually makes sense to me now, they write the current stationary point as $\zeta^k$, which includes both rotation and translation, and the local twist as $\delta$. The local pose parametrization is the composition of these $\delta \circ \zeta^k$, which corresponds to

    q(w,v) = exp(w^x) (Rp + T) + v

Their cost function consists of some residuals $r_k = r_k(\zeta)$, whose Jacobians are

$$
    J_k = {{\partial r_k(\delta \circ \zeta^{(k)})} \over {\partial \delta}} \quad \text{ evaluated at } \delta=0
$$

Which is the same as the stuff I wrote above, including, evaluating the derivative at zero.


(Q) What is done in DSO?

(A) In DSO, they refer to the current absolute pose estimate as $\zeta$, which apparently might be different from the linearization point, which they refer to as $\zeta_0$. They accumulate updates $x$, such that $\zeta = x \circ \zeta_0 = \exp(x) \zeta_0$. Their cost function is a sum over residuals $r_k = r_k(\zeta)$. For some reason, they linearize their residuals with respect to an additive increment of $x$:

$$
    J_k = {{\partial r_k((\delta + x)\circ \zeta_0)} \over {\partial \delta}}
$$

why not do

$$
    J_k = {{\partial r_k(\delta \circ \zeta_0)} \over {\partial \delta}}
$$

Why do they accumulate instead of updating $\zeta_0$ in each iteration?
 -->
