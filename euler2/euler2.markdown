# Stepping through rotations: Part II

<!--
1. Fixing gimbal lock with switching models
2. Fixing gimbal lock with absolute matrix and relative euler angles
3. Reducing computational cost
4. But which order should you use? I arbitrarily chose xyz!
5. Well it doesn't matter
    Local euler angle is identical to any other local euler angle ordering. Looking at it a different way, rotations commute (it doesn't matter which order you rotate in).

    In fact, let's look at another parametrization: axis-angle.
    Huh, it's the same!!
6. Isn't that neat?
    It's like there's a sort of "canonical" small step
    In fact there is, and mathematicians gave a name to this discovery
    The so3 lie algebra
    We describe the "small step" in terms of three numbers. We've called them Euler angles and axis-angle. But they're exactly the same.
    ...?
    What are these three numbers?
    And what is this weird looking matrix they're placed inside?
    Exponential map
7. I'm going to leave you with some questions

    What makes a small step in 'translation space' so much easier than a small step in 'rotation space'?
    Why do translation vectors commute, but rotations do not?

    "Rotations do not live in a vector space" [barfoot 6.2.5]

    "There are many ways of representing rotations mathematically, including matrices, axis-angle, Euler, unit-length quaternions. The most important fact to remember is that all these representations have the samy underlying rotation, which only has three degrees of freedom. A 3x3 rotation matrix has nine elements, but only three are independent. Unit-length quaternions have four parameters, but only three are independent (normalization)."

    What we are really after here is to linearize rotation: translation is already linear, so we can add a small translation and get a new translation. We can also find the midpoint between two translations by subtracting one from the other and dividing by two. We can't do that with rotation matrices.

    "The fact that rotations do not live in a vector space is fundamental when it comes to linearizing motion"

    Can we generalize these statements somehow to other mathematical things? Yes: Lie groups and Lie algebras. While SO3 is not a vector space, it is a matrix Lie group. And there are other matrix Lie groups. And they all share some properties, and can be treated in similar ways.

    Based on our discussion so far, one way you can begin to intuit Lie algebras, is that they encode what it means to take a small step. Knowing this encoding lets you do the tricks you're familiar with from vector spaces: you can interpolate between rotations, you can add or subtract rotations, you can take small steps.

    RVC Chapter 2.3
    Barfoot Chapter 6.2.5
    Barfoot Chapter 7
    Barfoot Chapter 7.1.9: specific to optimization
        (page 241 "A cleaner ways to carry out optimization is to find an update for C in the form of a small rotation on the left, rather than directly on the Lie algebra rotation vector representing C")
        (I just decided to write 3000 words about one sentence: "... which has singularities associated with it")

Q) Existence of local minima. Global uniqueness. (page 307). Dips within humps.
Q) What about global optimization? Point cloud alignment. Closed-form solution for rotation matrix. But our problem involves perspective projection with lens distortion, which requires iterative?
Q) Interpolation can be important, because you might want to do a line search? (page 248). Or do you? What does it mean to 'continue' in a rotation?
Q) Treating translation seperately from rotation (still as a vector space) is more beneficial I think. The se3 looks unnatural and makes it harder for optimization to achieve certain motions
    Instead of having x y z correspond to translation and rx ry rz correspond to rotation, each along an intuitive axis, you need to do a weird mixture of perturbations, sometimes large, just to achieve motion along one axis.

    Unwanted coupling: you don't want to have to rotate in order to translate.

-->

The take-home message from last time was that Euler angles can *gimbal lock*, whereby you lose the ability to rotate around all three axes: adjusting any angle in isolation can only generate two distinct motions, instead of the three you started with. This could cause gradient descent, or similar optimization strategies, to slow to a stop, or adjust the wrong parameters.

...

Another way of saying it is: Euler angles suck at keeping track of absolute orientation. See, when I made this textured 3D box, I inadvertently chose its "default" orientation (all angles zero) to be with its cover facing the camera, like so:

![](model3.png)

This happens to have an impact on gimbal lock: for this choice, we have all three degrees of freedom when the cover is facing the camera, but not when we turn the book sideways. On the other hand, if the default orientation had been sideways....

![](model4.png)

we would have three degrees of freedom at the sideways orientation, but *not* when the cover is facing the camera.

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
</style>

In other words, Euler angles suck at keeping track of absolute orientation, but they are pretty good when kept close to zero...

## The Tumbler

3D modelling software have tackled similar problems for a long time: how can the user, with their 2D mouse interface, rotate an object in 3D? One solution is called the *Tumbler*. It is notoriously unintuitive and the only excuse you get for using it is [not knowing any better](todo: matt keeter). It works like this:

When you click and start dragging, the Euler angles start from zero and you can rotate the thing around its current orientation. When you release, the orientation is saved but the Euler angles are reset to zero.

<div class="slider-wrap">
    <div class="slider" id="slider4" style="max-width:240px;max-height:260px;">
        <div style="width:1700px;"><img src="gimbals1.png"/><img src="gimbals1-2.png"/><img src="gimbals2.png"/><img src="gimbals3.png"/><img src="gimbals3-4.png"/><img src="gimbals4.png"/><img src="gimbals5.png"/></div>
    </div>
    <br>
    <input type="range" min=0 max=6 step=1 value=0 oninput="document.getElementById('slider4').scrollLeft = this.value*240;"></input>
    <label>Click and drag</label>
</div>

In other words, the coordinate frame you rotate around follows the object while you're rotating it, but as soon as you release, the frame resets. No matter where you start rotating, you can theoretically generate all three distinct rotations:

<div class="slider-wrap">
    <div class="slider" id="slider1" style="max-width:160px;max-height:180px;">
        <div style="width:700px;"><img src="x1.png"/><img src="x2.png"/><img src="x3.png"/></div>
    </div>
    <div class="slider" id="slider2" style="max-width:160px;max-height:180px;">
        <div style="width:700px;"><img src="y1.png"/><img src="x2.png"/><img src="y3.png"/></div>
    </div>
    <div class="slider" id="slider3" style="max-width:160px;max-height:180px;">
        <div style="width:700px;"><img src="z1.png"/><img src="x2.png"/><img src="z3.png"/></div>
    </div>
    <br>
    <input type="range" min=0 max=2 step=1 value=0 oninput="document.getElementById('slider1').scrollLeft = this.value*160;"></input>
    <label>rotate x</label>
    <br>
    <input type="range" min=0 max=2 step=1 value=0 oninput="document.getElementById('slider2').scrollLeft = this.value*160;"></input>
    <label>rotate y</label>
    <br>
    <input type="range" min=0 max=2 step=1 value=0 oninput="document.getElementById('slider3').scrollLeft = this.value*160;"></input>
    <label>rotate z</label>
</div>

It turns out that this is a **terrible** user interface, because, even though the object can theoretically be rotated in three distinct ways, the mouse's lack of a third dimension prevents the user from accessing more than two of those.

<p style="color:#999;">Particularly, the Tumbler lets the user control x-rotation by moving the mouse vertically, and y-rotation by moving the mouse horizontally. Whenever the user wants to rotate about z, they end up spinning their mouse like a methodic lunatic&mdash;a motion that gave the widget its name.</p>

For us, however, it is an ideal solution, because whereever the user starts a click-drag rotation, the object can rotate

1. We start out with the book cover facing us: `R = identity`.
2. We solve for the gradient descent direction, which gives us three delta Euler angles and a delta translation. But instead of accumulating the deltas into three absolute Euler angles, we apply the rotation they represent to the current rotation matrix: `R = euler(rx,ry,rz)*R`.
3. We then repeat and use the updated matrix as the default orientation for the next step, essentially "resetting" the Euler angles to zero.

One step of gradient descent is like one click-drag-release movement with the Tumbler.

How does this prevent gimbal lock? When we computed the gradient last time, we added or subtracted a delta around our absolute Euler angles, like so:

    dedrx = (E(euler(rx+drx,ry,rz), T) -
             E(euler(rx-drx,ry,rz), T)) / 2drx ...

But now we can compute the gradient by adding or subtracting a small delta around zero, and applying that to the current rotation matrix:

    dedrx = (E(euler(+drx,0,0) * R, T) -
             E(euler(-drx,0,0) * R, T)) / 2drx ...

Whether we use finite differences, automatic differentiation or analytic derivatives, the Euler matrix on the left always has three degrees of freedom, because it's based around the origin.

<p style="color:#999;">
We could also use unit-length quaternions to track orientation. They are often preferred because they use fewer bytes than rotation matrices and, like rotation matrices, they do not gimbal lock. But they also have constraints to keep them valid (must be unit-length), so we can't freely adjust its parameters to find a direction for gradient descent.
</p>

Upon closer inspection
----------------------

<!-- *Satisfied with your progress you decide to call it a day. You get ready to head home in eager anticipation of sipping some of that fancy tea you bought yesterday but didn't have the chance to try. After turning off your monitor&mdash;because you care about saving power and nothing irks you more than seeing your coworker leaving theirs on (again!)&mdash;as you finish tossing the last of your belongings into your bag, your mind starts to wander...* -->

Why did we choose that particular Euler angle ordering? Are there better orderings? I've heard that quaternions are super popular and useful, maybe they could help? And what about that weird thing on wikipedia... the *exponential map*? That was just confusing...

It turns out that there's a nice answer connecting all these questions and ideas together. To show you what I mean, let's look at a different Euler angle order.

There are many possible Euler angle variants, XYX, XZX, YXY. Some of them are popular, others will make people look at you like you're an excentric mad person (YXY... seriously?). Nevertheless, the number of choices might make you worry that the one we chose is not as good as another one.

Here are two different variants, XYZ and ZYX. As you drag the sliders and rotate the two, you realize that they look vastly different, for the same values.

<!-- todo: gizmo one, big angles -->
<!-- todo: not just adjusting any single angle in isolation -->

But looking more closely around the area that we're interested in, that of 'small angles'.

<!-- todo: gizmo two, small angles -->

Huh, within this 5-10 degree range, you almost can't tell them apart!

Small rotations
---------------

It turns out that for small angles, the order you apply rotations doesn't matter. We can see this mathematically. If we write out the matrix product `Rz(z)Ry(y)Rx(x)`, we get this nasty fellow:

    | cy*cz   cz*sx*sy - cx*sz   sx*sz + cx*cz*sy |
    | cy*sz   cx*cz + sx*sy*sz   cx*sy*sz - cz*sx |
    |   -sy              cy*sx              cx*cy |

<p style="color:#999;">(I wrote `cx` and `sx` instead of `cos(x)` and `sin(x)` and so on, for easier reading)</p>

For small angles, `cos(x) = 1` and `sin(x) = x`. So within reasonable approximation, the above is equal to this:

    |  1      x*y - z    x*z + y |
    |  z    x*y*z + 1    y*z - x |
    | -y            x          1 |

Moreover, the product of two small numbers of them becomes really small compared to any one of them alone, so we can simplify again:

    |  1   -z    y |
    |  z    1   -x |
    | -y    x    1 |

I'll leave it to your curiosity to check that no matter what Euler angle order you consider, they are all (approximately) equal to this when you plug in small angles. So when we write

    // update current rotation with offset from gradient descent
    R = euler(rx,ry,rz)*R

it doesn't really matter what ordering we choose&mdash;they all pretty much have the same effect, as long as `rx,ry,rz` are small.

Axis-angle
----------

Euler angles are not the only minimal representation. Axis-angle is another popular one. For fun, let's take a look at what happens to it when the angle is small.

Euler angles concatenate three rotations about three axes, but we can also parametrize our rotation in terms of an axis `r` and an angle `a` around it. There's a formula to convert this to a rotation matrix (copied from Wikipedia):

    R = I + sin(a) skew(r) + (1-cos(a)) skew(r)^2

<p style="color:#999;">We'll see what this `skew` function is in a bit...</p>

This is not a minimal parametrization because it has four numbers, the constraint being that the axis must be unit-length. But if we multiply the angle into the axis we do get a minimal parametrization: a vector whose length is the original angle and direction is the original axis.

    R = I + sin(|w|) skew(w/|w|) + (1-cos(|w|)) skew(w/|w|)^2

So what happens when the angle is small? Well, stuff becomes zero and we're left with the identity plus this `skew` thing.

    R = I + skew(w)

`skew(w)` is the skew-symmetric form of `w`. What's that? Well if `w = (x,y,z)`, wikipedia tells us that `S(w)` is

    |  0   -z    y |
    |  z    0   -x |
    | -y    x    0 |

And what do you know, if we add the identity to that, we get the exact same matrix as before. Weird!

<!-- this ties into physics. skew(w)*R is like taking the cross product between w and each axis of R. Remember from physics that the cross product of angular velocity with a vector points in the direction that vector moves. So this w is like an angular velocity, and skew(w)*R is how each axis changes. -->

So many choices.... but does it matter?
---------------------------------------
Now you might ask, when can I use this approximation? How small is 'small'? And how do I know if the incremental update I get from my optimization is compliant with that?

Here's a comparison for you where ex, ey and ez are each varied between -25 and +25 degrees. The red cube uses the Euler rotation R = Rz(rz)Ry(ry)Rx(rx). The green and the blue cubes are using the exact axis-angle rotation formula, (I + sin(|w|) K + (1-cos(|w|)) K^2 ), and its approximation, (I + w^x), respectively. The blue cube orthogonalizes the result to ensure that it remains a proper rotation matrix.

<!-- ![](cv-why-not-euler-anim1.gif) -->

They look pretty similiar. But here's what happens if rz is linearly increased forever:

<!-- ![](cv-why-not-euler-anim2.gif) -->

As you can see, all three approaches are pretty close for small angles, but for angles above 45 degrees or so the approximation starts to break down to the point where it fails to accomplish a full 90 degree rotation.

<!-- related: line search. what does it mean to rotate along a line in rotation space? -->
<!-- related: this is mitigated by the fact that we do multiple iterations of optimization. So we don't expect to get to the solution in one step anyway. Understepping toward the solution is mitigated by taking another step. -->

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

Aside: Normalization
--------------------

While gimbal lock is not one of them, tracking orientation with a rotation matrix, rather than Euler angles, does not come without its own set of issues. In particular, you might be worried by this piece of code that 'accumulates' a rotation to the current one:

    R = euler(rx,ry,rz)*R

If you are an expert in floating point numbers you probably know that they are not perfect realizations of the real numbers: for example, `0.1+0.1` is not `0.2`, but `0.20000000298023224`.

Imperfections like these can be a concern when you deal with rotation matrices (or quaternions) over longer periods of time&sup1;, in the sense that repeatedly appending rotations will accumulate errors and cause the matrix (or quaternion) to stray from a valid rotation: the columns are no longer unit-length and pair-wise perpendicular, and objects appear slightly deformed after rotation.

<p style="color:#999;">
&sup1;*Time* in the literal sense too: even if you don't touch that rotation matrix, you might want to check up on it from time to time if your hardware is operating in high-radiation environments (like space or a nuclear reactor), because an occasional bit-flip is a real risk.
</p>

If you use quaternions you can renormalize it: compute the length and divide by it. The analog for rotation matrices is called orthogonalization. You can find many ways to orthogonalize a matrix, depending on if you want higher accuracy or simpler code. Here's a simple one, used in Peter Corke's [Robotics, Vision and Control](todo: link to website) toolbox:

Given a slightly messed-up rotation matrix `R` with columns `(x, y, z)`
1. Pick an axis that you assume is in the correct direction (say `x`)

    `x' = x`

2. Pick another axis (say `y`) and create a third axis perpendicular to those two

    `z' = cross(x', y)`

3. But, because `x` and `y` may not have been perpendicular, make another axis perpendicular to the first one and the one you just made

    `y' = cross(z', x')`

4. Finally, normalize each axis to unit-length.

The fixed rotation matrix is then formed by the columns `(x', y', z')`. Another simple one can be found in Direction Cosine Matrix IMU: Theory by William Premerlani and Paul Bizard (paywalled, but you can find the procedure reprinted on StackOverflow [here](todo:)).

Aside: Reducing computational cost
----------------------------------

So far I've kept an aggressively *positive* attitude towards laziness and inefficiency. But sometimes, like when your algorithm takes an entire day to run, you don't need to write code faster, you need to write faster code. So let's break down our solution in terms of what the computer has to do.

<pre style="margin:0 8px -8px 0;float:left;width:41%;"><code style="font-size:60%;">update_parameters(R, T):
  dedrx = (E(euler(+drx,0,0)*R, T) -
          E(euler(-drx,0,0)*R, T))/2drx
  dedry = (E(euler(0,+dry,0)*R, T) -
          E(euler(0,-dry,0)*R, T))/2dry
  dedrz = (E(euler(0,0,+drz)*R, T) -
          E(euler(0,0,-drz)*R, T))/2drz
  dedtx = (E(R, T+[dtx,0,0]) -
          E(R, T-[dtx,0,0]))/2dtx
  dedty = (E(R, T+[0,dty,0]) -
          E(R, T-[0,dty,0]))/2dty
  dedtz = (E(R, T+[0,0,dtz]) -
          E(R, T-[0,0,dtz]))/2dtz

  rx = -gain*dedrx
  ry = -gain*dedry
  rz = -gain*dedrz
  R = euler(rx,ry,rz)*R
  T -= gain*[dedtx, dedty, dedtz]
</code></pre>

Maybe most prominently, each optimization step will evaluate the error function E twelve times. In our toy example this is not a big deal because it was a loop over five-or-so 2D-3D correspondences with not a lot of work each. But let's look at a real example...

*Direct Sparse Odometry* is an algorithm designed to track camera motion. At its core, what it does is the same as in our book problem, but instead of a book, they have a 3D model of the world. Similar to how we try to find how the book is positioned in a photo, they try to find how the world moved between frames (assuming it is caused by the camera moving).

![](dso.jpg)

<p style="max-width:500px;margin:0 auto;color:#999;">
A figure from Direct Sparse Odometry paper showing color-coded depth maps. In addition to estimating the camera pose over time, they also estimate the depth maps themselves, in a process called bundle adjustment (called so because it adjusts the entire bundle of parameters: points *and* cameras.)
</p>

Just like we use an error function to judge how good our alignment was, they use an error function to judge how well the world model fits with the photo, at a given translation and rotation.

There's one key difference though: our book model had five points, but their model can have as many points as there are pixels in an image. Each of these don't involve that much work: a camera projection, and a subtraction of two pixel patches. But it's a lot of work for an inner loop over 100k+ pixels, especially if the loop runs twelve times over!

This means that they have to do some tricks to speed things up.

 ## Ways to optimize our optimization

 Our book model had five points, but their model can have as many points as there are pixels in an image. Looping over all of those can be prohibitively slow if done twelve times per step, even if the work done per point is small&mdash;as a reminder, this is what our error function looks like:

     E(R, T):
         e = 0
         for u,v,p in patches
             u_est,v_est = camera_projection(R*p + T)
             du = u_est-u
             dv = v_est-v
             e += du*du + dv*dv
         return e / num_patches

 The first transformation we can make is to exploit the fact that the derivative of a sum is the sum of derivatives: instead of calling E twelve times, we can call it once and return all derivatives along with it. Something like this:

     E(R, T):
         e = 0, dedrx = 0, dedry = 0, ... dedtz = 0
         for u,v,p in patches
             // something...
         return [e, dedrx, dedry, ..., dedtz] / num_patches

 That could be faster? We could differentiate each term in the sum as we did for the sum itself, using finite differences, or maybe we want to use a library with automatic differentiation? Maybe we want analytic derivatives this time? In fact, maybe we shouldn't rewrite this error function at all. Maybe we've SIMD-optimized it so even if we do call it twelve times it's faster than the alternatives?

 <!-- Maybe you are using a different optimization method, like Gauss-Newton or Levenberg-Marquardt, in which case you may want to ... -->

 <!-- <p style="text-align:center;font-size:300%;">...</p> -->

 <p style="color:#999;">
 To be honest, the premise of this section is a cover story. I'm using it as an excuse to take you on a wild mathematical tangent, but you can use it as a rope, if you want, to reel yourself back into the world of practicality (or just tug on it to reassure yourself that it still exists), if you lose your way inside the abstract manifolds of rotation space.
 </p>

 There are many alternative routes to go; which one is best depends on your specific problem.

     u_est,v_est = camera_projection(R*p + T)
     du = u_est-u
     dv = v_est-v
     e += du*du + dv*dv

 u and v are constant (with respect to our parameters, not the loop).

     2*u_est*diff(u_est,[R,T]) + 2*v_est*diff(v_est,[R,T])

 u_est and v_est are some functions f1 and f2:

     u_est = f1(R*p + T)
     v_est = f2(R*p + T)

 Let's go on a mathematical detour and see what the latter routes would involve (automatic or analytic).

     euler = Rz(ez)Ry(ey)Rx(ex)

 Could do FD, but that can be slow.

 Automatic differentiation or analytic derivatives both involve a bunch of cos and sin.

 Can mix FD with AD, chain rule.

 But we have additional knowledge: euler angles close to zero.

 For small values of x: `cos(x) = 1` and `sin(x) = x`.

 What's a small value? It's almost wasteful to use the full trig terms when the difference is so small!

 So we can replace our `euler` function with this matrix, bearing in mind that it's only valid for small values.
