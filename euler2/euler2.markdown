# The space of rotations: Taking small steps

<!-- 1. INTRO. Getting around the problem with local Euler angles: store the current orientation and rotate around that. It's natural that it would work, considering how we could equivalently have pre-rotated our 3D model. Euler angles give you most rotational expressivity at the origin.

2. BODY. But which ordering should I choose? Well it doesn't matter. In fact, infinitesimally, there is a sort of "canonical" small step. Local Euler angle approach is identical to any other local Euler angle ordering, axis-angle and the exponential map.
    Infinitesimally, rotations commute (it doesn't matter which order you rotate in). Let's dig in why... second order terms cancel.

3. CONCLUSION. Rotations are weird. Curious? Read barfoot, lie group and lie algebra.
    Global optimization, solving for rotation matrices. Existence of local minima. Global uniqueness. -->

The take-home message from the last example was that Euler angles can  gimbal lock, whereby you lose the ability to rotate around all three axes. Instead, near gimbal lock, adjusting any single angle in isolation can only ever give you two distinct motions, instead of the three you started with.

This can cause gradient descent to slow to a stop, or, adjust the wrong parameters because they are the only ones that reduce the error. The reason for this is that gradient descent looks at how making small changes to any single angle affects the error, so the direction toward the solution has to be obtained in terms of the local motions you can produce, which is not always the case.

But a solution to this problem can be found right where we started.

Fixing gimbal lock with local Euler angles
------------------------------------------

When I made this textured 3D box, I subconsciously chose its "default" orientation (all angles set to zero) to be with its cover facing the camera, like so:

<!-- todo: 3d model+axes seen from tilted angle with camera as well? -->
<img style="max-width:240px;" src="../euler/model.png"/>

Although I made the choice without thinking, it happens to matter when we consider gimbal lock: we still have all three degrees of freedom around the zero orientation, but problems arise as we turn the book sideways.

<!-- todo: three slider boxes. one slider per box. x,y,z. -->
<!-- ![](../euler/plates1xyz.png) -->

On the other hand, if I had chosen the default orientation to be sideways, we get all three degrees of freedom around the sideways orientation, and problems arise as we turn the book cover facing the camera.

<!-- todo: 3d model+axes seen from tilted angle with camera as well? -->
<img style="max-width:240px;" src="../euler/model.png"/>

If, in the last example, the default orientation had been sideways, we would have had no problems tilting the book backward, as we still have all three degrees of freedom at the zero orientation.

![](../euler/sideways45.png)

So you could imagine fixing the issue by changing the model itself, to have a different default orientation, based on what orientation you're currently estimating around.

If you're around (0,0,0), you use the model with its cover facing the camera. But as you get close enough to (0, 90, 0), you switch the model to the one seen from the side.

Of course, you don't need to actually store seperate 3D models for each default orientation, since the only difference between them is a constant rotation matrix pre-applied to the 3D coordinates.
<!-- the textures and vertices themselves stay the same. -->

In code, this means the final rotation matrix would be calculated in one way among a number of branches:

    if default orientation a:
        R = Rz(rz)*Ry(ry)*Rx(rx) * Ra
    if default orientation b:
        R = Rz(rz)*Ry(ry)*Rx(rx) * Rb
    ...

<!-- This is what they do on aircraft? -->

This would be well and good, except that our Euler angle estimates would need to change as well: if our current estimate is close to sideways, or (0, 90, 0), and we switch model so that (0,0,0) means sideways, then our angles have to go back to (0,0,0) again.

This sounds complicated and not very nice to implement...

## Absolute and relative rotations

The problem with the above strategy is that we don't actually address the issue, which is that Euler angles suck at keeping track of absolute orientation. Any choice of Euler angles will, at some orientation away from zero, have weird nonintuitive properties.

In other words, Euler angles are best when kept close to the origin.

An alternative is

We keep track of the absolute orientation as a rotation matrix.

During one optimization step we use the current estimated absolute orientation as the 'default orientation', and look for a small incremental rotation around that.

![](cv-why-not-euler-anim3.gif)

    R = Rz(rz)*Ry(ry)*Rx(rx) * R0

After one step (of taking finite differences and finding the small increments to each parameter) we update R0 with the above, giving a new stationary point for the next optimization step.

In other words we only use Euler angles inside one optimization step.

Because gradient descent looks for small increments to the parameters, the Euler angles are always kept close to zero.

    R = Rz(drz)*Ry(0)*Rx(0) * R0
    R = Rz(-drz)*Ry(0)*Rx(0) * R0

    R = Rz(0)*Ry(dry)*Rx(0) * R0
    R = Rz(0)*Ry(-dry)*Rx(0) * R0

    R = Rz(0)*Ry(0)*Rx(drx) * R0
    R = Rz(0)*Ry(0)*Rx(-drx) * R0


## Alternative explanation (mention problem of jumps later...)

Of course, you don't need to actually store seperate 3D models, each one with a different default orientation, since the only difference between them is a constant rotation matrix: the textures and vertices themselves stay the same.

Let's make this happen. Recall our error function:

    measure_quality(matrix3x3 R, vector3 T):
        e = 0
        for u,v,p in patches
            u_est,v_est = camera_projection(R*p + T)
            du = u_est-u
            dv = v_est-v
            e += du*du + dv*dv
        return e / num_patches

The line of interest is the one where we transform and project the 3D model coordinate p. Changing the model's default orientation is the same as applying a constant rotation matrix to p, before applying any of the other transformations:

    u_est,v_est = camera_projection(R*R0*p + T)

Both R and R0 are variable rotations, in the sense that their values change as we update our Euler angle estimates: R is directly a function of the Euler angles and is always changing, while R0 is changed on a need-basis (if we are sufficiently close to a different orientation).

But R0 isn't really a parameter we want to estimate, it's just a *fix* to let us rotate more freely, so we treat R0 as constant when take the derivative and stuff.

<!-- Oh but wait, doesn't this mean that the Euler angles will jump when we change model? -->

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
The above is fine if you're doing finite differences. But if you want analytic derivatives, or you're doing automatic differentiation, you'll find it to be kinda computationally nasty&mdash;with all those cosines and sines. However, with our assumption that the optimization parameters remain small, we can make some useful approximations.

For small values of x: $\cos(x) \approx 1$ and $\sin(x) \approx x$, so we can replace all those nasty trigonmetric functions in the local Euler matrix with linear expressions.

<!-- todo? -->

We could also parametrize our local rotation in terms of an angle-axis rotation:

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
