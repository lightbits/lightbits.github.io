# The space of rotations: Taking small steps

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

The take-home message from the last example was that Euler angles can  gimbal lock, whereby you lose the ability to rotate around all three axes: adjusting any angle in isolation could only generate two distinct motions, instead of the three you started with.

This can cause gradient descent to slow to a stop, or, adjust the wrong parameters. This is because gradient descent looks at how making small changes to any single angle affects the error, so the direction toward the solution has to be obtainable in terms of the local motions you can generate, which is not always the case.

In this part I'll take you on a wild mathematical tangent into abstract rotation spaces, under the pretense of finding a solution to this problem...

Fixing gimbal lock with local Euler angles
------------------------------------------

When I made this textured 3D box, I subconsciously chose its "default" orientation (all angles set to zero) to be with its cover facing the camera, like so:

![](model3.png)

I made this choice arbitrarily, but it happens to matter when we consider gimbal lock. The reason is that we have all three degrees of freedom when the book is facing the camera, but not when we turn the book sideways. On the other hand, if the default orientation had been sideways....

![](model4.png)

we would *not* have three degrees when the book is facing the camera, but instead at the sideways orientation. As you can verify for yourself, manipulating any one of the Euler angles can produce one of three distinct motions:

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

<!-- So in the last example, if the default orientation had been sideways, gradient descent would have had no problems tilting the book backward slightly. -->

So you could imagine a fix where we change the model to have a different default orientation, based on what orientation we're currently estimating around: If we're close to facing the cover, we use the model with its cover facing the camera. But as we get close enough to sideways, we switch to the one seen from the side.

Of course, we don't need to actually store seperate 3D models for each default orientation, since the only difference between them is a constant rotation matrix pre-multiplied to the 3D coordinates in the original model. In other words, we can get by with a bunch of `if`-statements, computing the book's orientation in one way or another based on which default orientation is closest:

    if default orientation a:
        R = Rz(rz)*Ry(ry)*Rx(rx) * Ra
    if default orientation b:
        R = Rz(rz)*Ry(ry)*Rx(rx) * Rb

<!-- todo: is this done on aircraft? -->

This would be well and good, except that our Euler angles would need to change whenever we switch: if our current estimate is close to sideways, or (0, 90, 0), and we switch model so that (0,0,0) means sideways, then our angles have to go back to (0,0,0) again.

In other words, we would have to keep track of two things: (1) which default orientation we are currently based around, and (2) the Euler angle 'offset' around that. Whenever we switch default orientation, we need to reset the offset to zero.

This sounds complicated and not very nice to implement...

## Absolute and relative rotations

The problem with the above strategy is that we don't actually address the issue, which is that Euler angles suck at keeping track of absolute orientation: any choice of Euler angles will degrade in their ability to express three degrees of freedom *somewhere*. In other words, Euler angles are best when kept close to the origin....

Combining this insight with the idea of 'switching models', we can maybe think of another solution: if Euler angles are so poor at describing absolute orientation, what if we used a rotation matrix?

The reason we dumped that in the first place was that we couldn't easily express a valid 'direction' to move in&mdash;a small incremental rotation. Meanwhile we have learned that Euler angles are great for that, but only around the origin.

So here's one solution: we use Euler angles to express an 'offset' around an absolute rotation matrix (one like the default orientation we talked about earlier):

    R = Rz(rz)*Ry(ry)*Rx(rx) * R0

But to keep the Euler angles close to zero (and prevent gimbal lock), we 'reset' the offset to zero after each step of gradient descent by left-multiplying the offset into our absolute rotation, using that as the stationary point for the next step:

    update_parameters(R0, tx,ty,tz):
        // Find the gradient by finite differences
        dedrx = (E(euler(+drx, 0, 0)*R0, [tx, ty, tz]) -
                 E(euler(-drx, 0, 0)*R0, [tx, ty, tz])) / 2drx
        dedry = (E(euler(0, +dry, 0)*R0, [tx, ty, tz]) -
                 E(euler(0, -dry, 0)*R0, [tx, ty, tz])) / 2dry
        dedrz = (E(euler(0, 0, +drz)*R0, [tx, ty, tz]) -
                 E(euler(0, 0, -drz)*R0, [tx, ty, tz])) / 2drz

        // Move/Rotate in the opposite 'direction' by some amount
        rx = -gain*dedrx
        ry = -gain*dedry
        rz = -gain*dedrz

        // Update the absolute rotation matrix
        R0 = euler(rx,ry,rz)*R0

This is not that different from our first strategy: in both cases we have the notion of an Euler angle 'offset' around some stationary rotation, some default orientation. But instead of updating the default orientation and resetting the offset to zero at pre-defined switching points, we update and reset after every optimization step.

By doing this we avoid having to keep track of both a rotation matrix *and* the offset around it; the offset is only ever used within gradient descent. And, because we compute the gradient by adding or subtracting a small delta (now around zero), we won't get gimbal locked as long as that delta is small enough.

But why?
--------

Why did we choose that particular Euler angle ordering? Are there better orderings? And I've that quaternions are super popular and useful, but we're not using them?? What about axis-angle?

The answer to all of these is that it doesn't really matter?

Let's look at a different Euler angle order.

todo: animation of the two. show that they are very tight for 'small angles'.
todo: define small angle

<!-- I think I prefer this explanation, as it doesn't get into "oh let's take the derivative of our error function analytically". Instead it keeps the discussion at the level of "what do small rotations look like?" -->

Aside: Quaternions
------------------

We could use quaternions to keep track of our absolute orientation, instead of the rotation matrix R0. Unlike Euler angles, quaternions do not suck at this, and are often the storage format of choice in e.g. video game and animation systems, because they use less bytes than rotation matrices.

Aside: Normalization
--------------------

This line of code might worry you:

    R0 = euler(rx,ry,rz)*R0

If you are an expert in floating point numbers you probably know that they are not perfect realizations of the real numbers: for example, `0.1+0.1` is not `0.2`, but `0.20000000298023224`.

Imperfections like these can be a concern when you deal with rotation matrices or quaternions over longer periods of time&sup1;, in the sense that repeatedly appending rotations will accumulate errors and cause the matrix (or quaternion) to stray from a valid rotation: the columns are no longer unit-length and pair-wise perpendicular, and objects appear slightly deformed after rotation.

<p style="color:#999;">
&sup1;I also mean time in the literal sense: bits flipped by radiation  can be a real concern. So even if you don't touch that rotation matrix, you might want to check up on it from time to time!
</p>

If you use quaternions you can do a renormalization (just calculate the length and divide by it). The equivalent for rotation matrices is called orthogonalization.

There are different ways to orthogonalize a matrix, depending on if you want higher accuracy or simpler code. Here's a simple one, used in the Robotics, Vision and Control matlab toolbox:

<!-- todo: stack overflow, rvc -->

Small rotations go to work
--------------------------

It turns out that for small angles, rotation matrices *commute*. In other words, the order in which you apply rotations doesn't matter. If you write out the matrix product Rz(ez)Ry(ey)Rx(ex), and replace the sines and cosines with the above approximations, you will get this:

    |   1      ex*ey - ez    ex*ez + ey |
    |  ez    ex*ey*ez + 1    ey*ez - ex |
    | -ey              ex             1 |

Assuming ex,ey,ez are small, we'll drop everything but the first order terms to get:

    |   1   -ez    ey |
    |  ez     1   -ex |
    | -ey    ex     1 |

Other Euler angles
------------------

XYX, XZX, YXY... The particular sequence is often a convention in the community. This begs the question, which one is best for us?

Axis-angle
----------
<!-- todo: what are we after? A minimal parametrization (three numbers). We can choose the numbers freely. Why? To take the derivative? explain... -->

Euler angles concatenate three rotations about three axes, but Euler did a lot of thinking about rotations (as he did with many other things) and proved that any rotation can also be described as a rotation about a single axis.

So we could alternatively parametrize our offset rotation in terms of an angle `a` and an axis `r`. To convert it to a rotation matrix we can use this formula from wikipedia:

    R = I + sin(a) S(r) + (1-cos(a)) S(r)S(r)

Similar to quaternions and rotation matrices, this is not a minimal parametrization: in this case the constraint lies on the axis to be unit-length This means we can't choose the numbers all freely and find a nice derivative of our error function. But let's ignore this for now, because it turns out to not matter.

Let's again consider a small rotation: that is, the angle `a` is close to zero, and the axis is...well something. Pulling up our trig identities again we know that approximately `sin(a) = a` and `cos(a) = 1`, so the formula above simplifies to:

    R = I + a S(r)

What's this `S(r)` thing you ask? It's ...


<!-- But storing `a` as one number and `r` as three numbers leads us into a similar problem we had with rotation matrices: we can't choose them all freely. In this case, the constraint lies on `r` to be unit-length.

So a trick that's commonly used is to multiply the angle into the axis vector, giving three numbers that can all be chosen freely. To recover the angle, you take the length of the vector. To find the axis, you normalize it.

If we call this vector `w` -->

So many choices.... but does it matter?
---------------------------------------

It turns out that for small angles, rotation matrices are actually commutative! This means that we really could have used any order we wanted in the above expression; they would all evaluate to roughly the same matrix. If you write out the matrix product Rz(ez)Ry(ey)Rx(ex), and replace the sines and cosines with the above approximations, you will get this:

    |   1      ex*ey - ez    ex*ez + ey |
    |  ez    ex*ey*ez + 1    ey*ez - ex |
    | -ey              ex             1 |

Assuming ex,ey,ez are small, we'll drop everything but the first order terms to get:

    |   1   -ez    ey |
    |  ez     1   -ex |
    | -ey    ex     1 |

And you will indeed get this same matrix no matter which order you apply the rotations. However, since the order appears to not matter, why should it matter if we use angle-axis over Euler angles? It turns out that it doesn't either! The matrix above is *exactly* the approximated angle-axis matrix for small rotations:

    (I + w^x) where w=(ex,ey,ez)

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
