Local rotation parametrizations in optimization
===============================================
<!-- <h2 style="border-bottom: none; text-align: center; font-weight: normal;"><i>Problems that can occur when estimating 3D rotation using gradient-based optimization and how to deal with them.</i></h2> -->

<!-- Euler angles, quaternions, axis-angle, the exponential and logarithm maps; there are many ways to describe how a thing is rotated in 3D space, and you may have heard of some of these. -->

**"If all you have is a hammer, everything looks like a nail."**

A familiar proverb that, as noted by Chris Hecker in his [GDC talk](), has an unappreciated cousin:

**"If you can turn anything into a nail, all you need is a hammer."**

Chris Hecker's realization was that *numerical optimization* can be used to solve *a lot* of different problems in the same way, making it a sort of hammer in your maths toolbox. Consequently, if you have a library or program (like MATLAB) that can do optimization, you can solve any problem that you can wrangle into the right form, all using the same tool.

With increasing computer power and a desire for rapid iteration, having a hammer readily available can be of great value, even if it doesn't solve the problem as cleanly or as run-time-efficiently as it could, because it can save you a lot of time as a programmer.

I have since learned that optimization is a powerful technique that, when put in the right hands in the right problem domain, can solve useful problems.

I have also learned that 3D rotations can cause a lot of tears.

Estimating rotations
--------------------

<!-- todo: image PSO estimate rotation and translation of bunny -->
<!-- todo: image estimate rotation and translation of camera -->

A common problem in computer vision is finding the 3D rotation and translation (a *pose*) of either an object in an image or of the camera itself.

For example, if you're trying to make a [quadcopter land on a robotic vacuum cleaner](todo: iarc) using a downward facing camera, part of that problem is finding where the robot is relative to you, so you can move to the right position and orientation.

![](lander.gif)

Equivalently, you could find where you are relative to the robot. For the mathematics below it doesn't matter if we consider the pose of an object relative to the camera, or the camera relative to the object; converting from one to the other is just a matter of inverting the result.

Another example, if you want recover a 3D model of the scene from multiple images, part of the problem is finding the rotation and translation between the cameras, so you can find the depth of pixels by triangulating them (intersecting rays).

![](sfm.jpg)
<p style="text-align:center;color:#999;">Source: *Multi-View Stereo: A Tutorial.*</p>

Triangulation is part of stuff like photogrammetry, structure from motion or multi-view stereo: the study of recovering 3D models from images. A related problem is tracking the pose of a camera as it moves through the world, sometimes called visual odometry.

Many computer vision algorithms use optimization to estimate the pose (3D rotation and translation) of either an object in an image, or the camera itself. These problems need a *parametrization* of rotation in order to define the aforementioned cost function.

One such parametrization is the *rotation matrix*: a 3x3 matrix of mutually perpendicular and unit length columns. This is not a nice parametrization, because not all 3x3 matrices are valid rotation matrices. So if you, say, wanted to generate a random rotation, you could not just sample 9 numbers and put them in a matrix.

For example, some optimization methods, like *particle swarm optimization*, try to find the optimal parameters by evaluating the cost function at many (pseudorandom) locations in the parameter space, and share information between samples to pinpoint the location of the minimum.

These methods work very well with Euler angles because, unlike the methods I describe below, they do not need to compute the derivative of the cost function.

Gradient-based methods, like the first order Gauss-Newton method, try find the optimal parameters by iteratively solving a *linear* least squares problem, which involves taking the derivative of the cost function with respect to the pose parameters. It turns out that this opens a can of worms when your parameters involve rotation.

What is gimbal lock and why should I care?
------------------------------------------
Let's say that we want to estimate the pose of a camera against a calibration checkerboard. We'll use the Gauss-Newton algorithm. Our first attempt uses Euler angles to parametrize the rotation, and a translation vector to describe the checkerboard's origin. A point p in the checkerboard transformed into camera space, and projected into the image, is therefore given by:

    R = Rz(rz)Ry(ry)Rx(rx)
    T = (tx, ty, tz)
    u,v = project(R*p + t)

During optimization, we would then compute the derivative of our cost function with respect to these coordinates $(r_x,r_y,r_z,t_x,t_y,t_z)$, and update them directly using a Gauss-Newton step direction. To illustrate the problem that can occur here, consider a plate of four points, representing our checkerboard model.

    o----o     ^ Y
    |    |     |
    |    |     |
    o----o     +-----> X

In this diagram we are looking at the plate head-on. The x-axis is to the right, the y-axis goes up, and the z-axis goes out of the screen.  Now suppose the object is rotated 90 degrees about the y-axis, that is, it is oriented at $R_z(0)R_y(90^\circ)R_x(0)$. Then it looks like this

    o
    |
    |
    o

If we adjust $r_x$ so that the final orientation is $R_z(0)R_y(90^\circ)R_x(45^\circ)$, the plate will look like this

       o
      /
     /
    o

But if we adjust $r_z$ by the same amount in the opposite direction, so that the final orientation is $R_z(-45^\circ)R_y(90^\circ)R_x(0)$, the plate will end up in exactly the same orientation.

This phenomenom is called *gimbal lock*. Initially, at (0,0,0), we could rotate freely about all three axes, but if $r_y=90^\circ$ we can only really rotate in two, atleast infinitesimally. In other words, we have lost a degree of freedom. Here's an animation that shows the gimbal lock effect.

![](cv-why-not-euler-anim0.gif)

For the red plate I am adjusting rx and keeping rz fixed, for the green plate I am adjusting rz in the opposite direction and keeping rx fixed. I repeat this adjustment while adjusting ry for both plates toward 90 degrees, at which point they end up producing the same motion!

So why is that a problem?
-------------------------
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

Alright, so what should I do?
-----------------------------
The above should have convinced you that parametrizing your orientation in terms of global Euler angles can lead you to situations where the numerical properties of your optimization problem can cause problems. One way to fix this is to add a damping term. In the example above, suppose we add a damping term along the diagonal:

         2 -1
    H = -1  2

Now the system Hx=b has a unique solution. Another way to fix it is to avoid the singular point, if you can, but that is not very robust! I mean, you would need to guarantee that your system *never* get close to that orientation. I'm sure that you could get away with it in certain use cases, and if you are certain, then go ahead, but I would rather have a solution that handles this nonetheless.

A third way to get around this is to use a different parametrization. let's look at that in closer detail!

Localized Euler angle parametrization
-------------------------------------
What we did above was to parametrize our pose estimate with a set of minimal global parameters (absolute Euler angles). During optimization we would then update these directly, using the Gauss-Newton step. Another approach, if we still want to use Euler angles, is to optimize with respect to *local* Euler angles, that are afterwards used to update our *global* Euler angles. We can do this by concatenating two rotations: a local rotation, that is expressed with our optimization variables, and the global rotation, that stays constant during an optimization iteration. In other words, we parametrize an incremental rotation around a stationary orientation:

    R = Rz(ez)Ry(ey)Rx(ex) R0

The key that will make this work is to keep our local coordinates small, because if they were allowed to roam freely we might end up close to the singularity again. We avoid this problem by iteratively updating the stationary orientation after every time we solve for a Gauss-Newton step. R0 is treated as constant during each optimization iteration, and would be updated by the above equation successively after each iteration. In other words, each time we formulate our optimization problem we only search for a small perturbation away from zero, and reset the 'zero'-point after each iteration.

If you try to visualize what happens here in our plate example, you will discover that even if R0 is located at the singular orientation, the left-hand matrix allows us to rotate the plate about any of the three axes, thereby recovering our lost degree of freedom! The following animation tries to illustrate this:

![](cv-why-not-euler-anim3.gif)

The red plate is adjusting ez, the green is adjusting ey and the blue is adjusting ex. As you can see, even though the nominal rotation is located at the singularity, we can rotate about all three axes. This ought to not be very surprising, since this is equivalent to just multiplying all your points with a constant rotation matrix R, and then doing a standard Euler rotation on the result. Again, this will have gimbal lock problems as soon as ey gets close to 90 degrees, but we're avoiding that by keeping our local parameters small!

Reducing computational cost
---------------------------
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

Other questions
---------------
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
