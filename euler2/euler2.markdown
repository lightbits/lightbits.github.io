# Stepping through rotations: Part II

The take-away from last time was that Euler angles can *gimbal lock*, where you lose the ability to rotate around all three axes: adjusting any angle in isolation only gives you two distinct motions. This causes gradient descent, and similar optimization strategies, to slow to a stop, or adjust the wrong parameters.

Another way of seeing it is that Euler angles suck at tracking *absolute* orientation.

See, when I coded this 3D book model, I inadvertently chose its default orientation (all angles zero) to be with its cover facing the camera:

![](model3.png)

This happens to have an impact on gimbal lock: for this choice, we have all three degrees of freedom when the cover is facing the camera, but not when the book is sideways. On the other hand, if the default orientation had been sideways....

![](model4.png)

we would have three degrees of freedom at the sideways orientation, but *not* when the cover is facing the camera.

No matter which default orientation we base our Euler angles around, we will run into gimbal lock sufficiently far away. But they are OK as long as we stay close to the zero.

<br>
<br>
<br>
# The Tumbler

3D modelling software have tackled similar problems for a long time: how can the user, with their 2D mouse interface, rotate an object in 3D?

One solution is called the *Tumbler*. It is notoriously unintuitive and the only excuse you get for using it is [not knowing any better](todo: matt keeter). It works like this: when you click and drag your mouse horizontally or vertically, it adjusts either of two Euler angles and rotates the thing, but when you let go, this orientation is saved and the Euler angles are reset to zero.

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
<div class="slider-wrap">
    <div class="slider" id="slider4" style="max-width:240px;max-height:260px;">
        <div style="width:1700px;"><img src="gimbals1.png"/><img src="gimbals1-2.png"/><img src="gimbals2.png"/><img src="gimbals3.png"/><img src="gimbals3-4.png"/><img src="gimbals4.png"/><img src="gimbals5.png"/></div>
    </div>
    <br>
    <input type="range" min=0 max=6 step=1 value=0 oninput="document.getElementById('slider4').scrollLeft = this.value*240;"></input>
    <label>Click and drag</label>
</div>

The coordinate frame you rotate around follows the object while you're rotating it, but it resets when you release the mouse button. So no matter how much you have rotated the object in the past, when you click and drag your mouse up and down, or left and right, it behaves the same as the first time.

<!-- three distinct ways... -->
<!-- <div class="slider-wrap">
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
</div> -->

It turns out that this is a **terrible** user interface, because, even though the object can theoretically be rotated in three distinct ways anywhere you start rotating, the mouse's lack of a third dimension keeps you from accessing more than two. For us though it is a great solution to our gimbal lock problem.

Notice how dragging the Tumbler, without letting go, is like our first strategy of accumulating small angle increments from gradient descent (the difference being that gradient descent is not limited by a two-dimensional mouse). This runs into gimbal lock if we drag it too far, but not if we let go before the Euler angles get too big.

We can extend this idea to gradient descent: instead of accumulating increments into a set of global angles, we apply the rotation they represent to the object's currently saved orientation which we store, for example, as a rotation matrix.

This little change makes all the difference. See, when we computed the gradient last time, we added or subtracted a delta around global Euler angles, like so:

<pre><code>dedrx = <span style="color:#999;">(E(euler(</span><span id="efg">rx+drx</span><span style="color:#999;">,ry,rz), T) -
         E(euler(</span><span id="efg">rx-drx</span><span style="color:#999;">,ry,rz), T)) / 2drx</span>
dedry = <span style="color:#999;">(E(euler(rx,</span><span id="efg">ry+dry</span><span style="color:#999;">,rz), T) -
         E(euler(rx,</span><span id="efg">ry-dry</span><span style="color:#999;">,rz), T)) / 2dry</span>
dedrz = <span style="color:#999;">(E(euler(rx,ry,</span><span id="efg">rz+drz</span><span style="color:#999;">), T) -
         E(euler(rx,ry,</span><span id="efg">rz-drz</span><span style="color:#999;">), T)) / 2drz</span></code></pre>

<!--
    dedrx = (E(euler(rx+drx,ry,rz), T) -
             E(euler(rx-drx,ry,rz), T)) / 2drx
    dedry = (E(euler(rx,ry+dry,rz), T) -
             E(euler(rx,ry-dry,rz), T)) / 2dry
    dedrz = (E(euler(rx,ry,rz+drz), T) -
             E(euler(rx,ry,rz-drz), T)) / 2drz
 -->
If the global angles were at a particular point (the `euler` matrix was close to gimbal lock) adding or subtracting a delta would not have the effect we wanted. But now we can compute the gradient by adding or subtracting a delta around zero, and applying that to the current rotation matrix:

<pre><code><span style="color:#999;"><span style="color:#000;">dedrx = </span>(E(euler(<span style="color:#000;">0+drx</span>,0,0)<span style="color:#000;">*R</span>, T) -
         E(euler(<span style="color:#000;">0-drx</span>,0,0)<span style="color:#000;">*R</span>, T)) / 2drx
<span style="color:#000;">dedry = </span>(E(euler(0,<span style="color:#000;">0+dry</span>,0)<span style="color:#000;">*R</span>, T) -
         E(euler(0,<span style="color:#000;">0-dry</span>,0)<span style="color:#000;">*R</span>, T)) / 2dry
<span style="color:#000;">dedrz = </span>(E(euler(0,0,<span style="color:#000;">0+drz</span>)<span style="color:#000;">*R</span>, T) -
         E(euler(0,0,<span style="color:#000;">0-drz</span>)<span style="color:#000;">*R</span>, T)) / 2drz</span></code></pre>

The gradient gives us a "direction" to rotate in, and, like before, we can turn that into three angles `rx,ry,rz`. But instead of accumulating those into three global angles, we update the orientation like this:

    R = euler(rx,ry,rz) * R

As long as the amount we rotate by is small, these angles will be close to zero, and the euler matrix behaves nicely.

<!-- <p style="color:#999;">
We could also use unit-length quaternions to track orientation. They are often preferred because they use fewer bytes than rotation matrices and, like rotation matrices, they do not gimbal lock. But they also have constraints to keep them valid (must be unit-length), so we can't freely adjust its parameters to find a direction for gradient descent.
</p> -->

<br>
<br>
<br>
# Looking closely

<!-- alternatively, I could go into reducing computational cost. First, look at what computing the gradient would involve. Exploit fact that two/three parameters are zero. Close to zero. Trig approximations...

But that doesn't lead nicely into axis-angle or other euler angle orders...
 -->

![](euler-random-big.png)
<br>

You may be asking why we used that particular Euler angle convention; maybe a different one would be better?

The above image compares the two most popular conventions. Each cube is rotated by three random angles. The gray cubes are rotated by the same angles, but with the other Euler order.

It clearly looks like a mess, none of the cubes are alike. So if we were to update our orientation using one or the other, we could get completely different results!

    R = euler1(rx,ry,rz)*R // this would be completely different from
    R = euler2(rx,ry,rz)*R // this

But let's look more closely around the area that we're interested in, small angles, say, within plus or minus 20 degrees around zero.

![](euler-random-small.png)

Now we almost can't tell them apart! But why?

<br>
<br>
<!-- # The maths -->
<!-- <br> -->

Let's look at the actual maths behind these rotations:
 <!-- If we multiply together the rotation matrices for a ZYX rotation we get this: -->

![](eq1.png)

<!--
    | cy*cz   cz*sx*sy - cx*sz   sx*sz + cx*cz*sy |
    | cy*sz   cx*cz + sx*sy*sz   cx*sy*sz - cz*sx |
    |   -sy              cy*sx              cx*cy |
 -->

Ok it's horrible.

But a trig fact tells us that for small angles `cos(x) = 1` and `sin(x) = x`,  so we can say that the above monstrosity is almost equal to this:

![](eq2.png)

<!--
    |  1      x*y - z    x*z + y |
    |  z    x*y*z + 1    y*z - x |
    | -y            x          1 |
 -->

And if we multiply two small numbers together, the product becomes *really* small compared to any one of them alone, so we get:

![](eq3.png)

<!--
    |  1   -z    y |
    |  z    1   -x |
    | -y    x    1 |
 -->

Ok, here is the exciting part: if you go ahead and try, you will find that *any Euler angle order is equal to this matrix (for small angles)*.

This hints at the idea that it wouldn't matter which Euler convention we chose to update our orientation, they would have pretty much the same effect.

But wait, isn't that *super suspicious*? Doesn't it make you wonder, *why*?

<br>
<br>
<!-- # Axis-angle -->

No? How about this...

Euler angles are three angles about three axes, but we can also parametrize our rotation in terms of one axis and one angle around it. There's even a formula to convert that to a rotation matrix:

![](eq4.png)
<!-- R = I + sin(a) skew(r) + (1-cos(a)) skew(r)^2 -->

<p style="color:#999;">`a` is the angle and `r` is the axis. We'll see what this `skew` function is soon</p>

This is not minimal because it uses four numbers, but if we multiply the angle into the axis we do get a minimal parametrization: a vector whose length is the original angle and, when normalized, is the original axis. Let's rewrite our formula in terms of this vector:

![](eq5.png)
<!-- R = I + sin(|w|) skew(w/|w|) + (1-cos(|w|)) skew(w/|w|)^2 -->

So what happens when the angle is small? Well, some things cancel and we're left with:

![](eq6.png)
<!-- R = I + skew(w) -->

That `skew(w)` thing is called the *skew-symmetric* form of `w`, and is the matrix that, when multiplied with a vector, gives you the cross product between `w` and that vector.

![](eq7.png)
<!--
                    |  0   -z    y |
    skew([x,y,z]) = |  z    0   -x |
                    | -y    x    0 |
-->
which means that
![](eq8.png)
<!--
        |  1   -z    y |
    R = |  z    1   -x |
        | -y    x    1 |
-->

Well how about that, it's the same matrix as before!

<br>
<br>

To recap what's going on, we first represented the rotation as three numbers describing Euler angles, and irrespective of what convention we interpreted them to be, if the angles were small, we got the above matrix.

<!-- On one hand, we had three Euler angles x, y, and z. No matter what convention we use, xyz or zyx or something else, if the angles were small, we got the above matrix. -->
We then looked at using three numbers describing an angle and an axis, but if the angle was small, we got the same thing.

<!-- On the other hand, we used three numbers to describe an angle and an axis, but if the angle was small, we got the same thing. -->

Although we assigned entirely different meanings to these three numbers (an axis-angle or any order of Euler rotations)&mdash;and for big angles they look entirely different too!&mdash;they are all the same in some sense.

<br>
<br>
<br>
# Physics

It seems like there is a "canonical" small rotation, that all forms of rotations tend towards.

To try and intuitively appreciate this, let's first look at stuff in two dimensions.
![](physics2.png)
In physics, you may have learned that a point rotating on a circle has a velocity tangent to the circle, and that the speed is proportional to the angular speed and the radius: v = wr.

![](physics1.png)

We could also, more generally, say that the velocity is the cross product between an *angular velocity vector*, pointing in or out of the page, and the position: v = w x r.

<!-- The right-hand rule lets you figure out the direction of the angular velocity vector by wrapping your right hand along the rotation. Your thumb will then either point away from or into the page. -->

![](physics3.png)

The latter also holds in 3D, now with the point rotating in a plane perpendicular to the angular velocity vector, which can be an arbitrary direction; not just in or out of the page.

I bring this up because rotation matrices can be seen as a set of three vectors, defining the three axes of a coordinate system.
![](eq9.png)
<!-- R = [X | Y | Z] -->
What we did earlier was to rotate this matrix by a small Euler angle offset, which we wrote as a matrix-matrix product. But we can expand that and multiply each vector inside:
![](eq10.png)
<!-- R = euler*R = [euler*X | euler*Y | euler*Z] -->
We also saw that the euler (and axis-angle) matrix, for small angles, was equal to:

![](eq11.png)

<!-- I + skew(w) -->
Remember that skew(w), when multiplied by a vector, gives the cross product between w and that vector. So if we put that back into the above we get:

![](eq12.png)

<!-- R = R + [w cross X | w cross Y | w cross Z] -->
Which looks a lot like adding, to the current orientation, the tangential velocity of each axis rotating on a circle, with a speed and direction defined by w.

From a physics point of view, in the same way that a point rotating on a circle has a velocity tangent to it, each axis in the coordinate frame does too, and we find it by taking the cross product between the angular velocity and the axis.

The weird thing, though, is that *any* rotation made small enough, is essentially no different from an angular velocity vector, and they are all *the same* angular velocity vector.
<!-- It's the analog of linearizing a translation; but in rotation space -->

<br>
<br>
# The mathematics of things that look similar

What is this w thing, these three numbers? We've called them Euler angles and axis-angle, but if you make them small enough it doesn't matter. Why is rotation so annoying when translation is so easy? What's special about it?

Mathematicians thought about these questions, and decided to invent a thing in mathematics called Lie groups&mdash;which is a part of group theory, which is about defining very precisely how stuff that look similar are, in fact, similar (for some definition of similar).

They promptly went ahead and gave weird names to everything. w, for example, is called the *Lie Algebra Element of SO3* or just *so3*. Yes, lower case matters and yes, it's confusing. The way I remember which is which is that SO3 is a big rotation and so3 is a small rotation.

There's even a book about this. Its cover says *STATE ESTIMATION FOR ROBOTICS* in large, bold letters, with *A Matrix Lie Group Approach* underneath. Unfortunately, after reading it, I can't tell you why rotations are strange; it supposedly has to do with translations living in a vector space, and that rotations do not, but if you ask me why rotations don't live in a vector space I can't give you an answer.

<!-- 6. Isn't that neat?
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

    Unwanted coupling: you don't want to have to rotate in order to translate. -->

<!--

Back to our optimization problem then.

What we have learned is that when we update the current rotation with the offset from gradient descent:

    R = euler(rx,ry,rz)*R

it does not matter what Euler angle convention we use or if we use axis-angle&mdash;they all pretty much have the same effect if `rx,ry,rz` are small.

But that leads to another question: which one is the "correct" one to use, when we update the rotation matrix? If we interpret `rx,ry,rz` as not being Euler angles anymore (because they could be any ordering, or they could even be axis-angle), but being this canonical small rotation direction, what is the "canonical" rotation it represents along its line?

What we get from gradient descent is just a (weighted) direction. Line search etc. But what does it mean to continue a rotation? -->
